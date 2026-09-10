#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'PartnerSigning.ps1')

function Assert-Equal($Actual, $Expected, [string] $Name) {
    if ($Actual -cne $Expected) {
        throw "FAIL ${Name}: expected '$Expected', got '$Actual'."
    }
    Write-Output "PASS $Name"
}

function Assert-True([bool] $Condition, [string] $Name) {
    if (-not $Condition) {
        throw "FAIL $Name"
    }
    Write-Output "PASS $Name"
}

function Assert-Throws([scriptblock] $Action, [string] $Name) {
    $threw = $false
    try {
        & $Action
    }
    catch {
        $threw = $true
    }

    if (-not $threw) {
        throw "FAIL ${Name}: expected an exception"
    }

    Write-Output "PASS $Name"
}

# Builds a submission shaped like the Hardware Dashboard API's Submission
# resource, so the state table below is exercised against real field names and
# real documented values rather than invented ones.
function New-TestSubmission {
    [CmdletBinding()]
    param(
        [string] $CommitStatus = '',
        [string] $State = '',
        [string] $Step = '',
        [string[]] $DownloadTypes = @()
    )

    $submission = [ordered]@{
        id        = '1152921505701853745'
        productId = '13872423721100346'
        name      = 'DsHidMini 3.6.1 3.6.1.2202 submission'
        type      = 'initial'
    }
    if ($CommitStatus) {
        $submission['commitStatus'] = $CommitStatus
    }
    if ($State -or $Step) {
        $submission['workflowStatus'] = [pscustomobject]@{
            currentStep = $Step
            state       = $State
            messages    = @()
        }
    }
    if ($DownloadTypes.Count -gt 0) {
        $submission['downloads'] = [pscustomobject]@{
            items    = @($DownloadTypes | ForEach-Object {
                [pscustomobject]@{ type = $_; url = "https://example.invalid/$_" }
            })
            messages = @()
        }
    }

    return [pscustomobject]$submission
}

$product = New-PartnerProductPayload -ProductName 'DsHidMini 3.6.0 3.6.0.2145' -AnnouncementDate '2026-09-09T00:00:00'
Assert-Equal $product.testHarness 'Attestation' 'product harness'
Assert-Equal $product.deviceType 'external' 'product device type'
Assert-Equal $product.selectedProductTypes.windows_v100_RS5 'Unclassified' 'product type RS5'
Assert-Equal $product.requestedSignatures.Count 2 'signature count'
Assert-Equal $product.requestedSignatures[0] 'WINDOWS_v100_X64_RS5_FULL' 'x64 RS5 signature'
Assert-Equal $product.requestedSignatures[1] 'WINDOWS_v100_ARM64_RS5_FULL' 'ARM64 RS5 signature'

$submission = New-PartnerSubmissionPayload -Name 'DsHidMini 3.6.0.2145'
Assert-Equal $submission.type 'initial' 'submission type'
Assert-Equal (Get-PartnerPortalUrl -ProductId '123') 'https://partner.microsoft.com/dashboard/hardware/driver/123' 'portal url'

# The raw API returns ids as numbers; sdcm re-serializes them as quoted strings.
Assert-Equal (Get-SdcmEntityId -Json '{"id": 1152921505701840714, "name": "x"}') '1152921505701840714' 'entity id stays a string'
Assert-Equal (Get-SdcmEntityId -Json '{"id": "1152921505701840714", "name": "x"}') '1152921505701840714' 'quoted entity id stays a string'
Assert-Equal (Get-SdcmEntityId -Json '{"sharedProductId": "1152921504607010608", "id": "14631253285588838"}') '14631253285588838' 'sharedProductId is not mistaken for id'
Assert-Equal (Get-SdcmEntityId -Json '{"productId": "13872423721100346", "id": "1152921505701853745"}') '1152921505701853745' 'productId is not mistaken for id'
Assert-Equal (Get-SdcmEntityId -Json @('[', '  { "id": "42" }', ']')) '42' 'single-element array output unwraps'
Assert-Throws { Get-SdcmEntityId -Json '{"name": "x"}' } 'missing id throws'
Assert-Throws { Get-SdcmEntityId -Json '' } 'empty sdcm output throws'

# workflowStatus.state is the state of workflowStatus.currentStep, not of the
# submission as a whole, so every step/state combination has to be classified
# explicitly. Documented states: notStarted, started, failed, completed.
# Documented steps: packageInfoValidation, preparation, scanning, validation,
# catalogCreation, manualReview, signing, finalizeIngestion.
$progressCases = @(
    @{ Name = 'fresh submission is Created'; Commit = 'CommitPending'; State = 'notStarted'; Step = 'packageInfoValidation'; Downloads = @('initialPackage'); Expected = 'Created' }
    @{ Name = 'commitPending without workflow is Created'; Commit = 'commitPending'; State = ''; Step = ''; Downloads = @(); Expected = 'Created' }
    @{ Name = 'submission with no status at all is Created'; Commit = ''; State = ''; Step = ''; Downloads = @(); Expected = 'Created' }
    @{ Name = 'initialPackage alone is not completion'; Commit = 'CommitPending'; State = 'notStarted'; Step = ''; Downloads = @('initialPackage'); Expected = 'Created' }
    @{ Name = 'committed but not yet started is Submitted'; Commit = 'commitComplete'; State = 'notStarted'; Step = 'packageInfoValidation'; Downloads = @('initialPackage'); Expected = 'Submitted' }
    @{ Name = 'scanning in progress is Submitted'; Commit = 'commitComplete'; State = 'started'; Step = 'scanning'; Downloads = @('initialPackage'); Expected = 'Submitted' }
    @{ Name = 'completed intermediate step is still Submitted'; Commit = 'commitComplete'; State = 'completed'; Step = 'scanning'; Downloads = @('initialPackage'); Expected = 'Submitted' }
    @{ Name = 'manual review is Submitted'; Commit = 'commitComplete'; State = 'started'; Step = 'manualReview'; Downloads = @('initialPackage'); Expected = 'Submitted' }
    @{ Name = 'signing is Submitted'; Commit = 'commitComplete'; State = 'started'; Step = 'signing'; Downloads = @('initialPackage'); Expected = 'Submitted' }
    @{ Name = 'undocumented in-flight commit status is Submitted'; Commit = 'commitInProgress'; State = 'notStarted'; Step = ''; Downloads = @(); Expected = 'Submitted' }
    @{ Name = 'started workflow without commitStatus is Submitted'; Commit = ''; State = 'started'; Step = 'validation'; Downloads = @(); Expected = 'Submitted' }
    @{ Name = 'completed final step is Completed'; Commit = 'commitComplete'; State = 'completed'; Step = 'finalizeIngestion'; Downloads = @(); Expected = 'Completed' }
    @{ Name = 'signedPackage download is Completed'; Commit = 'commitComplete'; State = 'started'; Step = 'signing'; Downloads = @('initialPackage', 'signedPackage'); Expected = 'Completed' }
    @{ Name = 'failed workflow state is Failed'; Commit = 'commitComplete'; State = 'failed'; Step = 'validation'; Downloads = @('initialPackage'); Expected = 'Failed' }
    @{ Name = 'commitFailed is Failed'; Commit = 'commitFailed'; State = 'notStarted'; Step = 'preparation'; Downloads = @(); Expected = 'Failed' }
    @{ Name = 'failure wins over a signed package'; Commit = 'commitFailed'; State = 'failed'; Step = 'signing'; Downloads = @('signedPackage'); Expected = 'Failed' }
)

foreach ($case in $progressCases) {
    $candidate = New-TestSubmission -CommitStatus $case.Commit -State $case.State -Step $case.Step -DownloadTypes $case.Downloads
    Assert-Equal (Get-PartnerSubmissionProgress -Submission $candidate) $case.Expected $case.Name
}

# Upload and commit must fire exactly once, for a submission that is still ours.
$fresh = New-TestSubmission -CommitStatus 'CommitPending' -State 'notStarted' -Step 'packageInfoValidation' -DownloadTypes @('initialPackage')
Assert-True (Test-PartnerSubmissionNeedsUpload -Submission $fresh) 'fresh submission needs upload'
Assert-True (Test-PartnerSubmissionNeedsCommit -Submission $fresh) 'fresh submission needs commit'

$committed = New-TestSubmission -CommitStatus 'commitComplete' -State 'notStarted' -Step 'packageInfoValidation' -DownloadTypes @('initialPackage')
Assert-True (-not (Test-PartnerSubmissionNeedsUpload -Submission $committed)) 'committed submission skips upload'
Assert-True (-not (Test-PartnerSubmissionNeedsCommit -Submission $committed)) 'committed submission skips commit'

$processing = New-TestSubmission -CommitStatus 'commitComplete' -State 'started' -Step 'scanning' -DownloadTypes @('initialPackage')
Assert-True (-not (Test-PartnerSubmissionNeedsUpload -Submission $processing)) 'processing submission skips upload'
Assert-True (-not (Test-PartnerSubmissionNeedsCommit -Submission $processing)) 'processing submission skips commit'

# `sdcm submission list --submission-id` wraps the entity in an array and quotes
# every id, which is what the workflow actually has to parse.
$sdcmListJson = @'
[
  {
    "id": "1152921505701853745",
    "productId": "13872423721100346",
    "name": "DsHidMini 3.6.1 3.6.1.2202 submission",
    "type": "initial",
    "commitStatus": "commitComplete",
    "workflowStatus": {
      "currentStep": "scanning",
      "state": "started",
      "messages": []
    },
    "downloads": {
      "items": [
        { "type": "initialPackage", "url": "https://example.invalid/initial" }
      ],
      "messages": []
    }
  }
]
'@
$fromSdcm = ConvertFrom-SdcmJson -Json $sdcmListJson
Assert-Equal $fromSdcm.id '1152921505701853745' 'list array unwraps quoted id'
Assert-Equal (Get-PartnerSubmissionProgress -Submission $fromSdcm) 'Submitted' 'sdcm list JSON is Submitted'
Assert-True ((Get-PartnerSubmissionProgressSummary -Submission $fromSdcm) -like '*Submitted*') 'progress summary includes Submitted'
Assert-True ((Get-PartnerSubmissionProgressSummary -Submission $fromSdcm) -like '*currentStep=scanning*') 'progress summary includes the current step'

$sdcmCompletedJson = @'
[
  {
    "id": "1152921505701853745",
    "productId": "13872423721100346",
    "commitStatus": "commitComplete",
    "workflowStatus": {
      "currentStep": "finalizeIngestion",
      "state": "completed",
      "messages": []
    },
    "downloads": {
      "items": [
        { "type": "initialPackage", "url": "https://example.invalid/initial" },
        { "type": "signedPackage", "url": "https://example.invalid/signed" },
        { "type": "certificationReport", "url": "https://example.invalid/report" }
      ],
      "messages": []
    }
  }
]
'@
$completedFromSdcm = ConvertFrom-SdcmJson -Json $sdcmCompletedJson
Assert-Equal (Get-PartnerSubmissionProgress -Submission $completedFromSdcm) 'Completed' 'sdcm completed JSON is Completed'
Assert-True (Test-PartnerSubmissionHasSignedPackage -Submission $completedFromSdcm) 'signed package detected'
Assert-True (-not (Test-PartnerSubmissionHasSignedPackage -Submission $fromSdcm)) 'no signed package while processing'

$temp = Join-Path ([IO.Path]::GetTempPath()) ("dshm-partner-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $id = '1152921505701840714'
    Set-Content -LiteralPath (Join-Path $temp "Signed_$id.zip") -Value 'signed'
    Set-Content -LiteralPath (Join-Path $temp "Initial_$id.cab") -Value 'initial'
    $pair = Find-PartnerSignedPackagePair -Root $temp
    Assert-Equal $pair.Id $id 'pair id'
    Assert-Equal $pair.SignedName "Signed_$id.zip" 'signed name'
    Assert-Equal $pair.InitialName "Initial_$id.cab" 'initial name'

    Set-Content -LiteralPath (Join-Path $temp 'Signed_999.zip') -Value 'other'
    $stillPaired = Find-PartnerSignedPackagePair -Root $temp
    Assert-Equal $stillPaired.Id $id 'unmatched extra signed zip still pairs one id'

    Remove-Item -LiteralPath (Join-Path $temp "Initial_$id.cab")
    Assert-Throws { Find-PartnerSignedPackagePair -Root $temp } 'missing initial cab'

    $nested = Join-Path $temp 'nested'
    New-Item -ItemType Directory -Path $nested | Out-Null
    Set-Content -LiteralPath (Join-Path $nested "Signed_$id.zip") -Value 'signed'
    Set-Content -LiteralPath (Join-Path $nested "Initial_$id.cab") -Value 'initial'
    Set-Content -LiteralPath (Join-Path $temp "Signed_42.zip") -Value 'signed'
    Set-Content -LiteralPath (Join-Path $temp "Initial_42.cab") -Value 'initial'
    Assert-Throws { Find-PartnerSignedPackagePair -Root $temp } 'multiple pairs rejected'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force
}

Write-Output 'PartnerSigning tests passed'
