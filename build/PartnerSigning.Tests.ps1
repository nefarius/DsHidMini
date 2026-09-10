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

function New-TestStatus {
    [CmdletBinding()]
    param(
        [string] $Progress,
        [string] $CommitStatus = '',
        [string] $State = '',
        [string] $Step = '',
        [bool] $HasSignedPackage = $false
    )

    return [pscustomobject]@{
        progress         = $Progress
        commitStatus     = $CommitStatus
        state            = $State
        currentStep      = $Step
        hasSignedPackage = $HasSignedPackage
    }
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

Assert-Equal (Get-SdcmEntityId -Json '{"id": 1152921505701840714, "name": "x"}') '1152921505701840714' 'entity id stays a string'
Assert-Equal (Get-SdcmEntityId -Json '{"id": "1152921505701840714", "name": "x"}') '1152921505701840714' 'quoted entity id stays a string'
Assert-Equal (Get-SdcmEntityId -Json '{"sharedProductId": "1152921504607010608", "id": "14631253285588838"}') '14631253285588838' 'sharedProductId is not mistaken for id'
Assert-Equal (Get-SdcmEntityId -Json '{"productId": "13872423721100346", "id": "1152921505701853745"}') '1152921505701853745' 'productId is not mistaken for id'
Assert-Equal (Get-SdcmEntityId -Json ([pscustomobject]@{ id = '1152921505701840714'; name = 'x' })) '1152921505701840714' 'PSCustomObject entity id'
Assert-Throws { Get-SdcmEntityId -Json '{"name": "x"}' } 'missing id throws'
Assert-Throws { Get-SdcmEntityId -Json '' } 'empty sdcm output throws'

$created = New-TestStatus -Progress 'created' -CommitStatus 'CommitPending' -State 'notStarted' -Step 'packageInfoValidation'
Assert-Equal (Get-SdcmSubmissionProgress -Status $created) 'created' 'created progress'
Assert-True (Test-SdcmSubmissionNeedsUpload -Status $created) 'created needs upload'
Assert-True (Test-SdcmSubmissionNeedsCommit -Status $created) 'created needs commit'

$processing = New-TestStatus -Progress 'processing' -CommitStatus 'commitComplete' -State 'started' -Step 'scanning'
Assert-Equal (Get-SdcmSubmissionProgress -Status $processing) 'processing' 'processing progress'
Assert-True (-not (Test-SdcmSubmissionNeedsUpload -Status $processing)) 'processing skips upload'
Assert-True (-not (Test-SdcmSubmissionNeedsCommit -Status $processing)) 'processing skips commit'

$completed = New-TestStatus -Progress 'completed' -CommitStatus 'commitComplete' -State 'completed' -Step 'finalizeIngestion' -HasSignedPackage $true
Assert-Equal (Get-SdcmSubmissionProgress -Status $completed) 'completed' 'completed progress'
Assert-True (-not (Test-SdcmSubmissionNeedsUpload -Status $completed)) 'completed skips upload'

$failed = New-TestStatus -Progress 'failed' -CommitStatus 'commitFailed' -State 'failed' -Step 'validation'
Assert-Equal (Get-SdcmSubmissionProgress -Status $failed) 'failed' 'failed progress'

$statusJson = @'
{
  "progress": "processing",
  "commitStatus": "commitComplete",
  "state": "started",
  "currentStep": "scanning",
  "hasSignedPackage": false
}
'@
$fromStatus = ConvertFrom-SdcmJson -Json $statusJson
Assert-Equal (Get-SdcmSubmissionProgress -Status $fromStatus) 'processing' 'status JSON is processing'
Assert-True ((Get-SdcmSubmissionProgressSummary -Status $fromStatus) -like '*processing*') 'status summary includes processing'
Assert-True ((Get-SdcmSubmissionProgressSummary -Status $fromStatus) -like '*currentStep=scanning*') 'status summary includes the current step'

$getJson = @'
{
  "id": "1152921505701853745",
  "productId": "13872423721100346",
  "name": "DsHidMini 3.6.1 3.6.1.2202 submission",
  "type": "initial",
  "commitStatus": "commitComplete"
}
'@
$fromGet = ConvertFrom-SdcmJson -Json $getJson
Assert-True ($fromGet.id -is [string]) 'get id is a string'
Assert-Equal $fromGet.id '1152921505701853745' 'get JSON keeps quoted id'
Assert-Equal (Get-SdcmEntityId -Json $getJson) '1152921505701853745' 'get JSON entity id'

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
