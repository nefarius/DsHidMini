#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'SetupRelease.ps1')

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

function New-TestPublisherCertificate {
    param(
        [string] $SimpleName,
        [string] $Subject = '',
        [string] $Thumbprint = 'DEADBEEF'
    )

    $cert = [pscustomobject]@{
        Subject          = $Subject
        Thumbprint       = $Thumbprint
        SimpleNameValue  = $SimpleName
    }
    $cert | Add-Member -MemberType ScriptMethod -Name GetNameInfo -Value {
        param($nameType, $forIssuer)
        if ($nameType -ne [System.Security.Cryptography.X509Certificates.X509NameType]::SimpleName -or $forIssuer -ne $false) {
            throw "GetNameInfo expected SimpleName and forIssuer=`$false, got nameType='$nameType' forIssuer='$forIssuer'."
        }
        [string]$this.SimpleNameValue
    }
    return $cert
}

function New-TestDriverPackage {
    param([string] $Root)

    New-Item -ItemType Directory -Force -Path @(
        (Join-Path $Root 'x64')
        (Join-Path $Root 'ARM64')
    ) | Out-Null
    Set-Content -LiteralPath (Join-Path $Root 'dshidmini.inf') -Value 'inf' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $Root 'dshidmini.cat') -Value 'cat' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $Root 'x64\dshidmini.dll') -Value 'dll' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $Root 'ARM64\dshidmini.dll') -Value 'dll' -Encoding utf8
    return $Root
}

$identity = ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag 'v2.12.0'
Assert-Equal $identity.DriverTag 'v2.12.0' 'identity tag'
Assert-Equal $identity.SetupVersion '2.12.0' 'identity setup version'
Assert-Equal $identity.SetupTagBase 'setup-v2.12.0' 'identity setup tag base'

$fromRef = ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag 'refs/tags/v2.12.0'
Assert-Equal $fromRef.SetupVersion '2.12.0' 'identity strips refs/tags prefix'

Assert-Throws { ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag '2.12.0' } 'bare version rejected'
Assert-Throws { ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag 'setup-v2.12.0' } 'setup tag rejected as driver tag'
Assert-Throws { ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag 'v2.12.0.1' } 'four-part tag rejected'
Assert-Throws { ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag 'v2.12.0-pre1' } 'prerelease tag rejected'
Assert-Throws { ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag 'v2.12.0-r1' } 'revision suffix rejected as driver tag'

Assert-Equal (Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12.0' -ExistingTags @()) 'setup-v2.12.0' 'first setup tag'
Assert-Equal (Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12.0' -ExistingTags @('setup-v2.12.0')) 'setup-v2.12.0-r1' 'first re-spin'
Assert-Equal (Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12.0' -ExistingTags @('setup-v2.12.0', 'setup-v2.12.0-r1')) 'setup-v2.12.0-r2' 'second re-spin'
Assert-Equal (Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12.0' -ExistingTags @('setup-v2.12.0', 'setup-v2.12.0-r2')) 'setup-v2.12.0-r1' 'fills revision gap'
Assert-Equal (Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12.0' -ExistingTags @('setup-v2.12.1', 'v2.12.0', 'setup-v2.12.0-beta')) 'setup-v2.12.0' 'unrelated tags ignored'
Assert-Equal (Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12.0' -ExistingTags @('refs/tags/setup-v2.12.0')) 'setup-v2.12.0-r1' 'refs/tags prefix occupies base tag'
Assert-Throws { Get-DsHidMiniNextSetupReleaseTag -SetupVersion '2.12' -ExistingTags @() } 'short setup version rejected'

$metadata = [pscustomobject]@{
    tag           = 'v2.12.0'
    setupVersion  = '2.12.0'
    driverVersion = '2.12.0.2012'
    commit        = '0123456789abcdef0123456789abcdef01234567'
}
Assert-DsHidMiniReleaseMetadataMatchesTag -Metadata $metadata -DriverTag 'v2.12.0'
Write-Output 'PASS metadata matches driver tag'

$badTag = [pscustomobject]@{
    tag           = 'v2.11.0'
    setupVersion  = '2.11.0'
    driverVersion = '2.11.0.2012'
    commit        = $metadata.commit
}
Assert-Throws { Assert-DsHidMiniReleaseMetadataMatchesTag -Metadata $badTag -DriverTag 'v2.12.0' } 'metadata tag mismatch'

$badDriver = [pscustomobject]@{
    tag           = 'v2.12.0'
    setupVersion  = '2.12.0'
    driverVersion = '2.11.0.2012'
    commit        = $metadata.commit
}
Assert-Throws { Assert-DsHidMiniReleaseMetadataMatchesTag -Metadata $badDriver -DriverTag 'v2.12.0' } 'metadata driver version mismatch'

$badCommit = [pscustomobject]@{
    tag           = 'v2.12.0'
    setupVersion  = '2.12.0'
    driverVersion = '2.12.0.2012'
    commit        = 'abc'
}
Assert-Throws { Assert-DsHidMiniReleaseMetadataMatchesTag -Metadata $badCommit -DriverTag 'v2.12.0' } 'short commit rejected'

Assert-Equal (Get-DsHidMiniSetupMsiFileName -SetupVersion '2.12.0') 'Nefarius_DsHidMini_Drivers_x64_arm64_v2.12.0.msi' 'msi file name'

Assert-True (Test-DsHidMiniIsAllowlistedPublisher -Certificate (New-TestPublisherCertificate `
            -SimpleName 'Nefarius Software Solutions e.U.' `
            -Subject 'CN=Nefarius Software Solutions e.U., O=Nefarius Software Solutions e.U.')) 'allowlists publisher by simple name'
Assert-True (-not (Test-DsHidMiniIsAllowlistedPublisher -Certificate (New-TestPublisherCertificate `
                -SimpleName 'Some Other Signer' `
                -Subject 'CN=Some Other Signer'))) 'rejects signer that is not allowlisted'
Assert-True (-not (Test-DsHidMiniIsAllowlistedPublisher -Certificate (New-TestPublisherCertificate `
                -SimpleName 'Evil Corp' `
                -Subject 'CN=Evil Corp, OU=Nefarius Software Solutions e.U.'))) 'rejects subject substring that is not the simple name'
Assert-Throws {
    Assert-DsHidMiniAllowlistedPublisher `
        -Certificate (New-TestPublisherCertificate -SimpleName 'Some Other Signer' -Subject 'CN=Some Other Signer') `
        -Path 'setup.msi'
} 'unsigned-identity MSI signer rejected'
Assert-Equal (ConvertFrom-DsHidMiniMsiName -Value 'CONTRO~1.EXE|ControlApp.exe') 'ControlApp.exe' 'decodes MSI long file name'
Assert-True (Test-DsHidMiniMsiNamePresent -Values @('CONTRO~1.EXE|ControlApp.exe') -Expected 'ControlApp.exe') 'MSI name present after decode'
# Windows Installer stores shortcuts as "<short>.lnk|<long>.lnk".
Assert-True (Test-DsHidMiniMsiNamePresent `
        -Values @('-sd7_iyj.lnk|DsHidMini Control App.lnk') `
        -Expected 'DsHidMini Control App') 'shortcut matches expectation without .lnk'
Assert-True (Test-DsHidMiniMsiNamePresent `
        -Values @('-sd7_iyj.lnk|DsHidMini Control App.lnk') `
        -Expected 'DsHidMini Control App.lnk') 'shortcut matches expectation with .lnk'
Assert-True (-not (Test-DsHidMiniMsiNamePresent `
            -Values @('-sd7_iyj.lnk|Some Other Shortcut.lnk') `
            -Expected 'DsHidMini Control App')) 'unrelated shortcut is not matched'

Assert-Equal (ConvertTo-DsHidMiniSetupVersionFromTag -Tag 'setup-v2.17.0') ([version]'2.17.0') 'parses plain setup tag'
Assert-Equal (ConvertTo-DsHidMiniSetupVersionFromTag -Tag 'setup-v2.12.0-r3') ([version]'2.12.0') 'parses re-spin tag, drops -rN'
Assert-Equal (ConvertTo-DsHidMiniSetupVersionFromTag -Tag 'setup-v2.10.371.0') ([version]'2.10.371') 'parses legacy 4-component tag as ProductVersion'
Assert-Equal (ConvertTo-DsHidMiniSetupVersionFromTag -Tag 'refs/tags/setup-v2.17.0') ([version]'2.17.0') 'strips refs/tags prefix'
Assert-Equal (ConvertTo-DsHidMiniSetupVersionFromTag -Tag 'v2.17.0') $null 'ignores non-setup tags'
Assert-Equal (ConvertTo-DsHidMiniSetupVersionFromTag -Tag 'setup-v2.17.0-beta') $null 'ignores non-numeric suffix'

Assert-Equal (Get-DsHidMiniHighestSetupVersion -Tags @()) $null 'highest version of empty set is null'
Assert-Equal (Get-DsHidMiniHighestSetupVersion -Tags @('setup-v2.12.0', 'setup-v2.17.0', 'setup-v2.9.336')) ([version]'2.17.0') 'highest version picks max'
Assert-Equal (Get-DsHidMiniHighestSetupVersion -Tags @('setup-v2.10.371.0', 'setup-v2.10.371', 'not-a-tag')) ([version]'2.10.371') 'highest version ignores unrelated tags'
$legacyHighest = Get-DsHidMiniHighestSetupVersion -Tags @('setup-v2.10.371.0')
Assert-Equal $legacyHighest ([version]'2.10.371') 'legacy four-component tag normalizes to ProductVersion'
Assert-DsHidMiniSetupVersionNotRegressed -SetupVersion '2.10.371' -HighestPublishedVersion $legacyHighest
Write-Output 'PASS regression guard treats setup-v2.10.371.0 as ProductVersion 2.10.371'

Assert-DsHidMiniSetupVersionNotRegressed -SetupVersion '2.17.0' -HighestPublishedVersion $null
Write-Output 'PASS regression guard allows first-ever setup version'
Assert-DsHidMiniSetupVersionNotRegressed -SetupVersion '2.18.0' -HighestPublishedVersion ([version]'2.17.0')
Write-Output 'PASS regression guard allows newer version'
Assert-DsHidMiniSetupVersionNotRegressed -SetupVersion '2.17.0' -HighestPublishedVersion ([version]'2.17.0')
Write-Output 'PASS regression guard allows re-spin of the current highest version'
Assert-Throws { Assert-DsHidMiniSetupVersionNotRegressed -SetupVersion '2.13.0' -HighestPublishedVersion ([version]'2.17.0') } 'regression guard rejects older version'
Assert-Throws { Assert-DsHidMiniSetupVersionNotRegressed -SetupVersion '2.12' -HighestPublishedVersion $null } 'regression guard rejects malformed setup version'

$provenance = New-DsHidMiniSetupProvenance `
    -DriverTag 'v2.12.0' `
    -SetupVersion '2.12.0' `
    -SetupTag 'setup-v2.12.0-r1' `
    -DriverVersion '2.12.0.2012' `
    -DriverCommit $metadata.commit `
    -SetupCommit 'fedcba9876543210fedcba9876543210fedcba98' `
    -BuildRunId 11 `
    -DriversRunId 22 `
    -Repository 'nefarius/DsHidMini' `
    -MsiName 'Nefarius_DsHidMini_Drivers_x64_arm64_v2.12.0.msi' `
    -MsiSha256 ('a' * 64)
Assert-Equal $provenance.setupTag 'setup-v2.12.0-r1' 'provenance setup tag'
Assert-Equal $provenance.files.msi.sha256 ('a' * 64) 'provenance msi hash'
try {
    New-DsHidMiniSetupProvenance `
        -DriverTag 'v2.12.0' `
        -SetupVersion '2.12.0' `
        -SetupTag 'setup-v2.12.1' `
        -DriverVersion '2.12.0.2012' `
        -DriverCommit $metadata.commit `
        -SetupCommit $metadata.commit `
        -BuildRunId 11 `
        -DriversRunId 22 `
        -Repository 'nefarius/DsHidMini' `
        -MsiName 'Nefarius_DsHidMini_Drivers_x64_arm64_v2.12.0.msi' `
        -MsiSha256 ('a' * 64)
    throw 'FAIL provenance rejects foreign setup tag: expected an exception'
}
catch {
    if ($_.Exception.Message -eq 'FAIL provenance rejects foreign setup tag: expected an exception') {
        throw
    }
    if ($_.Exception.Message -cne "Setup tag 'setup-v2.12.1' is not setup-v2.12.0 or a -rN re-spin.") {
        throw "FAIL provenance interpolates SetupTagBase: $($_.Exception.Message)"
    }
    Write-Output 'PASS provenance rejects foreign setup tag'
    Write-Output 'PASS provenance interpolates SetupTagBase'
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("dshidmini-setup-tests-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
try {
    $jsonPath = Join-Path $tempRoot 'setup-metadata.json'
    Write-DsHidMiniUtf8NoBomJson -Object $provenance -Path $jsonPath
    $bytes = [IO.File]::ReadAllBytes($jsonPath)
    Assert-True ($bytes.Length -gt 0 -and $bytes[0] -ne 0xEF) 'provenance json has no BOM'
    $roundTrip = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
    Assert-Equal $roundTrip.setupTag 'setup-v2.12.0-r1' 'provenance json round trip'

    $drivers = New-TestDriverPackage (Join-Path $tempRoot 'drivers-src')
    $tools = Join-Path $tempRoot 'tools-src\bin'
    New-Item -ItemType Directory -Force -Path $tools | Out-Null
    Set-Content -LiteralPath (Join-Path $tools 'ControlApp.exe') -Value 'ui' -Encoding utf8
    $artifacts = Join-Path $tempRoot 'artifacts'
    Copy-DsHidMiniSetupPayload -DriversSource $drivers -ToolsSource (Join-Path $tempRoot 'tools-src') -ArtifactsRoot $artifacts
    Assert-DsHidMiniSetupPayload -ArtifactsRoot $artifacts
    Write-Output 'PASS staged payload layout'

    Remove-Item -LiteralPath (Join-Path $artifacts 'drivers\x64\dshidmini.dll') -Force
    Assert-Throws { Assert-DsHidMiniSetupPayload -ArtifactsRoot $artifacts } 'incomplete payload rejected'

    $nested = Join-Path $tempRoot 'nested\dshidmini-microsoft-drivers'
    New-TestDriverPackage $nested | Out-Null
    $flatTools = Join-Path $tempRoot 'flat-tools'
    New-Item -ItemType Directory -Force -Path $flatTools | Out-Null
    Set-Content -LiteralPath (Join-Path $flatTools 'ControlApp.exe') -Value 'ui' -Encoding utf8
    $artifacts2 = Join-Path $tempRoot 'artifacts2'
    Copy-DsHidMiniSetupPayload -DriversSource (Split-Path -Parent $nested) -ToolsSource $flatTools -ArtifactsRoot $artifacts2
    Assert-DsHidMiniSetupPayload -ArtifactsRoot $artifacts2
    Write-Output 'PASS nested drivers and flat tools'

    $emptyTools = Join-Path $tempRoot 'empty-tools'
    New-Item -ItemType Directory -Force -Path $emptyTools | Out-Null
    Assert-Throws {
        Copy-DsHidMiniSetupPayload -DriversSource $drivers -ToolsSource $emptyTools -ArtifactsRoot (Join-Path $tempRoot 'artifacts3')
    } 'missing tools rejected'

    $requiredCa = Get-DsHidMiniRequiredCustomActionAssemblies
    Assert-True ($requiredCa -contains 'CliWrap.dll') 'required CA assemblies include CliWrap.dll'
    Assert-True ($requiredCa -contains 'Nefarius.Utilities.DeviceManagement.dll') 'required CA assemblies include DeviceManagement'
    Assert-True ($requiredCa -contains 'System.Numerics.Vectors.dll') 'required CA assemblies include System.Numerics.Vectors'
    Assert-True ($requiredCa -contains 'Newtonsoft.Json.dll') 'required CA assemblies include Newtonsoft.Json'

    $complete = @($requiredCa + 'DsHidMini.Installer.dll' + 'CustomActions.config')
    Assert-DsHidMiniCustomActionPackageFiles -PackageFiles $complete
    Write-Output 'PASS complete custom-action file list accepted'

    $extensionless = @($requiredCa | ForEach-Object {
            if ($_ -eq 'CliWrap.dll') { 'CliWrap' } else { $_ }
        })
    Assert-DsHidMiniCustomActionPackageFiles -PackageFiles $extensionless
    Write-Output 'PASS extensionless CliWrap name accepted'

    Assert-Throws {
        Assert-DsHidMiniCustomActionPackageFiles -PackageFiles @('DsHidMini.Installer.dll', 'CustomActions.config')
    } 'custom-action list missing CliWrap rejected'

    Assert-Throws {
        Assert-DsHidMiniCustomActionPackageFiles -PackageFiles $complete -ExpectedAssemblies @('Totally.New.Dependency.dll')
    } 'transitive dependency from build manifest enforced'

    $manifestPath = Join-Path $tempRoot 'ca-support-assemblies.txt'
    Set-Content -LiteralPath $manifestPath -Value @('CliWrap.dll', '', '  System.Numerics.Vectors.dll  ') -Encoding utf8
    $manifestNames = Get-DsHidMiniCustomActionManifestAssemblies -ManifestPath $manifestPath
    Assert-Equal $manifestNames.Count 2 'manifest skips blank lines'
    Assert-True ($manifestNames -contains 'System.Numerics.Vectors.dll') 'manifest entries are trimmed'

    Assert-Throws {
        Get-DsHidMiniCustomActionBinaryName -MsiPath (Join-Path $tempRoot 'absent.msi') -ActionName 'Install Drivers'
    } 'custom action name with spaces rejected'

    Assert-Throws {
        Get-DsHidMiniCustomActionManifestAssemblies -ManifestPath (Join-Path $tempRoot 'absent-manifest.txt')
    } 'missing custom-action manifest rejected'

    $emptyManifest = Join-Path $tempRoot 'empty-manifest.txt'
    Set-Content -LiteralPath $emptyManifest -Value '' -Encoding utf8
    Assert-Throws {
        Get-DsHidMiniCustomActionManifestAssemblies -ManifestPath $emptyManifest
    } 'empty custom-action manifest rejected'

    $cabSource = Join-Path $tempRoot 'cab-src'
    $cabOut = Join-Path $tempRoot 'cab-out'
    New-Item -ItemType Directory -Force -Path @($cabSource, $cabOut) | Out-Null
    Set-Content -LiteralPath (Join-Path $cabSource 'CliWrap.dll') -Value 'cliwrap' -Encoding utf8
    Set-Content -LiteralPath (Join-Path $cabSource 'Newtonsoft.Json.dll') -Value 'json' -Encoding utf8
    $ddf = Join-Path $tempRoot 'package.ddf'
    @(
        '.OPTION EXPLICIT'
        ".Set CabinetNameTemplate=package.cab"
        ".Set DiskDirectoryTemplate=$cabOut"
        '.Set Cabinet=ON'
        '.Set Compress=ON'
        ('"{0}"' -f (Join-Path $cabSource 'CliWrap.dll'))
        ('"{0}"' -f (Join-Path $cabSource 'Newtonsoft.Json.dll'))
    ) | Set-Content -LiteralPath $ddf -Encoding ascii
    $makecab = Join-Path $env:WINDIR 'System32\makecab.exe'
    $null = & $makecab /F $ddf
    $cab = Join-Path $cabOut 'package.cab'
    Assert-True (Test-Path -LiteralPath $cab -PathType Leaf) 'test cabinet created'

    $sfx = Join-Path $tempRoot 'InstallDrivers_File.bin'
    $stub = [Text.Encoding]::ASCII.GetBytes('SfxCA-stub')
    $cabBytes = [IO.File]::ReadAllBytes($cab)
    $payload = New-Object byte[] ($stub.Length + $cabBytes.Length)
    [Array]::Copy($stub, 0, $payload, 0, $stub.Length)
    [Array]::Copy($cabBytes, 0, $payload, $stub.Length, $cabBytes.Length)
    [IO.File]::WriteAllBytes($sfx, $payload)

    $names = Get-DsHidMiniSfxCaCabinetFileNames -Path $sfx
    Assert-True ($names -contains 'CliWrap.dll') 'SfxCA cabinet lister finds CliWrap.dll'
    Assert-True ($names -contains 'Newtonsoft.Json.dll') 'SfxCA cabinet lister finds Newtonsoft.Json.dll'

    Assert-Throws {
        Get-DsHidMiniSfxCaCabinetFileNames -Path (Join-Path $tempRoot 'missing.bin')
    } 'missing SfxCA payload rejected'
    $emptyPayload = Join-Path $tempRoot 'empty.bin'
    [IO.File]::WriteAllBytes($emptyPayload, [Text.Encoding]::ASCII.GetBytes('not-a-cabinet'))
    Assert-Throws { Get-DsHidMiniSfxCaCabinetFileNames -Path $emptyPayload } 'payload without cabinet rejected'

    $lfsRoot = Join-Path $tempRoot 'lfs-repo'
    $payloadRelative = @('setup\nefcon\x64\nefconc.exe', 'setup\nefarius_DsHidMini_Updater.exe')
    foreach ($relative in $payloadRelative) {
        $path = Join-Path $lfsRoot $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
        [IO.File]::WriteAllBytes($path, (New-Object byte[] 4096))
    }
    Assert-DsHidMiniNoGitLfsPointers -RepositoryRoot $lfsRoot -RelativePaths $payloadRelative
    Write-Output 'PASS materialized payload binaries accepted'

    $pointerText = @(
        'version https://git-lfs.github.com/spec/v1'
        'oid sha256:b65013f08bef9d0ddcdeef7501fc6be346478b7b29b7730de97c496408ddf9b4'
        'size 1054688'
    ) -join "`n"
    [IO.File]::WriteAllText((Join-Path $lfsRoot $payloadRelative[0]), $pointerText)
    Assert-True (Test-DsHidMiniIsGitLfsPointer -Path (Join-Path $lfsRoot $payloadRelative[0])) 'pointer stub detected'
    Assert-Throws {
        Assert-DsHidMiniNoGitLfsPointers -RepositoryRoot $lfsRoot -RelativePaths $payloadRelative
    } 'Git LFS pointer payload rejected'
    Assert-Throws {
        Assert-DsHidMiniNoGitLfsPointers -RepositoryRoot $lfsRoot -RelativePaths @('setup\does-not-exist.exe')
    } 'missing payload file rejected'

    Assert-DsHidMiniMsiBinariesVersioned -PayloadFiles @(
        [pscustomobject]@{ FileName = 'nefconc.exe'; FileSize = 1054688; Version = '1.20.0.0' }
        [pscustomobject]@{ FileName = 'dshidmini.dll'; FileSize = 67712; Version = '3.6.0.2145' }
        [pscustomobject]@{ FileName = 'LICENSE'; FileSize = 1574; Version = '' }
        [pscustomobject]@{ FileName = 'igfilter.inf'; FileSize = 2908; Version = '' }
        [pscustomobject]@{ FileName = 'DsHidMini.man'; FileSize = 12453; Version = '' }
    )
    Write-Output 'PASS versioned MSI binaries accepted, unversioned data files ignored'

    Assert-Throws {
        Assert-DsHidMiniMsiBinariesVersioned -PayloadFiles @(
            [pscustomobject]@{ FileName = 'nefconc.exe'; FileSize = 132; Version = '' }
        )
    } 'unversioned MSI executable rejected'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Output 'SetupRelease tests passed'
