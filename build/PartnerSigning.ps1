#Requires -Version 7.0
Set-StrictMode -Version Latest

$script:PartnerSignedNamePattern = '^Signed_(\d+)\.zip$'
$script:PartnerInitialNamePattern = '^Initial_(\d+)\.cab$'

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

function Get-PartnerSubmissionWorkflowState {
    [CmdletBinding()]
    param($Submission)

    if ($null -eq $Submission) {
        return $null
    }

    if (-not $Submission.PSObject.Properties['workflowStatus']) {
        return $null
    }

    $workflow = $Submission.workflowStatus
    if ($null -eq $workflow) {
        return $null
    }

    if ($workflow -is [string]) {
        return $workflow
    }

    foreach ($name in @('state', 'currentState', 'status')) {
        if ($workflow.PSObject.Properties[$name] -and $workflow.$name) {
            return [string]$workflow.$name
        }
    }

    return $null
}

function Get-PartnerSubmissionProgress {
    [CmdletBinding()]
    param($Submission)

    $workflow = Get-PartnerSubmissionWorkflowState -Submission $Submission
    $commit = $null
    if ($Submission -and $Submission.PSObject.Properties['commitStatus'] -and $Submission.commitStatus) {
        $commit = [string]$Submission.commitStatus
    }

    $downloadTypes = @()
    if ($Submission -and $Submission.PSObject.Properties['downloads'] -and $Submission.downloads -and
        $Submission.downloads.PSObject.Properties['items'] -and $Submission.downloads.items) {
        $downloadTypes = @(
            $Submission.downloads.items |
                ForEach-Object { $_.type } |
                Where-Object { $_ }
        )
    }

    $failed = @(
        'failed', 'failure', 'cancelled', 'canceled', 'commitFailed'
    )
    if ($workflow -and ($failed -contains $workflow)) {
        return 'Failed'
    }
    if ($commit -and ($failed -contains $commit)) {
        return 'Failed'
    }

    $completed = @('completed', 'complete', 'succeeded', 'success')
    if ($workflow -and ($completed -contains $workflow)) {
        return 'Completed'
    }
    if ($downloadTypes -contains 'signedPackage') {
        return 'Completed'
    }

    $submitted = @(
        'inProgress', 'inprogress', 'commitSucceeded', 'commitInProgress', 'finalizeIngestion'
    )
    if ($commit -and ($submitted -contains $commit)) {
        return 'Submitted'
    }
    if ($workflow -and ($submitted -contains $workflow)) {
        return 'Submitted'
    }

    return 'Created'
}

function Test-PartnerSubmissionNeedsUpload {
    [CmdletBinding()]
    param($Submission)

    $progress = Get-PartnerSubmissionProgress -Submission $Submission
    return $progress -eq 'Created'
}

function Test-PartnerSubmissionNeedsCommit {
    [CmdletBinding()]
    param($Submission)

    $progress = Get-PartnerSubmissionProgress -Submission $Submission
    return $progress -eq 'Created'
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
        [string] $Json
    )

    # sdcm --output json re-serializes Hardware Dev Center ids with
    # LongToStringJsonConverter, so the value is a quoted string. The raw API
    # uses an unquoted number. Accept both so the id stays a string.
    $match = [regex]::Match($Json, '"id"\s*:\s*"?(\d+)"?')
    if (-not $match.Success) {
        $preview = if ($Json.Length -gt 500) { $Json.Substring(0, 500) + '...' } else { $Json }
        throw "sdcm JSON is missing an id field.`n$preview"
    }

    return $match.Groups[1].Value
}

function ConvertFrom-SdcmJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Json
    )

    if ([string]::IsNullOrWhiteSpace($Json)) {
        throw 'sdcm returned empty JSON.'
    }

    $parsed = $Json | ConvertFrom-Json
    if ($parsed -is [System.Array]) {
        if ($parsed.Count -eq 1) {
            return $parsed[0]
        }
        return $parsed
    }

    return $parsed
}
