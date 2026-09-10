#Requires -Version 7.0
Set-StrictMode -Version Latest

$script:PartnerSignedNamePattern = '^Signed_(\d+)\.zip$'
$script:PartnerInitialNamePattern = '^Initial_(\d+)\.cab$'

# sdcm 1.0.0-pre004 owns the Hardware Dev Center state machine. progress is
# created | processing | completed | failed. state describes currentStep only.
$script:SdcmProgressCreated = 'created'
$script:SdcmProgressProcessing = 'processing'
$script:SdcmProgressCompleted = 'completed'
$script:SdcmProgressFailed = 'failed'

function Invoke-Sdcm {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]] $SdcmArgs
    )

    & sdcm --output json --auth client-secret @SdcmArgs
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "sdcm $($SdcmArgs -join ' ') failed with exit code $exitCode"
    }
}

function Write-Utf8NoBomJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $Object,

        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $directory = Split-Path -Parent $Path
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $json = $Object | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($false))
}

function New-PartnerProductPayload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProductName,

        [Parameter(Mandatory = $true)]
        [string] $AnnouncementDate
    )

    [ordered]@{
        productName           = $ProductName
        testHarness           = 'Attestation'
        announcementDate      = $AnnouncementDate
        firmwareVersion       = '0'
        deviceType            = 'external'
        isTestSign            = $false
        isFlightSign          = $false
        selectedProductTypes  = @{ windows_v100_RS5 = 'Unclassified' }
        requestedSignatures   = @(
            'WINDOWS_v100_X64_RS5_FULL'
            'WINDOWS_v100_ARM64_RS5_FULL'
        )
    }
}

function New-PartnerSubmissionPayload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    [ordered]@{
        name = $Name
        type = 'initial'
    }
}

function Get-PartnerPortalUrl {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProductId
    )

    "https://partner.microsoft.com/dashboard/hardware/driver/$ProductId"
}

function Get-PartnerSubmissionProperty {
    [CmdletBinding()]
    param(
        $Object,
        [string[]] $Names
    )

    if ($null -eq $Object) {
        return $null
    }

    foreach ($name in $Names) {
        $property = $Object.PSObject.Properties[$name]
        if ($property -and $null -ne $property.Value -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            return [string]$property.Value
        }
    }

    return $null
}

function Get-SdcmSubmissionStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProductId,

        [Parameter(Mandatory = $true)]
        [string] $SubmissionId
    )

    # Failed submissions still emit the status document, then exit 7.
    $json = & sdcm --output json --auth client-secret submission status --product-id $ProductId --submission-id $SubmissionId
    $exitCode = $LASTEXITCODE
    if ($exitCode -notin 0, 7) {
        throw "sdcm submission status --product-id $ProductId --submission-id $SubmissionId failed with exit code $exitCode"
    }
    return ConvertFrom-SdcmJson -Json $json
}

function Get-SdcmSubmissionProgress {
    [CmdletBinding()]
    param($Status)

    $progress = Get-PartnerSubmissionProperty -Object $Status -Names @('progress')
    if (-not $progress) {
        return $script:SdcmProgressCreated
    }

    return $progress.Trim().ToLowerInvariant()
}

function Get-SdcmSubmissionProgressSummary {
    [CmdletBinding()]
    param($Status)

    $progress = Get-SdcmSubmissionProgress -Status $Status
    $commit = Get-PartnerSubmissionProperty -Object $Status -Names @('commitStatus')
    $state = Get-PartnerSubmissionProperty -Object $Status -Names @('state')
    $step = Get-PartnerSubmissionProperty -Object $Status -Names @('currentStep')
    $signed = $false
    if ($Status -and $Status.PSObject.Properties['hasSignedPackage'] -and $null -ne $Status.hasSignedPackage) {
        $signed = [bool]$Status.hasSignedPackage
    }
    if (-not $commit) { $commit = '-' }
    if (-not $state) { $state = '-' }
    if (-not $step) { $step = '-' }

    return "Submission progress: $progress (commitStatus=$commit; state=$state; currentStep=$step; signedPackage=$signed)"
}

function Test-SdcmSubmissionNeedsUpload {
    [CmdletBinding()]
    param($Status)

    return (Get-SdcmSubmissionProgress -Status $Status) -eq $script:SdcmProgressCreated
}

function Test-SdcmSubmissionNeedsCommit {
    [CmdletBinding()]
    param($Status)

    return (Get-SdcmSubmissionProgress -Status $Status) -eq $script:SdcmProgressCreated
}

function Find-PartnerSignedPackagePair {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root
    )

    if (-not (Test-Path -LiteralPath $Root)) {
        throw "Partner download directory not found: $Root"
    }

    $signed = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter 'Signed_*.zip' |
        Where-Object { $_.Name -match $script:PartnerSignedNamePattern })
    $initial = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter 'Initial_*.cab' |
        Where-Object { $_.Name -match $script:PartnerInitialNamePattern })

    if ($signed.Count -eq 0 -or $initial.Count -eq 0) {
        throw "Partner download is missing Signed_<id>.zip and/or Initial_<id>.cab under $Root."
    }

    $pairs = foreach ($zip in $signed) {
        if ($zip.Name -notmatch $script:PartnerSignedNamePattern) {
            continue
        }
        $id = $Matches[1]
        $cab = $initial | Where-Object { $_.Name -eq "Initial_$id.cab" } | Select-Object -First 1
        if ($cab) {
            [pscustomobject]@{
                Id           = $id
                SignedZip    = $zip.FullName
                InitialCab   = $cab.FullName
                SignedName   = $zip.Name
                InitialName  = $cab.Name
            }
        }
    }

    $pairs = @($pairs)
    if ($pairs.Count -eq 0) {
        throw "Partner download has Signed_*.zip and Initial_*.cab files, but no matching numeric id pair under $Root."
    }
    if ($pairs.Count -gt 1) {
        $names = ($pairs | ForEach-Object { $_.SignedName }) -join ', '
        throw "Partner download has multiple Signed_/Initial_ pairs ($names). Point at a single submission download."
    }

    return $pairs[0]
}

function Get-SdcmEntityId {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $Json
    )

    $entity = ConvertFrom-SdcmJson -Json $Json
    if ($entity -is [System.Collections.IList] -and $entity -isnot [string]) {
        $entity = @($entity) | Select-Object -First 1
    }

    $id = Get-PartnerSubmissionProperty -Object $entity -Names @('id')
    if (-not $id -or $id -notmatch '^\d+$') {
        $preview = ConvertTo-SdcmJsonText -Json $Json
        if ($preview.Length -gt 500) { $preview = $preview.Substring(0, 500) + '...' }
        throw "sdcm JSON is missing a numeric id field.`n$preview"
    }

    return $id
}

function ConvertTo-SdcmJsonText {
    [CmdletBinding()]
    param($Json)

    if ($null -eq $Json) {
        return ''
    }
    if ($Json -is [string]) {
        return $Json
    }

    $items = @($Json)
    $allStrings = $true
    foreach ($item in $items) {
        if ($item -isnot [string]) {
            $allStrings = $false
            break
        }
    }
    if ($allStrings) {
        return $items -join [Environment]::NewLine
    }

    return ConvertTo-Json -InputObject $Json -Depth 8
}

function ConvertFrom-SdcmJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $Json
    )

    $text = ConvertTo-SdcmJsonText -Json $Json
    if ([string]::IsNullOrWhiteSpace($text)) {
        throw 'sdcm returned empty JSON.'
    }

    $parsed = $text | ConvertFrom-Json
    if ($parsed -is [System.Collections.IList] -and $parsed -isnot [string]) {
        $items = @($parsed)
        if ($items.Count -eq 1) {
            return $items[0]
        }
        return $items
    }

    return $parsed
}
