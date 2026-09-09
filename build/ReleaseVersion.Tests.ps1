#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'ReleaseVersion.ps1')

function Assert-Equal($Actual, $Expected, [string] $Name) {
    if ($Actual -cne $Expected) {
        throw "FAIL ${Name}: expected '${Expected}', got '${Actual}'"
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

$release = ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0' -RunNumber 145 -BuildVersionOffset 2000
Assert-Equal $release.IsRelease $true 'tag is release'
Assert-Equal $release.Tag 'v3.6.0' 'tag name'
Assert-Equal $release.SetupVersion '3.6.0' 'setup version'
Assert-Equal $release.DriverVersion '3.6.0.2145' 'driver version uses offset plus run number'
Assert-Equal $release.Revision 2145 'revision'

$bare = ConvertTo-DsHidMiniReleaseVersion -Ref 'v3.6.0' -RunNumber 1 -BuildVersionOffset 2000
Assert-Equal $bare.DriverVersion '3.6.0.2001' 'bare tag ref'

$ci = ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/heads/master' -RunNumber 12 -BuildVersionOffset 2000
Assert-Equal $ci.IsRelease $false 'master is not a release'
Assert-Equal $ci.SetupVersion '' 'master has no setup version'
Assert-Equal $ci.DriverVersion '0.0.0.2012' 'master uses CI-only version'

Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0.1' -RunNumber 1 -BuildVersionOffset 2000 } 'four-part tag rejected'
Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0-pre1' -RunNumber 1 -BuildVersionOffset 2000 } 'prerelease tag rejected'
Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/setup-v3.6.0' -RunNumber 1 -BuildVersionOffset 2000 } 'setup tag rejected'
Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0' -RunNumber 0 -BuildVersionOffset 2000 } 'run number must be positive'
Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0' -RunNumber 1 -BuildVersionOffset -1 } 'negative offset rejected'
Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0' -RunNumber 1 -BuildVersionOffset 70000 } 'offset range rejected'
Assert-Throws { ConvertTo-DsHidMiniReleaseVersion -Ref 'refs/tags/v3.6.0' -RunNumber 50000 -BuildVersionOffset 20000 } 'revision overflow rejected'

$output = Join-Path ([IO.Path]::GetTempPath()) ("dshm-version-" + [guid]::NewGuid().ToString('N') + ".txt")
try {
    $lines = Write-DsHidMiniReleaseVersionOutputs -Version $release -GitHubOutput $output
    $text = [IO.File]::ReadAllText($output)
    if ($text -notmatch 'is-release=true') { throw 'FAIL github output is-release' }
    if ($text -notmatch 'driver-version=3.6.0.2145') { throw 'FAIL github output driver-version' }
    if ($text -notmatch 'setup-version=3.6.0') { throw 'FAIL github output setup-version' }
    Write-Output 'PASS github output file'
}
finally {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
}

Write-Output 'ReleaseVersion tests passed'
