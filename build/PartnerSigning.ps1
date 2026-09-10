#Requires -Version 7.0
Set-StrictMode -Version Latest

$script:PartnerSignedNamePattern = '^Signed_(\d+)\.zip$'
$script:PartnerInitialNamePattern = '^Initial_(\d+)\.cab$'

# Hardware Dev Center submission vocabulary. commitStatus is documented as
# commitPending / commitComplete / commitFailed, but the API reference shows
# 'CommitPending' while the service returns 'commitPending', so every comparison
# below is case-insensitive. workflowStatus.state is one of notStarted, started,
# failed, completed and describes *the current step only*; currentStep walks
# packageInfoValidation, preparation, scanning, validation, catalogCreation,
# manualReview, signing, finalizeIngestion.
$script:PartnerCommitPending = 'commitPending'
$script:PartnerCommitFailed = 'commitFailed'
$script:PartnerWorkflowFinalStep = 'finalizeIngestion'
$script:PartnerSignedPackageType = 'signedPackage'

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

function Test-PartnerValueEquals {
    [CmdletBinding()]
    param(
        [string] $Value,
        [Parameter(Mandatory = $true)]
        [string] $Expected
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return [string]::Equals($Value.Trim(), $Expected, [StringComparison]::OrdinalIgnoreCase)
}

function Get-PartnerSubmissionWorkflow {
    [CmdletBinding()]
    param($Submission)

    if ($null -eq $Submission -or -not $Submission.PSObject.Properties['workflowStatus']) {
        return $null
    }

    return $Submission.workflowStatus
}

function Get-PartnerSubmissionWorkflowState {
    [CmdletBinding()]
    param($Submission)

    $workflow = Get-PartnerSubmissionWorkflow -Submission $Submission
    if ($null -eq $workflow) {
        return $null
    }
    if ($workflow -is [string]) {
        return $workflow
    }

    return Get-PartnerSubmissionProperty -Object $workflow -Names @('state', 'currentState', 'status')
}

function Get-PartnerSubmissionWorkflowStep {
    [CmdletBinding()]
    param($Submission)

    $workflow = Get-PartnerSubmissionWorkflow -Submission $Submission
    if ($null -eq $workflow -or $workflow -is [string]) {
        return $null
    }

    return Get-PartnerSubmissionProperty -Object $workflow -Names @('currentStep')
}

function Get-PartnerSubmissionCommitStatus {
    [CmdletBinding()]
    param($Submission)

    return Get-PartnerSubmissionProperty -Object $Submission -Names @('commitStatus')
}

function Test-PartnerSubmissionHasSignedPackage {
    [CmdletBinding()]
    param($Submission)

    if ($null -eq $Submission -or -not $Submission.PSObject.Properties['downloads']) {
        return $false
    }

    $downloads = $Submission.downloads
    if ($null -eq $downloads -or -not $downloads.PSObject.Properties['items'] -or -not $downloads.items) {
        return $false
    }

    foreach ($item in @($downloads.items)) {
        if ($null -eq $item -or -not $item.PSObject.Properties['type']) {
            continue
        }
        if (Test-PartnerValueEquals -Value ([string]$item.type) -Expected $script:PartnerSignedPackageType) {
            return $true
        }
    }

    return $false
}

function Get-PartnerSubmissionProgress {
    [CmdletBinding()]
    param($Submission)

    $commit = Get-PartnerSubmissionCommitStatus -Submission $Submission
    $state = Get-PartnerSubmissionWorkflowState -Submission $Submission
    $step = Get-PartnerSubmissionWorkflowStep -Submission $Submission

    if ((Test-PartnerValueEquals -Value $commit -Expected $script:PartnerCommitFailed) -or
        (Test-PartnerValueEquals -Value $state -Expected 'failed')) {
        return 'Failed'
    }

    # A downloadable signedPackage is the only unambiguous completion signal.
    # state applies to currentStep, so 'completed' on an intermediate step such
    # as scanning must not be read as the whole submission being done.
    if (Test-PartnerSubmissionHasSignedPackage -Submission $Submission) {
        return 'Completed'
    }
    if ((Test-PartnerValueEquals -Value $state -Expected 'completed') -and
        (Test-PartnerValueEquals -Value $step -Expected $script:PartnerWorkflowFinalStep)) {
        return 'Completed'
    }

    # Anything other than commitPending means the submission already belongs to
    # Hardware Dev Center and must not be uploaded or committed again. Treating
    # unrecognised values as committed keeps an undocumented in-flight status
    # from triggering a second upload.
    if ($commit -and -not (Test-PartnerValueEquals -Value $commit -Expected $script:PartnerCommitPending)) {
        return 'Submitted'
    }
    # Fallback for a submission that reports no commitStatus at all: the
    # workflow only leaves notStarted once Hardware Dev Center owns the package.
    if ((Test-PartnerValueEquals -Value $state -Expected 'started') -or
        (Test-PartnerValueEquals -Value $state -Expected 'completed')) {
        return 'Submitted'
    }

    return 'Created'
}

function Get-PartnerSubmissionProgressSummary {
    [CmdletBinding()]
    param($Submission)

    $progress = Get-PartnerSubmissionProgress -Submission $Submission
    $commit = Get-PartnerSubmissionCommitStatus -Submission $Submission
    $state = Get-PartnerSubmissionWorkflowState -Submission $Submission
    $step = Get-PartnerSubmissionWorkflowStep -Submission $Submission
    $signed = Test-PartnerSubmissionHasSignedPackage -Submission $Submission
    if (-not $commit) { $commit = '-' }
    if (-not $state) { $state = '-' }
    if (-not $step) { $step = '-' }

    return "Submission progress: $progress (commitStatus=$commit; state=$state; currentStep=$step; signedPackage=$signed)"
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
        $Json
    )

    # sdcm --output json re-serializes Hardware Dev Center ids as quoted strings
    # via LongToStringJsonConverter, while the raw API uses unquoted numbers.
    # Parse the document instead of pattern matching so a nested id can never be
    # mistaken for the entity's own id.
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

    return (@($Json) | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
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
