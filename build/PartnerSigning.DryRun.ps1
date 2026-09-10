#Requires -Version 7.0
<#
.SYNOPSIS
    Runs the Partner Center signing workflow offline against a mock sdcm.

.DESCRIPTION
    Every attempt against the real Hardware Dev Center consumes a Partner Center
    product and submission, so the workflow's create / upload / wait glue cannot
    be debugged in CI. This harness extracts the literal `run:` bodies out of
    .github/workflows/partner-signing.yml and executes them against a fake sdcm
    that implements the documented Submission state machine, asserting which sdcm
    verbs each stage invokes. It needs no credentials and no network.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$workflowPath = Join-Path $repoRoot '.github/workflows/partner-signing.yml'
$root = Join-Path ([IO.Path]::GetTempPath()) ('dshm-partner-dryrun-' + [guid]::NewGuid().ToString('N'))

$script:Failures = New-Object System.Collections.Generic.List[string]

function Assert-Equal($Actual, $Expected, [string] $Name) {
    if ([string]$Actual -ne [string]$Expected) {
        $script:Failures.Add($Name)
        Write-Host "FAIL ${Name}: expected '$Expected', got '$Actual'"
        return
    }
    Write-Host "PASS $Name"
}

function Assert-True([bool] $Condition, [string] $Name) {
    if (-not $Condition) {
        $script:Failures.Add($Name)
        Write-Host "FAIL $Name"
        return
    }
    Write-Host "PASS $Name"
}

# Pull the literal `run: |` bodies out of the workflow so the dry run exercises
# the same glue CI executes rather than a paraphrase of it.
function Get-WorkflowPwshBlocks {
    param([string] $Path)

    $lines = [IO.File]::ReadAllLines($Path)
    $blocks = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^(\s+)run: \|\s*$') { continue }
        $indent = $Matches[1].Length
        $body = New-Object System.Collections.Generic.List[string]
        $j = $i + 1
        while ($j -lt $lines.Count) {
            $line = $lines[$j]
            if ([string]::IsNullOrWhiteSpace($line)) { $body.Add(''); $j++; continue }
            $lineIndent = $line.Length - $line.TrimStart().Length
            if ($lineIndent -le $indent) { break }
            $body.Add($line.Substring($indent + 2))
            $j++
        }
        $blocks.Add(($body -join "`n"))
        $i = $j - 1
    }

    return $blocks
}

function Select-Block {
    param(
        [System.Collections.Generic.List[string]] $Blocks,
        [string] $Signature
    )

    $matched = @($Blocks | Where-Object { $_ -like "*$Signature*" })
    if ($matched.Count -ne 1) {
        throw "Expected exactly one workflow run block containing '$Signature', found $($matched.Count)."
    }

    return $matched[0]
}

function Invoke-Block {
    param(
        [string] $Body,
        [string] $WorkDir,
        [hashtable] $Substitutions = @{},
        [hashtable] $Variables = @{}
    )

    $text = $Body
    foreach ($key in $Substitutions.Keys) {
        $text = $text.Replace($key, [string]$Substitutions[$key])
    }
    if ($text -match '\$\{\{') {
        throw "Unsubstituted workflow expression remains: $([regex]::Match($text, '\$\{\{[^}]*\}\}').Value)"
    }

    $scriptPath = Join-Path $WorkDir ('block-' + [guid]::NewGuid().ToString('N') + '.ps1')
    [IO.File]::WriteAllText($scriptPath, $text, [Text.UTF8Encoding]::new($false))

    foreach ($key in $Variables.Keys) {
        Set-Item -Path "Env:$key" -Value ([string]$Variables[$key])
    }

    Push-Location $WorkDir
    try {
        $output = & pwsh -NoProfile -File $scriptPath 2>&1
        $exit = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    return [pscustomobject]@{
        ExitCode = $exit
        Output   = ($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
    }
}

# ---------------------------------------------------------------------------
# Mock sdcm: a state machine over the documented Submission resource. Ids come
# back quoted. `submission get` returns one object; `submission status` returns
# the normalized progress document. Verbs reject out-of-order calls the way
# the real tool does.
# ---------------------------------------------------------------------------
$mockScript = @'
#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$statePath = $env:SDCM_MOCK_STATE
$logPath = $env:SDCM_MOCK_LOG
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json -AsHashtable

function Save-State { $state | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statePath -Encoding utf8 }

$cliArgs = @($args)
function Read-Option([string] $Name) {
    $idx = [array]::IndexOf($cliArgs, $Name)
    if ($idx -lt 0 -or $idx + 1 -ge $cliArgs.Count) { return $null }
    return $cliArgs[$idx + 1]
}

$flags = @('--overwrite', '--wait-metadata', '--verbose', '-v')
$verbs = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $cliArgs.Count; $i++) {
    if ($cliArgs[$i] -like '--*' -or $cliArgs[$i] -eq '-v') {
        if ($flags -notcontains $cliArgs[$i]) { $i++ }
        continue
    }
    $verbs.Add($cliArgs[$i])
}
$noun = $verbs[0]
$verb = $verbs[1]
Add-Content -LiteralPath $logPath -Value "$noun $verb"

function Write-SubmissionJson([switch] $AsArray) {
    $downloads = @($state.downloads | ForEach-Object { @{ type = $_; url = "https://example.invalid/$_" } })
    $submission = [ordered]@{
        id             = $state.submissionId
        productId      = $state.productId
        name           = 'DsHidMini dry run submission'
        type           = 'initial'
        commitStatus   = $state.commitStatus
        workflowStatus = [ordered]@{
            currentStep = $state.step
            state       = $state.state
            messages    = @()
        }
        downloads      = [ordered]@{ items = $downloads; messages = @() }
    }
    if ($AsArray) { ConvertTo-Json @($submission) -Depth 8 } else { ConvertTo-Json $submission -Depth 8 }
}

function Write-StatusJson {
    $failed = ($state.state -eq 'failed') -or ($state.commitStatus -eq 'commitFailed')
    $signed = $state.downloads -contains 'signedPackage'
    $ready = $signed -or ($state.state -eq 'completed' -and $state.step -eq 'finalizeIngestion')
    $progress = if ($failed) { 'failed' } elseif ($ready) { 'completed' } elseif ($state.commitStatus -eq 'CommitPending') { 'created' } else { 'processing' }
    ConvertTo-Json ([ordered]@{
        progress         = $progress
        commitStatus     = $state.commitStatus
        state            = $state.state
        currentStep      = $state.step
        hasSignedPackage = [bool]$signed
    }) -Depth 8
    if ($failed) { exit 7 }
}

switch ("$noun $verb") {
    'product create' {
        $state.productId = '14631253285588838'
        Save-State
        ConvertTo-Json ([ordered]@{
            id              = $state.productId
            sharedProductId = '1152921504607010608'
            productName     = 'DsHidMini dry run'
        }) -Depth 8
        exit 0
    }
    'submission create' {
        $state.submissionId = '1152921505701853745'
        $state.commitStatus = 'CommitPending'
        $state.state = 'notStarted'
        $state.step = 'packageInfoValidation'
        $state.downloads = @('initialPackage')
        Save-State
        Write-SubmissionJson
        exit 0
    }
    'submission list' {
        Write-SubmissionJson -AsArray
        exit 0
    }
    'submission get' {
        Write-SubmissionJson
        exit 0
    }
    'submission status' {
        Write-StatusJson
        exit 0
    }
    'submission upload' {
        if ($state.commitStatus -ne 'CommitPending') { Write-Error 'requestInvalidForCurrentState'; exit 6 }
        $state.uploaded = $true
        Save-State
        exit 0
    }
    'submission commit' {
        if (-not $state.uploaded) { Write-Error 'no package uploaded'; exit 6 }
        if ($state.commitStatus -ne 'CommitPending') { Write-Error 'already committed'; exit 6 }
        $state.commitStatus = 'commitComplete'
        $state.state = 'started'
        $state.step = 'preparation'
        Save-State
        exit 0
    }
    'submission wait' {
        if ($state.commitStatus -eq 'CommitPending') { Write-Error 'wait timed out'; exit 9 }
        $state.state = 'completed'
        $state.step = 'finalizeIngestion'
        $state.downloads = @('initialPackage', 'signedPackage', 'certificationReport')
        Save-State
        Write-SubmissionJson
        exit 0
    }
    'submission download' {
        if ($state.downloads -notcontains 'signedPackage') { Write-Error 'no signedPackage available'; exit 5 }
        $target = Read-Option '--output-file'
        $overwrite = $cliArgs -contains '--overwrite'
        if ((Test-Path -LiteralPath $target) -and -not $overwrite) { Write-Error 'destination exists'; exit 4 }
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
        $stage = Join-Path ([IO.Path]::GetTempPath()) ('sdcm-mock-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $stage | Out-Null
        $pkg = $state.submissionId
        Set-Content -LiteralPath (Join-Path $stage "Signed_$pkg.zip") -Value 'signed-package'
        Set-Content -LiteralPath (Join-Path $stage "Initial_$pkg.cab") -Value 'initial-package'
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $target
        Remove-Item -LiteralPath $stage -Recurse -Force
        exit 0
    }
    default {
        Write-Error "mock sdcm does not implement '$noun $verb'"
        exit 1
    }
}
'@

$mockBin = Join-Path $root 'bin'
New-Item -ItemType Directory -Force -Path $mockBin | Out-Null
[IO.File]::WriteAllText((Join-Path $mockBin 'sdcm-mock.ps1'), $mockScript, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText(
    (Join-Path $mockBin 'sdcm.cmd'),
    "@echo off`r`npwsh -NoProfile -File `"%~dp0sdcm-mock.ps1`" %*`r`n",
    [Text.UTF8Encoding]::new($false))
$originalPath = $env:PATH
$env:PATH = "$mockBin$([IO.Path]::PathSeparator)$env:PATH"

# ---------------------------------------------------------------------------
# Scenario driver
# ---------------------------------------------------------------------------
$blocks = Get-WorkflowPwshBlocks -Path $workflowPath
$createBlock = Select-Block -Blocks $blocks -Signature 'RESUME_PRODUCT_ID'
$uploadBlock = Select-Block -Blocks $blocks -Signature 'Test-SdcmSubmissionNeedsUpload'
$waitBlock = Select-Block -Blocks $blocks -Signature 'partner-signing-result.json'

function New-Sandbox([string] $Name) {
    $dir = Join-Path $root $Name
    New-Item -ItemType Directory -Force -Path (Join-Path $dir 'build') | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot 'build/PartnerSigning.ps1') -Destination (Join-Path $dir 'build/PartnerSigning.ps1')
    New-Item -ItemType Directory -Force -Path (Join-Path $dir 'metadata') | Out-Null
    @{ tag = 'v3.6.1.2202'; setupVersion = '3.6.1'; driverVersion = '3.6.1.2202' } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dir 'metadata/release-metadata.json') -Encoding utf8
    New-Item -ItemType Directory -Force -Path (Join-Path $dir 'submission') | Out-Null
    Set-Content -LiteralPath (Join-Path $dir 'submission/dshidmini_3.6.1.2202.cab') -Value 'ev-signed-cab'
    Set-Content -LiteralPath (Join-Path $dir 'outputs.txt') -Value ''
    Set-Content -LiteralPath (Join-Path $dir 'summary.md') -Value ''
    return $dir
}

function New-ScenarioEnvironment {
    param([string] $Sandbox, [hashtable] $SeedState, [hashtable] $Overrides = @{})

    $statePath = Join-Path $Sandbox 'sdcm-state.json'
    $logPath = Join-Path $Sandbox 'sdcm-calls.log'
    $SeedState | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statePath -Encoding utf8
    Set-Content -LiteralPath $logPath -Value ''

    $variables = @{
        SOURCE_RUN_ID        = '34446000000'
        GITHUB_OUTPUT        = (Join-Path $Sandbox 'outputs.txt')
        GITHUB_STEP_SUMMARY  = (Join-Path $Sandbox 'summary.md')
        SDCM_MOCK_STATE      = $statePath
        SDCM_MOCK_LOG        = $logPath
        RESUME_PRODUCT_ID    = ''
        RESUME_SUBMISSION_ID = ''
    }
    foreach ($key in $Overrides.Keys) { $variables[$key] = $Overrides[$key] }

    return $variables
}

function Get-StepOutput([string] $Sandbox, [string] $Key) {
    $line = Get-Content -LiteralPath (Join-Path $Sandbox 'outputs.txt') |
        Where-Object { $_ -like "$Key=*" } | Select-Object -First 1
    if (-not $line) { return '' }
    return ($line -split '=', 2)[1]
}

function Get-SdcmCalls([string] $Sandbox) {
    # A list rather than an array so an empty call log survives being stored on
    # a pscustomobject property instead of collapsing to $null.
    $calls = New-Object System.Collections.Generic.List[string]
    Get-Content -LiteralPath (Join-Path $Sandbox 'sdcm-calls.log') |
        Where-Object { $_ } | ForEach-Object { $calls.Add($_) }
    return , $calls
}

function Measure-Call([string[]] $Calls, [string] $Call) {
    return @($Calls | Where-Object { $_ -eq $Call }).Count
}

function Invoke-Pipeline {
    param([string] $Name, [hashtable] $SeedState, [hashtable] $Overrides = @{}, [int] $ExpectedWaitExitCode = 0)

    Write-Host ''
    Write-Host "=== $Name ==="
    $sandbox = New-Sandbox ($Name -replace '[^A-Za-z0-9]', '-')
    $variables = New-ScenarioEnvironment -Sandbox $sandbox -SeedState $SeedState -Overrides $Overrides

    $create = Invoke-Block -Body $createBlock -WorkDir $sandbox -Variables $variables `
        -Substitutions @{ '${{ github.run_id }}' = '99999999' }
    Assert-Equal $create.ExitCode 0 "${Name}: create step succeeds"
    if ($create.ExitCode -ne 0) { Write-Host $create.Output }

    $productId = Get-StepOutput $sandbox 'product-id'
    $submissionId = Get-StepOutput $sandbox 'submission-id'
    Assert-True ($productId -match '^\d+$') "${Name}: create emits a numeric product-id"
    Assert-True ($submissionId -match '^\d+$') "${Name}: create emits a numeric submission-id"

    New-Item -ItemType Directory -Force -Path (Join-Path $sandbox 'checkpoint') | Out-Null
    Copy-Item -LiteralPath (Join-Path $sandbox 'partner-signing-checkpoint.json') `
        -Destination (Join-Path $sandbox 'checkpoint/partner-signing-checkpoint.json') -Force

    $substitutions = @{
        '${{ github.run_id }}'                      = '99999999'
        '${{ needs.create.outputs.product-id }}'    = $productId
        '${{ needs.create.outputs.submission-id }}' = $submissionId
    }

    $upload = Invoke-Block -Body $uploadBlock -WorkDir $sandbox -Variables $variables -Substitutions $substitutions
    Assert-Equal $upload.ExitCode 0 "${Name}: upload step succeeds"
    if ($upload.ExitCode -ne 0) { Write-Host $upload.Output }

    $wait = Invoke-Block -Body $waitBlock -WorkDir $sandbox -Variables $variables -Substitutions $substitutions
    Assert-Equal $wait.ExitCode $ExpectedWaitExitCode "${Name}: wait step exit $ExpectedWaitExitCode"
    if ($wait.ExitCode -ne $ExpectedWaitExitCode) { Write-Host $wait.Output }

    $calls = Get-SdcmCalls $sandbox
    Write-Host "sdcm calls: $($calls -join ' | ')"

    return [pscustomobject]@{
        Sandbox      = $sandbox
        Calls        = $calls
        ProductId    = $productId
        SubmissionId = $submissionId
        UploadOutput = $upload.Output
        WaitOutput   = $wait.Output
    }
}

function Invoke-CreateOnly {
    param([string] $Name, [hashtable] $SeedState, [hashtable] $Overrides = @{})

    Write-Host ''
    Write-Host "=== $Name ==="
    $sandbox = New-Sandbox ($Name -replace '[^A-Za-z0-9]', '-')
    $variables = New-ScenarioEnvironment -Sandbox $sandbox -SeedState $SeedState -Overrides $Overrides
    $create = Invoke-Block -Body $createBlock -WorkDir $sandbox -Variables $variables `
        -Substitutions @{ '${{ github.run_id }}' = '99999999' }

    return [pscustomobject]@{
        Sandbox  = $sandbox
        ExitCode = $create.ExitCode
        Output   = $create.Output
        Calls    = (Get-SdcmCalls $sandbox)
    }
}

try {
    # A normal release run: create a product, upload, commit, wait, download.
    $fresh = Invoke-Pipeline -Name 'fresh product' -SeedState @{
        productId = ''; submissionId = ''; commitStatus = 'CommitPending'
        state = 'notStarted'; step = ''; downloads = @(); uploaded = $false
    }
    Assert-Equal (Measure-Call $fresh.Calls 'product create') 1 'fresh: product created once'
    Assert-Equal (Measure-Call $fresh.Calls 'submission create') 1 'fresh: submission created once'
    Assert-Equal (Measure-Call $fresh.Calls 'submission upload') 1 'fresh: package uploaded exactly once'
    Assert-Equal (Measure-Call $fresh.Calls 'submission commit') 1 'fresh: submission committed exactly once'
    Assert-Equal (Measure-Call $fresh.Calls 'submission wait') 1 'fresh: waited once'
    Assert-Equal (Measure-Call $fresh.Calls 'submission download') 1 'fresh: downloaded once'
    Assert-Equal (Measure-Call $fresh.Calls 'submission list') 0 'fresh: does not use deprecated list'
    Assert-True ((Measure-Call $fresh.Calls 'submission status') -ge 2) 'fresh: status is the progress source'
    $result = Get-Content -LiteralPath (Join-Path $fresh.Sandbox 'partner-signing-result.json') -Raw | ConvertFrom-Json
    Assert-Equal $result.productId $fresh.ProductId 'fresh: result records the product id'
    Assert-Equal $result.submissionId $fresh.SubmissionId 'fresh: result records the submission id'
    Assert-Equal $result.signedZip "Signed_$($fresh.SubmissionId).zip" 'fresh: result records the signed zip'
    Assert-Equal $result.tag 'v3.6.1.2202' 'fresh: result records the tag'
    Assert-True (Test-Path -LiteralPath (Join-Path $fresh.Sandbox "partner-signed/Signed_$($fresh.SubmissionId).zip")) 'fresh: signed zip staged'
    Assert-True (Test-Path -LiteralPath (Join-Path $fresh.Sandbox "partner-signed/Initial_$($fresh.SubmissionId).cab")) 'fresh: initial cab staged'

    # Resuming a submission Hardware Dev Center is already processing must not
    # open a second product, and must not upload or commit again.
    $processing = Invoke-Pipeline -Name 'resume processing' -Overrides @{
        RESUME_PRODUCT_ID = '13872423721100346'; RESUME_SUBMISSION_ID = '1152921505701853745'
    } -SeedState @{
        productId = '13872423721100346'; submissionId = '1152921505701853745'; commitStatus = 'commitComplete'
        state = 'started'; step = 'scanning'; downloads = @('initialPackage'); uploaded = $true
    }
    Assert-Equal (Measure-Call $processing.Calls 'product create') 0 'processing: no new product created'
    Assert-Equal (Measure-Call $processing.Calls 'submission create') 0 'processing: no new submission created'
    Assert-Equal (Measure-Call $processing.Calls 'submission upload') 0 'processing: upload skipped'
    Assert-Equal (Measure-Call $processing.Calls 'submission commit') 0 'processing: commit skipped'
    Assert-Equal (Measure-Call $processing.Calls 'submission wait') 1 'processing: waited once'
    Assert-Equal (Measure-Call $processing.Calls 'submission get') 1 'processing: get used to resume'
    Assert-Equal (Measure-Call $processing.Calls 'submission list') 0 'processing: does not use deprecated list'
    Assert-Equal $processing.ProductId '13872423721100346' 'processing: keeps the requested product id'
    Assert-Equal $processing.SubmissionId '1152921505701853745' 'processing: keeps the requested submission id'

    # Resuming a product whose submission was created but never committed, which
    # is what an orphaned product from a failed early step looks like.
    $uncommitted = Invoke-Pipeline -Name 'resume uncommitted' -Overrides @{
        RESUME_PRODUCT_ID = '13872423721100347'; RESUME_SUBMISSION_ID = '1152921505701853746'
    } -SeedState @{
        productId = '13872423721100347'; submissionId = '1152921505701853746'; commitStatus = 'CommitPending'
        state = 'notStarted'; step = 'packageInfoValidation'; downloads = @('initialPackage'); uploaded = $false
    }
    Assert-Equal (Measure-Call $uncommitted.Calls 'product create') 0 'uncommitted: no new product created'
    Assert-Equal (Measure-Call $uncommitted.Calls 'submission upload') 1 'uncommitted: package uploaded once'
    Assert-Equal (Measure-Call $uncommitted.Calls 'submission commit') 1 'uncommitted: submission committed once'
    Assert-Equal (Measure-Call $uncommitted.Calls 'submission wait') 1 'uncommitted: waited once'

    # An already finished submission must skip the wait and go straight to the
    # download.
    $completed = Invoke-Pipeline -Name 'resume completed' -Overrides @{
        RESUME_PRODUCT_ID = '13872423721100348'; RESUME_SUBMISSION_ID = '1152921505701853747'
    } -SeedState @{
        productId = '13872423721100348'; submissionId = '1152921505701853747'; commitStatus = 'commitComplete'
        state = 'completed'; step = 'finalizeIngestion'
        downloads = @('initialPackage', 'signedPackage'); uploaded = $true
    }
    Assert-Equal (Measure-Call $completed.Calls 'submission wait') 0 'completed: wait skipped'
    Assert-Equal (Measure-Call $completed.Calls 'submission download') 1 'completed: downloaded once'
    Assert-True ($completed.WaitOutput -like '*already completed*') 'completed: skip is reported'

    # progress=completed is not enough; download only when a signed package exists.
    $completedNoSigned = Invoke-Pipeline -Name 'completed without signed package' -ExpectedWaitExitCode 1 -Overrides @{
        RESUME_PRODUCT_ID = '13872423721100350'; RESUME_SUBMISSION_ID = '1152921505701853749'
    } -SeedState @{
        productId = '13872423721100350'; submissionId = '1152921505701853749'; commitStatus = 'commitComplete'
        state = 'completed'; step = 'finalizeIngestion'
        downloads = @('initialPackage'); uploaded = $true
    }
    Assert-Equal (Measure-Call $completedNoSigned.Calls 'submission wait') 0 'completed-no-signed: wait skipped'
    Assert-Equal (Measure-Call $completedNoSigned.Calls 'submission download') 0 'completed-no-signed: download not attempted'
    Assert-True ($completedNoSigned.WaitOutput -like '*not ready to download*') 'completed-no-signed: refusal names the missing package'

    # A failed submission must be refused up front instead of burning the rest
    # of the pipeline.
    $failed = Invoke-CreateOnly -Name 'resume failed' -Overrides @{
        RESUME_PRODUCT_ID = '13872423721100349'; RESUME_SUBMISSION_ID = '1152921505701853748'
    } -SeedState @{
        productId = '13872423721100349'; submissionId = '1152921505701853748'; commitStatus = 'commitComplete'
        state = 'failed'; step = 'validation'; downloads = @('initialPackage'); uploaded = $true
    }
    Assert-True ($failed.ExitCode -ne 0) 'failed: create refuses a failed submission'
    Assert-True ($failed.Output -like '*already failed*') 'failed: refusal explains why'

    # Half-specified resume inputs are a mistake worth catching before any API
    # call happens.
    $halfResume = Invoke-CreateOnly -Name 'partial resume inputs' -Overrides @{
        RESUME_PRODUCT_ID = '13872423721100346'
    } -SeedState @{
        productId = ''; submissionId = ''; commitStatus = 'CommitPending'
        state = 'notStarted'; step = ''; downloads = @(); uploaded = $false
    }
    Assert-True ($halfResume.ExitCode -ne 0) 'partial resume: create fails'
    Assert-Equal $halfResume.Calls.Count 0 'partial resume: no sdcm call made'
    Assert-True ($halfResume.Output -like '*must be supplied together*') 'partial resume: message names both inputs'

    # A non-numeric id must never reach the API.
    $badId = Invoke-CreateOnly -Name 'non numeric resume id' -Overrides @{
        RESUME_PRODUCT_ID = 'not-an-id'; RESUME_SUBMISSION_ID = '1152921505701853745'
    } -SeedState @{
        productId = ''; submissionId = ''; commitStatus = 'CommitPending'
        state = 'notStarted'; step = ''; downloads = @(); uploaded = $false
    }
    Assert-True ($badId.ExitCode -ne 0) 'bad id: create fails'
    Assert-Equal $badId.Calls.Count 0 'bad id: no sdcm call made'
}
finally {
    $env:PATH = $originalPath
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Output ''
if ($script:Failures.Count -gt 0) {
    throw "PartnerSigning dry run failed $($script:Failures.Count) assertion(s): $($script:Failures -join '; ')"
}
Write-Output 'PartnerSigning dry run passed'
