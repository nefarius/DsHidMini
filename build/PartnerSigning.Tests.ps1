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

$product = New-PartnerProductPayload -ProductName 'DsHidMini 3.6.0 3.6.0.2145' -AnnouncementDate '2026-09-09T00:00:00'
Assert-Equal $product.testHarness 'Attestation' 'product harness'
Assert-Equal $product.deviceType 'external' 'product device type'
Assert-Equal $product.selectedProductTypes.windows_v100_RS5 'Unclassified' 'product type RS5'
Assert-Equal $product.requestedSignatures.Count 2 'signature count'
Assert-Equal $product.requestedSignatures[0] 'WINDOWS_v100_X64_RS5_FULL' 'x64 RS5 signature'
Assert-Equal $product.requestedSignatures[1] 'WINDOWS_v100_ARM64_RS5_FULL' 'ARM64 RS5 signature'

Assert-Equal (Get-SdcmEntityId -Json '{"id": 1152921505701840714, "name": "x"}') '1152921505701840714' 'entity id stays a string'
Assert-Equal (Get-SdcmEntityId -Json '{"id": "1152921505701840714", "name": "x"}') '1152921505701840714' 'quoted entity id stays a string'

$submission = New-PartnerSubmissionPayload -Name 'DsHidMini 3.6.0.2145'
Assert-Equal $submission.type 'initial' 'submission type'
Assert-Equal (Get-PartnerPortalUrl -ProductId '123') 'https://partner.microsoft.com/dashboard/hardware/driver/123' 'portal url'

$created = [pscustomobject]@{ commitStatus = 'commitPending' }
Assert-Equal (Get-PartnerSubmissionProgress -Submission $created) 'Created' 'created progress'
Assert-True (Test-PartnerSubmissionNeedsUpload -Submission $created) 'created needs upload'
Assert-True (Test-PartnerSubmissionNeedsCommit -Submission $created) 'created needs commit'

$idle = [pscustomobject]@{
    commitStatus   = 'commitPending'
    workflowStatus = [pscustomobject]@{ state = 'notStarted'; currentStep = '' }
}
Assert-Equal (Get-PartnerSubmissionProgress -Submission $idle) 'Created' 'notStarted stays created'

$submitted = [pscustomobject]@{
    commitStatus    = 'commitSucceeded'
    workflowStatus  = [pscustomobject]@{ state = 'inProgress'; currentStep = 'finalizeIngestion' }
}
Assert-Equal (Get-PartnerSubmissionProgress -Submission $submitted) 'Submitted' 'submitted progress'
Assert-True (-not (Test-PartnerSubmissionNeedsUpload -Submission $submitted)) 'submitted skips upload'
Assert-True (-not (Test-PartnerSubmissionNeedsCommit -Submission $submitted)) 'submitted skips commit'

$processing = [pscustomobject]@{
    commitStatus   = 'commitComplete'
    workflowStatus = [pscustomobject]@{ state = 'notStarted'; currentStep = 'Processing' }
}
Assert-Equal (Get-PartnerSubmissionProgress -Submission $processing) 'Submitted' 'portal Processing is submitted'
Assert-True (-not (Test-PartnerSubmissionNeedsCommit -Submission $processing)) 'processing skips commit'

$sdcmListJson = @'
[
  {
    "id": "1152921505701853745",
    "commitStatus": "commitComplete",
    "workflowStatus": {
      "currentStep": "Processing",
      "state": "notStarted"
    }
  }
]
'@
$fromSdcm = ConvertFrom-SdcmJson -Json $sdcmListJson
Assert-Equal $fromSdcm.id '1152921505701853745' 'list array unwraps quoted id'
Assert-Equal (Get-PartnerSubmissionProgress -Submission $fromSdcm) 'Submitted' 'sdcm list JSON is submitted'
Assert-True ((Get-PartnerSubmissionProgressSummary -Submission $fromSdcm) -like '*Submitted*') 'progress summary includes Submitted'

$completed = [pscustomobject]@{
    workflowStatus = [pscustomobject]@{ state = 'completed' }
    downloads      = [pscustomobject]@{
        items = @(
            [pscustomobject]@{ type = 'signedPackage' }
        )
    }
}
Assert-Equal (Get-PartnerSubmissionProgress -Submission $completed) 'Completed' 'completed progress'

$failed = [pscustomobject]@{ workflowStatus = [pscustomobject]@{ state = 'failed' } }
Assert-Equal (Get-PartnerSubmissionProgress -Submission $failed) 'Failed' 'failed progress'

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
