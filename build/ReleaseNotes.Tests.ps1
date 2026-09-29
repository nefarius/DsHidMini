#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'ReleaseNotes.ps1')

function Assert-Equal($Actual, $Expected, [string] $Name) {
    if ($Actual -cne $Expected) {
        throw "FAIL ${Name}: expected '${Expected}', got '${Actual}'"
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

$identity = ConvertTo-DsHidMiniSetupReleaseTagIdentity -Tag 'setup-v3.2.0'
Assert-Equal $identity.Tag 'setup-v3.2.0' 'parses setup tag'
Assert-Equal $identity.Version ([version]'3.2.0') 'parses setup version'
Assert-Equal $identity.Revision 0 'base tag is revision 0'

$respin = ConvertTo-DsHidMiniSetupReleaseTagIdentity -Tag 'refs/tags/setup-v3.0.0-r6'
Assert-Equal $respin.Tag 'setup-v3.0.0-r6' 'strips refs/tags from respin'
Assert-Equal $respin.Revision 6 'parses respin revision'
Assert-Equal (ConvertTo-DsHidMiniSetupReleaseTagIdentity -Tag 'v3.2.0') $null 'ignores driver tags'
Assert-Equal (ConvertTo-DsHidMiniSetupReleaseTagIdentity -Tag 'setup-v3.2.0-beta') $null 'ignores non-numeric suffix'

$tags = @(
    'v3.2.0'
    'setup-v2.17.0'
    'setup-v3.0.0'
    'setup-v3.0.0-r6'
    'setup-v3.2.0'
    'not-a-tag'
)
Assert-Equal (Get-DsHidMiniPreviousSetupReleaseTag -CurrentTag 'setup-v3.2.0' -Tags $tags) 'setup-v3.0.0-r6' 'previous tag is last setup release'
Assert-Equal (Get-DsHidMiniPreviousSetupReleaseTag -CurrentTag 'setup-v3.0.0-r6' -Tags $tags) 'setup-v3.0.0' 'respin compares to previous respin or base'
Assert-Equal (Get-DsHidMiniPreviousSetupReleaseTag -CurrentTag 'setup-v3.0.0-r1' -Tags @('setup-v3.0.0', 'setup-v3.0.0-r1')) 'setup-v3.0.0' 'first respin uses base tag'
Assert-Equal (Get-DsHidMiniPreviousSetupReleaseTag -CurrentTag 'setup-v2.0.0' -Tags @('setup-v2.0.0')) $null 'first setup tag has no previous'
Assert-Equal (Get-DsHidMiniPreviousSetupReleaseTag -CurrentTag 'setup-v3.1.0' -Tags @('setup-v3.0.0-r2', 'setup-v3.2.0')) 'setup-v3.0.0-r2' 'skips newer setup tags'
Assert-Throws { Get-DsHidMiniPreviousSetupReleaseTag -CurrentTag 'v3.2.0' -Tags $tags } 'driver tag rejected as current'

$publishedReleases = @(
    [pscustomobject]@{ tag_name = 'setup-v3.17.2'; draft = $true; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.17.1'; draft = $true; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.17.0'; draft = $true; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.16.1'; draft = $true; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.16.0'; draft = $true; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.15.0'; draft = $false; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.5.1'; draft = $false; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.4.0'; draft = $false; prerelease = $true }
    [pscustomobject]@{ tag_name = 'setup-v3.0.0-r6'; draft = $false; prerelease = $false }
    [pscustomobject]@{ tag_name = 'setup-v3.0.0'; draft = $false; prerelease = $false }
    [pscustomobject]@{ tag_name = 'v2.2.282.0'; draft = $false; prerelease = $false }
)
Assert-Equal (Get-DsHidMiniPreviousPublishedSetupReleaseTag -CurrentTag 'setup-v3.17.2' -Releases $publishedReleases) 'setup-v3.15.0' 'v3.17.2 notes use latest published release, not draft tags'
Assert-Equal (Get-DsHidMiniPreviousPublishedSetupReleaseTag -CurrentTag 'setup-v3.16.0' -Releases $publishedReleases) 'setup-v3.15.0' 'draft 3.16.0 still baselines to published 3.15.0'
Assert-Equal (Get-DsHidMiniPreviousPublishedSetupReleaseTag -CurrentTag 'setup-v3.5.1' -Releases $publishedReleases) 'setup-v3.4.0' 'published prereleases remain a valid baseline'
Assert-Equal (Get-DsHidMiniPreviousPublishedSetupReleaseTag -CurrentTag 'setup-v3.0.0-r6' -Releases $publishedReleases) 'setup-v3.0.0' 'published respin compares to previous published base'
Assert-Equal (Get-DsHidMiniPreviousPublishedSetupReleaseTag -CurrentTag 'setup-v3.0.0' -Releases $publishedReleases) $null 'oldest published setup has no previous'
Assert-Equal (Get-DsHidMiniPreviousPublishedSetupReleaseTag -CurrentTag 'setup-v3.17.2' -Releases @(
        [pscustomobject]@{ tagName = 'setup-v3.17.1'; isDraft = $true }
        [pscustomobject]@{ tagName = 'setup-v3.15.0'; isDraft = $false }
    )) 'setup-v3.15.0' 'accepts tagName/isDraft aliases'
Assert-Equal (@(Get-DsHidMiniPublishedSetupReleaseTags -Releases $publishedReleases) -join ',') 'setup-v3.15.0,setup-v3.5.1,setup-v3.4.0,setup-v3.0.0-r6,setup-v3.0.0' 'published filter drops drafts and driver tags'

$template = @"
# v{{SetupVersion}} changelog

## ✨ Highlights

{{HIGHLIGHTS}}

{{WHATS_CHANGED}}
"@
$rendered = Format-DsHidMiniReleaseNotes `
    -Template $template `
    -SetupVersion '3.2.0' `
    -Highlights "- One change" `
    -WhatsChanged "## What's Changed`n* Title by @user in https://github.com/nefarius/DsHidMini/pull/1"
Assert-True ($rendered -match '# v3.2.0 changelog') 'renders version'
Assert-True ($rendered -match '- One change') 'renders highlights'
Assert-True ($rendered -match "What's Changed") 'renders generated notes'
Assert-Throws { Format-DsHidMiniReleaseNotes -Template $template -SetupVersion '3.2' -Highlights 'x' -WhatsChanged 'y' } 'short version rejected'

$generated = @"
## What's Changed
* Wrap the install-method description by @nefarius in https://github.com/nefarius/DsHidMini/pull/175
* diag: add structured PSM events by @nefarius in https://github.com/nefarius/DsHidMini/pull/177

**Full Changelog**: https://github.com/nefarius/DsHidMini/compare/setup-v3.0.0-r6...setup-v3.2.0
"@
Assert-True (Test-DsHidMiniGeneratedNotesHavePullRequests -GeneratedNotes $generated) 'detects PR lines'
Assert-True (-not (Test-DsHidMiniGeneratedNotesHavePullRequests -GeneratedNotes "## What's Changed`n")) 'empty generated notes have no PRs'
Assert-Equal (Get-DsHidMiniFallbackHighlights -SetupVersion '3.2.0' -PreviousSetupTag 'setup-v3.0.0-r6') '- Packaging or installer-only refresh of v3.2.0 with no additional pull requests since `setup-v3.0.0-r6`.' 'fallback mentions previous tag'

$ok = Assert-DsHidMiniHighlightsMarkdown -Highlights @"
## ✨ Highlights

This release focuses on diagnostics.

- Improved ETW diagnostics
- Cleaner idle-settings logging
"@
Assert-True ($ok -match 'Improved ETW diagnostics') 'accepts typical highlights'
Assert-True ($ok -notmatch '✨ Highlights') 'strips highlights heading'

Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights '' } 'empty highlights rejected'
Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights 'Just a sentence.' } 'highlights require a bullet'
Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights "- Fine`n`n## What's Changed`n* injected" } 'whats-changed injection rejected'
Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights "- Fine`n`n**Full Changelog**: https://example.test" } 'changelog injection rejected'
Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights "- Fine`n`n## Extra Section" } 'extra heading rejected'
Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights '- {{HIGHLIGHTS}}' } 'placeholder injection rejected'
Assert-Throws { Assert-DsHidMiniHighlightsMarkdown -Highlights '- <script>alert(1)</script>' } 'html injection rejected'

$draftView = ConvertFrom-DsHidMiniGitHubReleaseView -ExitCode 0 -Stdout '{"isDraft":true,"tagName":"setup-v3.2.0","name":"DsHidMini Driver v3.2.0"}' -Stderr ''
Assert-True ([bool]$draftView.isDraft) 'release view JSON exposes isDraft'
Assert-Equal $draftView.tagName 'setup-v3.2.0' 'release view JSON exposes tagName'
Assert-Equal (ConvertFrom-DsHidMiniGitHubReleaseView -ExitCode 1 -Stdout '' -Stderr 'release not found') $null 'missing release is null'
Assert-Equal (ConvertFrom-DsHidMiniGitHubReleaseView -ExitCode 1 -Stdout '' -Stderr 'HTTP 404: Not Found') $null 'HTTP 404 is treated as missing'
Assert-Throws { ConvertFrom-DsHidMiniGitHubReleaseView -ExitCode 1 -Stdout '' -Stderr 'HTTP 401: Bad credentials' } 'auth failure is not treated as missing'
Assert-Throws { ConvertFrom-DsHidMiniGitHubReleaseView -ExitCode 1 -Stdout '' -Stderr 'dial tcp: lookup api.github.com' } 'network failure is not treated as missing'
Assert-Throws { ConvertFrom-DsHidMiniGitHubReleaseView -ExitCode 0 -Stdout '' -Stderr '' } 'empty successful view is rejected'

Assert-Equal (Resolve-DsHidMiniDraftReleaseAction -ExistingRelease $null) 'create' 'missing release creates draft'
Assert-Equal (Resolve-DsHidMiniDraftReleaseAction -ExistingRelease ([pscustomobject]@{ draft = $true; tag_name = 'setup-v3.2.0' })) 'update' 'draft is updated'
Assert-Equal (Resolve-DsHidMiniDraftReleaseAction -ExistingRelease ([pscustomobject]@{ isDraft = $true })) 'update' 'isDraft alias is accepted'
Assert-Throws { Resolve-DsHidMiniDraftReleaseAction -ExistingRelease ([pscustomobject]@{ draft = $false; tag_name = 'setup-v3.2.0' }) } 'published release is protected'

$temp = Join-Path ([IO.Path]::GetTempPath()) ("dshidmini-notes-tests-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    $templatePath = Join-Path $temp 'release-notes.md'
    [IO.File]::WriteAllText($templatePath, $template, [Text.UTF8Encoding]::new($false))
    $notes = New-DsHidMiniReleaseNotes `
        -SetupVersion '3.2.0' `
        -SetupTag 'setup-v3.2.0' `
        -Repository 'nefarius/DsHidMini' `
        -TemplatePath $templatePath `
        -SetupTags $tags `
        -GenerateNotes { param($Repository, $SetupTag, $Previous) "$Repository $SetupTag $Previous`n* Title by @user in https://github.com/nefarius/DsHidMini/pull/1" } `
        -HighlightsProvider { '- From provider' }
    Assert-Equal $notes.PreviousPublishedSetupTag 'setup-v3.0.0-r6' 'generator selects previous published setup tag'
    Assert-Equal $notes.Title 'DsHidMini Driver v3.2.0' 'generator title'
    Assert-True ($notes.Body -match '- From provider') 'injectable highlights used when PRs exist'
    Assert-True ($notes.WhatsChanged -match 'setup-v3.0.0-r6') 'generate-notes receives previous published tag'

    $cumulative = New-DsHidMiniReleaseNotes `
        -SetupVersion '3.17.2' `
        -SetupTag 'setup-v3.17.2' `
        -Repository 'nefarius/DsHidMini' `
        -TemplatePath $templatePath `
        -Releases $publishedReleases `
        -GenerateNotes { param($Repository, $SetupTag, $Previous) "$Repository $SetupTag $Previous`n* Title by @user in https://github.com/nefarius/DsHidMini/pull/1" } `
        -HighlightsProvider { '- From provider' }
    Assert-Equal $cumulative.PreviousPublishedSetupTag 'setup-v3.15.0' 'generator skips draft 3.16/3.17 tags'
    Assert-True ($cumulative.WhatsChanged -match 'setup-v3.15.0') 'generate-notes receives published 3.15.0 baseline'

    $fallbackNotes = New-DsHidMiniReleaseNotes `
        -SetupVersion '3.2.0' `
        -SetupTag 'setup-v3.2.0' `
        -Repository 'nefarius/DsHidMini' `
        -TemplatePath $templatePath `
        -SetupTags $tags `
        -GenerateNotes { "## What's Changed" } `
        -HighlightsProvider { throw 'Copilot should not run when there are no PRs' }
    Assert-True ($fallbackNotes.Highlights -match 'no additional pull requests') 'empty PR history uses fallback highlights'

    $repoRootTemplate = Join-Path (Split-Path -Parent $here) '.github\release-notes.md'
    $fromRepo = Get-DsHidMiniReleaseNotesTemplate -Path $repoRootTemplate
    Assert-True ($fromRepo -match 'DsHidMini changelog') 'repo template has product title'
    Assert-True ($fromRepo -match '## Remarks') 'repo template has remarks'
    Assert-True ($fromRepo -match 'How to install/update/uninstall') 'repo template has install section'
    Assert-True ($fromRepo -match '\{\{HIGHLIGHTS\}\}') 'repo template has highlights placeholder'
    Assert-True ($fromRepo -match '\{\{WHATS_CHANGED\}\}') 'repo template has generated-notes placeholder'

    $metadataPath = Join-Path $temp 'setup-metadata.json'
    $msiPath = Join-Path $temp 'Nefarius_DsHidMini_Drivers_x64_arm64_v3.2.0.msi'
    [IO.File]::WriteAllText($msiPath, 'msi-bytes', [Text.UTF8Encoding]::new($false))
    $sha = (Get-FileHash -LiteralPath $msiPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $metadata = [pscustomobject]@{
        setupTag     = 'setup-v3.2.0'
        setupVersion = '3.2.0'
        files        = [pscustomobject]@{
            msi = [pscustomobject]@{
                name   = 'Nefarius_DsHidMini_Drivers_x64_arm64_v3.2.0.msi'
                sha256 = $sha
            }
        }
    }
    [IO.File]::WriteAllText($metadataPath, ($metadata | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
    $read = Read-DsHidMiniSetupMetadataFile -Path $temp
    $artifact = Assert-DsHidMiniSetupReleaseArtifact -Metadata $read -ArtifactDirectory $temp
    Assert-Equal $artifact.MsiName 'Nefarius_DsHidMini_Drivers_x64_arm64_v3.2.0.msi' 'artifact name'
    Assert-Equal $artifact.MsiSha256 $sha 'artifact hash'

    $bad = $metadata | ConvertTo-Json -Depth 5 | ConvertFrom-Json
    $bad.files.msi.sha256 = '0' * 64
    Assert-Throws { Assert-DsHidMiniSetupReleaseArtifact -Metadata $bad -ArtifactDirectory $temp } 'hash mismatch rejected'

    $notesPath = Join-Path $temp 'notes.md'
    Export-DsHidMiniReleaseNotes -Path $notesPath -Body $notes.Body
    $bytes = [IO.File]::ReadAllBytes($notesPath)
    Assert-True (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)) 'notes file has no UTF-8 BOM'

    $created = [System.Collections.Generic.List[object]]::new()
    $createAction = Publish-DsHidMiniDraftRelease `
        -Repository 'nefarius/DsHidMini' `
        -Tag 'setup-v3.2.0' `
        -Title $notes.Title `
        -NotesPath $notesPath `
        -MsiPath $msiPath `
        -GetExistingRelease { $null } `
        -GitHubCommand { param($Arguments) $created.Add(@($Arguments)) }
    Assert-Equal $createAction 'create' 'publish creates missing draft'
    Assert-Equal $created[0][1] 'create' 'create uses gh release create'

    $updated = [System.Collections.Generic.List[object]]::new()
    $updateAction = Publish-DsHidMiniDraftRelease `
        -Repository 'nefarius/DsHidMini' `
        -Tag 'setup-v3.2.0' `
        -Title $notes.Title `
        -NotesPath $notesPath `
        -MsiPath $msiPath `
        -GetExistingRelease { [pscustomobject]@{ draft = $true; tag_name = 'setup-v3.2.0' } } `
        -GitHubCommand { param($Arguments) $updated.Add(@($Arguments)) }
    Assert-Equal $updateAction 'update' 'publish updates existing draft'
    Assert-Equal $updated[0][1] 'edit' 'update edits notes'
    Assert-Equal $updated[1][1] 'upload' 'update reclobbers MSI'

    Assert-Throws {
        Publish-DsHidMiniDraftRelease `
            -Repository 'nefarius/DsHidMini' `
            -Tag 'setup-v3.2.0' `
            -Title $notes.Title `
            -NotesPath $notesPath `
            -MsiPath $msiPath `
            -GetExistingRelease { [pscustomobject]@{ draft = $false; tag_name = 'setup-v3.2.0' } } `
            -GitHubCommand { throw 'gh must not run for a published release' }
    } 'publish refuses a published release'
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Output 'PASS ReleaseNotes.Tests.ps1'
