#Requires -Version 7.0
<#
.SYNOPSIS
    Resolves setup tags, stages attested payloads, and writes setup provenance.

.DESCRIPTION
    Driver tags stay vMAJOR.MINOR.PATCH. The first setup tag for that payload is
    setup-vMAJOR.MINOR.PATCH; later setup-only re-spins use -r1, -r2, and so on.
    The MSI product version remains MAJOR.MINOR.PATCH.

    Assert-DsHidMiniSetupVersionNotRegressed guards against dispatching a driver
    tag whose MAJOR.MINOR.PATCH is lower than a setup version already
    published (setup-v* tag anywhere in history, not just re-spins of the
    current version). Windows Installer treats a lower ProductVersion as a
    downgrade and blocks it, so this must fail fast instead of quietly
    producing an MSI nobody who already updated can install.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DsHidMiniRequiredAttestedDriverFiles {
    @(
        'dshidmini.inf'
        'dshidmini.cat'
        'x64\dshidmini.dll'
        'ARM64\dshidmini.dll'
    )
}

function Get-DsHidMiniRequiredAttestedDriverBinaries {
    @(
        'x64\dshidmini.dll'
        'ARM64\dshidmini.dll'
    )
}

function Get-DsHidMiniSetupMsiFileName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $SetupVersion
    )

    if ($SetupVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "SetupVersion must be MAJOR.MINOR.PATCH. Got: '$SetupVersion'."
    }

    "Nefarius_DsHidMini_Drivers_x64_arm64_v$SetupVersion.msi"
}

function ConvertTo-DsHidMiniSetupReleaseIdentity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string] $DriverTag
    )

    $tag = $DriverTag.Trim()
    if ($tag -match '^refs/tags/(.+)$') {
        $tag = $Matches[1]
    }

    if ($tag -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
        throw "Driver tags must be vMAJOR.MINOR.PATCH (example: v2.12.0). Got: '$DriverTag'."
    }

    $setup = $tag.Substring(1)
    return [pscustomobject]@{
        DriverTag     = $tag
        SetupVersion  = $setup
        SetupTagBase  = "setup-v$setup"
    }
}

function Get-DsHidMiniNextSetupReleaseTag {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $SetupVersion,

        [AllowEmptyCollection()]
        [AllowNull()]
        [string[]] $ExistingTags = @()
    )

    if ($SetupVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "SetupVersion must be MAJOR.MINOR.PATCH. Got: '$SetupVersion'."
    }

    if ($null -eq $ExistingTags) {
        $ExistingTags = @()
    }

    $base = "setup-v$SetupVersion"
    $escaped = [regex]::Escape($SetupVersion)
    $occupied = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($raw in $ExistingTags) {
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        $name = $raw.Trim()
        if ($name -match '^refs/tags/(.+)$') {
            $name = $Matches[1]
        }

        if ($name -eq $base -or $name -match "^setup-v$escaped-r([1-9][0-9]*)$") {
            [void]$occupied.Add($name)
        }
    }

    if (-not $occupied.Contains($base)) {
        return $base
    }

    $revision = 1
    while ($occupied.Contains("$base-r$revision")) {
        $revision++
    }

    return "$base-r$revision"
}

function ConvertTo-DsHidMiniSetupVersionFromTag {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string] $Tag
    )

    $name = $Tag.Trim()
    if ($name -match '^refs/tags/(.+)$') {
        $name = $Matches[1]
    }

    # Accepts both the current setup-vMAJOR.MINOR.PATCH[-rN] scheme and legacy
    # setup-v* tags with an extra 4th component (e.g. setup-v2.10.371.0). The
    # fourth component is dropped so comparison uses the three-part
    # ProductVersion that SetupVersion / MSI actually ship.
    if ($name -notmatch '^setup-v(\d+)\.(\d+)\.(\d+)(?:\.\d+)?(?:-r[1-9][0-9]*)?$') {
        return $null
    }

    return [version]"$($Matches[1]).$($Matches[2]).$($Matches[3])"
}

function Get-DsHidMiniHighestSetupVersion {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [AllowEmptyCollection()]
        [string[]] $Tags = @()
    )

    if ($null -eq $Tags) {
        $Tags = @()
    }

    $highest = $null
    foreach ($tag in $Tags) {
        $version = ConvertTo-DsHidMiniSetupVersionFromTag -Tag $tag
        if ($null -eq $version) {
            continue
        }

        if ($null -eq $highest -or $version -gt $highest) {
            $highest = $version
        }
    }

    return $highest
}

function Assert-DsHidMiniSetupVersionNotRegressed {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $SetupVersion,

        [AllowNull()]
        [version] $HighestPublishedVersion
    )

    if ($SetupVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "SetupVersion must be MAJOR.MINOR.PATCH. Got: '$SetupVersion'."
    }

    if ($null -eq $HighestPublishedVersion) {
        return
    }

    $candidate = [version]$SetupVersion
    if ($candidate -lt $HighestPublishedVersion) {
        $lines = @(
            "Setup version $SetupVersion is older than the highest already-published setup-v$HighestPublishedVersion."
            'Windows Installer treats a lower ProductVersion as a downgrade and blocks it for anyone already on the newer build.'
            "Dispatch a driver tag whose MAJOR.MINOR.PATCH exceeds $HighestPublishedVersion. If the driver's own version"
            "numbering has not caught up, bump its major version (e.g. v3.0.0) instead of adding a version offset;"
            'that stays correct permanently without ongoing bookkeeping.'
        )
        throw ($lines -join ' ')
    }
}

function Get-DsHidMiniAllSetupReleaseTags {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository
    )

    $pages = gh api --paginate --slurp "repos/$Repository/git/matching-refs/tags/setup-v" | ConvertFrom-Json
    if ($LASTEXITCODE) {
        throw 'Failed to list setup-v* tags.'
    }

    @(
        foreach ($page in @($pages)) {
            foreach ($item in @($page)) {
                [string]$item.ref -replace '^refs/tags/', ''
            }
        }
    )
}

function Assert-DsHidMiniReleaseMetadataMatchesTag {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Metadata,

        [Parameter(Mandatory)]
        [string] $DriverTag
    )

    $identity = ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag $DriverTag
    $tag = [string]$Metadata.tag
    $setup = [string]$Metadata.setupVersion
    $driver = [string]$Metadata.driverVersion
    $commit = [string]$Metadata.commit

    if ($tag -ne $identity.DriverTag) {
        throw "Release metadata tag '$tag' does not match requested driver tag '$($identity.DriverTag)'."
    }

    if ($setup -ne $identity.SetupVersion) {
        throw "Release metadata setupVersion '$setup' must be '$($identity.SetupVersion)'."
    }

    if ($driver -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "Release metadata driverVersion is invalid: '$driver'."
    }

    if (-not $driver.StartsWith("$($identity.SetupVersion).", [StringComparison]::Ordinal)) {
        throw "Release metadata driverVersion '$driver' is not derived from setupVersion '$($identity.SetupVersion)'."
    }

    if ($commit -notmatch '^[0-9a-f]{40}$') {
        throw "Release metadata commit is missing or not a full SHA: '$commit'."
    }
}

function Find-DsHidMiniExistingFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Root,

        [Parameter(Mandatory)]
        [string[]] $RelativeCandidates
    )

    foreach ($relative in $RelativeCandidates) {
        $path = Join-Path $Root $relative
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            return (Resolve-Path -LiteralPath $path).Path
        }
    }

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return $null
    }

    foreach ($relative in $RelativeCandidates) {
        $leaf = Split-Path -Leaf $relative
        $normalized = $relative.Replace('\', '/')
        $match = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $leaf -ErrorAction SilentlyContinue |
            Where-Object {
                $_.FullName.Replace('\', '/').EndsWith($normalized, [StringComparison]::OrdinalIgnoreCase)
            } |
            Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

function Resolve-DsHidMiniAttestedDriversRoot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Source
    )

    if (Test-Path -LiteralPath (Join-Path $Source 'dshidmini.inf') -PathType Leaf) {
        return (Resolve-Path -LiteralPath $Source).Path
    }

    $inf = Find-DsHidMiniExistingFile -Root $Source -RelativeCandidates @('dshidmini.inf')
    if (-not $inf) {
        throw "Microsoft-attested drivers were not found under $Source."
    }

    return Split-Path -Parent $inf
}

function Assert-DsHidMiniDriverLayout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $DriversRoot
    )

    $missing = @()
    foreach ($relative in Get-DsHidMiniRequiredAttestedDriverFiles) {
        $path = Join-Path $DriversRoot $relative
        $alias = $null
        if ($relative -eq 'ARM64\dshidmini.dll') {
            $alias = Join-Path $DriversRoot 'arm64\dshidmini.dll'
        }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -and
            (-not $alias -or -not (Test-Path -LiteralPath $alias -PathType Leaf))) {
            $missing += $path
        }
    }

    if ($missing.Count -gt 0) {
        throw "Driver package is missing required files:`n$($missing -join [Environment]::NewLine)"
    }
}

function Copy-DsHidMiniSetupPayload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $DriversSource,

        [Parameter(Mandatory)]
        [string] $ToolsSource,

        [Parameter(Mandatory)]
        [string] $ArtifactsRoot
    )

    $driversRoot = Resolve-DsHidMiniAttestedDriversRoot -Source $DriversSource
    Assert-DsHidMiniDriverLayout -DriversRoot $driversRoot

    $controlApp = Find-DsHidMiniExistingFile -Root $ToolsSource -RelativeCandidates @(
        'bin\ControlApp.exe'
        'ControlApp.exe'
    )
    if (-not $controlApp) {
        throw "Downloaded tools are missing ControlApp.exe under $ToolsSource."
    }

    $setupDrivers = Join-Path $ArtifactsRoot 'drivers'
    $setupBin = Join-Path $ArtifactsRoot 'bin'
    foreach ($path in @($setupDrivers, $setupBin)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }

    New-Item -ItemType Directory -Force -Path $setupBin | Out-Null
    Copy-Item -LiteralPath $driversRoot -Destination $setupDrivers -Recurse
    $arm64Dll = Join-Path $setupDrivers 'ARM64\dshidmini.dll'
    $arm64Alias = Join-Path $setupDrivers 'arm64\dshidmini.dll'
    if (-not (Test-Path -LiteralPath $arm64Dll -PathType Leaf) -and (Test-Path -LiteralPath $arm64Alias -PathType Leaf)) {
        New-Item -ItemType Directory -Force -Path (Join-Path $setupDrivers 'ARM64') | Out-Null
        Copy-Item -LiteralPath $arm64Alias -Destination $arm64Dll
    }

    Copy-Item -LiteralPath $controlApp -Destination (Join-Path $setupBin 'ControlApp.exe')

    Assert-DsHidMiniSetupPayload -ArtifactsRoot $ArtifactsRoot
}

function Get-DsHidMiniRequiredCustomActionAssemblies {
    @(
        'CliWrap.dll'
        'Microsoft.Bcl.AsyncInterfaces.dll'
        'Nefarius.Utilities.DeviceManagement.dll'
        'Newtonsoft.Json.dll'
        'System.Buffers.dll'
        'System.Memory.dll'
        'System.Numerics.Vectors.dll'
        'System.Runtime.CompilerServices.Unsafe.dll'
        'System.Threading.Tasks.Extensions.dll'
    )
}

function Get-DsHidMiniLfsTrackedPayloadFiles {
    @(
        'setup\nefcon\x64\nefconc.exe'
        'setup\nefcon\ARM64\nefconc.exe'
        'setup\nefarius_DsHidMini_Updater.exe'
        'setup\igfilter\nssmkig_x64\nssmkig.sys'
        'setup\igfilter\nssmkig_ARM64\nssmkig.sys'
    )
}

function Test-DsHidMiniIsGitLfsPointer {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $file = Get-Item -LiteralPath $Path
    # Pointer files are small UTF-8 text blobs; real payload binaries are far larger.
    if ($file.Length -gt 1024) {
        return $false
    }

    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $text = [Text.Encoding]::ASCII.GetString($bytes)
    return $text.StartsWith('version https://git-lfs.github.com/spec/', [StringComparison]::Ordinal)
}

<#
.SYNOPSIS
    Fails when repository-tracked installer binaries are still Git LFS pointer stubs.

.DESCRIPTION
    nefcon and the updater are stored in Git LFS. A checkout without LFS leaves
    132-byte pointer files behind, and MSI happily packages those as unversioned
    files. Windows Installer then refuses to overwrite the versioned copy from an
    earlier setup ("higher versioned keyfile exists"), so nefconc.exe is never laid
    down and the deferred driver install fails with 1603.
#>
function Assert-DsHidMiniNoGitLfsPointers {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $RepositoryRoot,

        [AllowNull()]
        [AllowEmptyCollection()]
        [string[]] $RelativePaths
    )

    if ($null -eq $RelativePaths -or $RelativePaths.Count -eq 0) {
        $RelativePaths = Get-DsHidMiniLfsTrackedPayloadFiles
    }

    $missing = @()
    $pointers = @()
    foreach ($relative in $RelativePaths) {
        $path = Join-Path $RepositoryRoot $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $missing += $relative
            continue
        }

        if (Test-DsHidMiniIsGitLfsPointer -Path $path) {
            $pointers += $relative
        }
    }

    if ($missing.Count -gt 0) {
        throw "Installer payload files are missing:`n$($missing -join [Environment]::NewLine)"
    }

    if ($pointers.Count -gt 0) {
        $lines = @(
            'Installer payload files are Git LFS pointer stubs, not real binaries:'
            ($pointers -join [Environment]::NewLine)
            "Check out with LFS enabled (actions/checkout 'lfs: true', or run 'git lfs pull')."
        )
        throw ($lines -join [Environment]::NewLine)
    }

    Write-Output "Installer payload binaries are real files (no Git LFS pointers): $($RelativePaths.Count) checked."
}

function Find-DsHidMiniCabinetRange {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [byte[]] $Bytes
    )

    for ($offset = 0; $offset -le $Bytes.Length - 36; $offset++) {
        if ($Bytes[$offset] -ne 0x4D -or $Bytes[$offset + 1] -ne 0x53 -or
            $Bytes[$offset + 2] -ne 0x43 -or $Bytes[$offset + 3] -ne 0x46) {
            continue
        }

        $length = [BitConverter]::ToUInt32($Bytes, $offset + 8)
        if ($length -ge 36 -and ($offset + $length) -le $Bytes.Length) {
            return [pscustomobject]@{
                Offset = $offset
                Length = [int]$length
            }
        }
    }

    return $null
}

function Get-DsHidMiniSfxCaCabinetFileNames {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "SfxCA payload was not found: $Path"
    }

    $bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    $range = Find-DsHidMiniCabinetRange -Bytes $bytes
    if ($null -eq $range) {
        throw "No appended cabinet was found in SfxCA payload: $Path"
    }

    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("dshidmini-sfxca-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
    try {
        $cab = Join-Path $tempRoot 'package.cab'
        $extract = Join-Path $tempRoot 'files'
        New-Item -ItemType Directory -Force -Path $extract | Out-Null
        $cabBytes = New-Object byte[] $range.Length
        [Array]::Copy($bytes, $range.Offset, $cabBytes, 0, $range.Length)
        [IO.File]::WriteAllBytes($cab, $cabBytes)

        $expand = Join-Path $env:WINDIR 'System32\expand.exe'
        if (-not (Test-Path -LiteralPath $expand -PathType Leaf)) {
            throw "expand.exe was not found at $expand."
        }

        $null = & $expand $cab -F:* $extract
        if ($LASTEXITCODE) {
            throw "expand.exe failed to extract the SfxCA cabinet from $Path (exit $LASTEXITCODE)."
        }

        @(Get-ChildItem -LiteralPath $extract -File | ForEach-Object { $_.Name })
    }
    finally {
        if (Test-Path -LiteralPath $tempRoot) {
            Remove-Item -LiteralPath $tempRoot -Recurse -Force
        }
    }
}

function Get-DsHidMiniMsiBinaryPayload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $MsiPath,

        [Parameter(Mandatory)]
        [string] $BinaryName
    )

    if (-not (Test-Path -LiteralPath $MsiPath -PathType Leaf)) {
        throw "MSI was not found: $MsiPath"
    }

    if ($BinaryName -match "[^A-Za-z0-9_.-]") {
        throw "Binary name contains unsupported characters: '$BinaryName'."
    }

    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("dshidmini-msi-binary-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
    $destination = Join-Path $tempRoot 'payload.bin'

    $installer = $null
    $database = $null
    $view = $null
    $record = $null
    try {
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.OpenDatabase((Resolve-Path -LiteralPath $MsiPath).Path, 0)
        $view = $database.OpenView("SELECT ``Data`` FROM ``Binary`` WHERE ``Name``='$BinaryName'")
        [void]$view.Execute()
        $record = $view.Fetch()
        if ($null -eq $record) {
            throw "Binary '$BinaryName' was not found in $MsiPath."
        }

        $remaining = [int64]$record.DataSize(1)
        if ($remaining -le 0) {
            throw "Binary '$BinaryName' in $MsiPath is empty."
        }

        $output = [IO.File]::Create($destination)
        try {
            while ($remaining -gt 0) {
                $chunkSize = [int][Math]::Min(1048576L, $remaining)
                # Record.ReadStream(..., 1) returns a string whose Char values are the raw bytes.
                $chunk = $record.ReadStream(1, $chunkSize, 1)
                $bytes = [byte[]][char[]]$chunk
                if ($bytes.Length -le 0) {
                    throw "Windows Installer stopped streaming '$BinaryName' with $remaining bytes remaining."
                }

                $output.Write($bytes, 0, $bytes.Length)
                $remaining -= $bytes.Length
            }
        }
        finally {
            $output.Dispose()
        }

        return [pscustomobject]@{
            Path     = $destination
            TempRoot = $tempRoot
        }
    }
    catch {
        if (Test-Path -LiteralPath $tempRoot) {
            Remove-Item -LiteralPath $tempRoot -Recurse -Force
        }
        throw
    }
    finally {
        foreach ($comObject in @($record, $view, $database, $installer)) {
            if ($null -ne $comObject) {
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($comObject)
            }
        }
    }
}

function Get-DsHidMiniMsiPayloadFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $MsiPath
    )

    if (-not (Test-Path -LiteralPath $MsiPath -PathType Leaf)) {
        throw "MSI was not found: $MsiPath"
    }

    $installer = $null
    $database = $null
    $view = $null
    try {
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.OpenDatabase((Resolve-Path -LiteralPath $MsiPath).Path, 0)
        $view = $database.OpenView("SELECT ``File``,``FileName``,``FileSize``,``Version`` FROM ``File``")
        [void]$view.Execute()

        $rows = [System.Collections.Generic.List[object]]::new()
        while ($true) {
            $record = $view.Fetch()
            if ($null -eq $record) {
                break
            }

            try {
                # FileName is "short|long" when a short name was generated.
                $name = [string]$record.StringData(2)
                $long = $name.Split('|')[-1]
                $rows.Add([pscustomobject]@{
                        Key      = [string]$record.StringData(1)
                        FileName = $long
                        FileSize = [int64]$record.StringData(3)
                        Version  = [string]$record.StringData(4)
                    })
            }
            finally {
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($record)
            }
        }

        return $rows.ToArray()
    }
    finally {
        foreach ($comObject in @($view, $database, $installer)) {
            if ($null -ne $comObject) {
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($comObject)
            }
        }
    }
}

function Assert-DsHidMiniMsiBinariesVersioned {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        $PayloadFiles
    )

    $unversioned = @(
        foreach ($row in @($PayloadFiles)) {
            $extension = [IO.Path]::GetExtension([string]$row.FileName)
            if ($extension -notin @('.exe', '.dll', '.sys')) {
                continue
            }

            if ([string]::IsNullOrWhiteSpace($row.Version)) {
                "$($row.FileName) (size $($row.FileSize), no version)"
            }
        }
    )

    if ($unversioned.Count -gt 0) {
        $lines = @(
            'MSI packages executables without version resources:'
            ($unversioned -join [Environment]::NewLine)
            'This is what a Git LFS pointer stub looks like once packaged; Windows Installer'
            'will refuse to overwrite an existing versioned copy of these files.'
        )
        throw ($lines -join [Environment]::NewLine)
    }
}

function Test-DsHidMiniPackageContainsAssembly {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [System.Collections.Generic.HashSet[string]] $Present,

        [Parameter(Mandatory)]
        [string] $AssemblyFileName
    )

    $base = [IO.Path]::GetFileNameWithoutExtension($AssemblyFileName)
    return $Present.Contains($AssemblyFileName) -or
        $Present.Contains($base) -or
        $Present.Contains("$base.dll")
}

function Get-DsHidMiniCustomActionManifestAssemblies {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $ManifestPath
    )

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "Custom-action manifest was not produced by the build: $ManifestPath"
    }

    $names = @(
        Get-Content -LiteralPath $ManifestPath |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ }
    )
    if ($names.Count -eq 0) {
        throw "Custom-action manifest is empty: $ManifestPath"
    }

    return $names
}

function Assert-DsHidMiniCustomActionPackageFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]] $PackageFiles,

        # Closure emitted by the MSI build. Checking it keeps this guard honest when a new
        # or transitive dependency is added that the static baseline below does not name.
        [AllowNull()]
        [AllowEmptyCollection()]
        [string[]] $ExpectedAssemblies
    )

    $present = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($name in @($PackageFiles)) {
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            [void]$present.Add($name.Trim())
        }
    }

    $required = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($name in @(Get-DsHidMiniRequiredCustomActionAssemblies) + @($ExpectedAssemblies)) {
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            [void]$required.Add($name.Trim())
        }
    }

    $missing = @(
        $required |
            Where-Object { -not (Test-DsHidMiniPackageContainsAssembly -Present $present -AssemblyFileName $_) } |
            Sort-Object
    )
    if ($missing.Count -gt 0) {
        throw "Custom-action package is missing required assemblies:`n$($missing -join [Environment]::NewLine)"
    }
}

function Get-DsHidMiniCustomActionBinaryName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $MsiPath,

        [string] $ActionName = 'InstallDrivers'
    )

    if ($ActionName -match "[^A-Za-z0-9_.-]") {
        throw "Custom action name contains unsupported characters: '$ActionName'."
    }

    $rows = @(Get-DsHidMiniMsiTableRows -MsiPath $MsiPath -Sql "SELECT ``Source`` FROM ``CustomAction`` WHERE ``Action``='$ActionName'" -ColumnCount 1)
    if ($rows.Count -eq 0) {
        throw "Custom action '$ActionName' was not found in $MsiPath."
    }

    $first = $rows[0]
    $source = if ($first -is [System.Array] -and $first.Length -gt 0) {
        [string]$first[0]
    }
    else {
        [string]$first
    }

    if ([string]::IsNullOrWhiteSpace($source)) {
        throw "Custom action '$ActionName' has no Binary source in $MsiPath."
    }

    return $source
}

function Assert-DsHidMiniCustomActionPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $MsiPath,

        [string] $BinaryName,

        [AllowNull()]
        [string] $ManifestPath
    )

    $expected = @()
    if ($ManifestPath) {
        $expected = Get-DsHidMiniCustomActionManifestAssemblies -ManifestPath $ManifestPath
        Write-Output "Custom-action manifest lists $($expected.Count) support assemblies."
    }

    if ([string]::IsNullOrWhiteSpace($BinaryName)) {
        $BinaryName = Get-DsHidMiniCustomActionBinaryName -MsiPath $MsiPath
        Write-Output "Custom-action binary for InstallDrivers is $BinaryName."
    }

    $payload = Get-DsHidMiniMsiBinaryPayload -MsiPath $MsiPath -BinaryName $BinaryName
    try {
        $names = Get-DsHidMiniSfxCaCabinetFileNames -Path $payload.Path
        Assert-DsHidMiniCustomActionPackageFiles -PackageFiles $names -ExpectedAssemblies $expected
        Write-Output "Custom-action package $BinaryName contains required assemblies."
        foreach ($name in ($names | Sort-Object)) {
            Write-Output "  $name"
        }
    }
    finally {
        $tempRoot = $null
        if ($null -ne $payload -and $payload -isnot [array]) {
            $tempRoot = $payload.TempRoot
        }
        elseif ($payload -is [array]) {
            $tempRoot = @($payload | Where-Object { $_ -and $_.PSObject.Properties['TempRoot'] } | Select-Object -Last 1).TempRoot
        }

        if ($tempRoot -and (Test-Path -LiteralPath $tempRoot)) {
            Remove-Item -LiteralPath $tempRoot -Recurse -Force
        }
    }
}

function Get-DsHidMiniMsiTableRows {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $MsiPath,

        [Parameter(Mandatory)]
        [string] $Sql,

        # Windows Installer's Record COM object does not expose FieldCount through
        # PowerShell's COM adapter, so the selected column count has to be passed in.
        [ValidateRange(1, 16)]
        [int] $ColumnCount = 1
    )

    if (-not (Test-Path -LiteralPath $MsiPath -PathType Leaf)) {
        throw "MSI was not found: $MsiPath"
    }

    $installer = $null
    $database = $null
    $view = $null
    try {
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.OpenDatabase((Resolve-Path -LiteralPath $MsiPath).Path, 0)
        $view = $database.OpenView($Sql)
        [void]$view.Execute()

        $rows = [System.Collections.Generic.List[object]]::new()
        while ($true) {
            $record = $view.Fetch()
            if ($null -eq $record) {
                break
            }

            try {
                $values = @(
                    for ($i = 1; $i -le $ColumnCount; $i++) {
                        [string]$record.StringData($i)
                    }
                )
                $rows.Add($values)
            }
            finally {
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($record)
            }
        }

        return $rows.ToArray()
    }
    finally {
        foreach ($comObject in @($view, $database, $installer)) {
            if ($null -ne $comObject) {
                [void][Runtime.InteropServices.Marshal]::ReleaseComObject($comObject)
            }
        }
    }
}

function ConvertFrom-DsHidMiniMsiName {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [string] $Value
    )

    if ([string]::IsNullOrEmpty($Value)) {
        return $Value
    }

    $pipe = $Value.IndexOf('|')
    if ($pipe -ge 0) {
        return $Value.Substring($pipe + 1)
    }

    return $Value
}

function Test-DsHidMiniMsiNamePresent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]] $Values,

        [Parameter(Mandatory)]
        [string] $Expected
    )

    # Windows Installer stores shortcut names with the .lnk extension appended, so both
    # sides are compared without it.
    $trimLnk = {
        param([string] $Name)
        if ($Name -and $Name.EndsWith('.lnk', [StringComparison]::OrdinalIgnoreCase)) {
            return $Name.Substring(0, $Name.Length - 4)
        }
        return $Name
    }

    $expected = & $trimLnk $Expected
    foreach ($value in @($Values)) {
        $actual = & $trimLnk (ConvertFrom-DsHidMiniMsiName -Value $value)
        if ([string]::Equals($actual, $expected, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Assert-DsHidMiniMsiContract {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $MsiPath
    )

    $errors = [System.Collections.Generic.List[string]]::new()

    $fileNames = @(
        Get-DsHidMiniMsiTableRows -MsiPath $MsiPath -Sql "SELECT ``FileName`` FROM ``File``" -ColumnCount 1 |
            ForEach-Object { $_[0] }
    )
    if (-not (Test-DsHidMiniMsiNamePresent -Values $fileNames -Expected 'ControlApp.exe')) {
        $errors.Add('File table is missing ControlApp.exe.')
    }
    if (-not (Test-DsHidMiniMsiNamePresent -Values $fileNames -Expected 'DsHidMini.man')) {
        $errors.Add('File table is missing DsHidMini.man.')
    }

    $shortcutNames = @(
        Get-DsHidMiniMsiTableRows -MsiPath $MsiPath -Sql "SELECT ``Name`` FROM ``Shortcut``" -ColumnCount 1 |
            ForEach-Object { $_[0] }
    )
    if (-not (Test-DsHidMiniMsiNamePresent -Values $shortcutNames -Expected 'DsHidMini Control App')) {
        $errors.Add("Shortcut table is missing 'DsHidMini Control App'.")
    }

    $customActions = @(
        Get-DsHidMiniMsiTableRows -MsiPath $MsiPath -Sql "SELECT ``Action`` FROM ``CustomAction``" -ColumnCount 1 |
            ForEach-Object { $_[0] }
    )
    if (-not ($customActions | Where-Object { [string]::Equals($_, 'CheckDotNetRuntime', [StringComparison]::OrdinalIgnoreCase) })) {
        $errors.Add('CustomAction table is missing CheckDotNetRuntime.')
    }
    if (-not ($customActions | Where-Object { [string]::Equals($_, 'OpenArticle', [StringComparison]::OrdinalIgnoreCase) })) {
        $errors.Add('CustomAction table is missing OpenArticle.')
    }
    if (-not ($customActions | Where-Object { [string]::Equals($_, 'InstallManifest', [StringComparison]::OrdinalIgnoreCase) })) {
        $errors.Add('CustomAction table is missing InstallManifest.')
    }
    if (-not ($customActions | Where-Object { [string]::Equals($_, 'UninstallManifest', [StringComparison]::OrdinalIgnoreCase) })) {
        $errors.Add('CustomAction table is missing UninstallManifest.')
    }

    $sequence = @(
        Get-DsHidMiniMsiTableRows -MsiPath $MsiPath -Sql "SELECT ``Action``,``Condition`` FROM ``InstallExecuteSequence``" -ColumnCount 2 |
            ForEach-Object {
                [pscustomobject]@{ Action = $_[0]; Condition = $_[1] }
            }
    )
    $runtimeSequenced = $sequence | Where-Object {
        [string]::Equals($_.Action, 'CheckDotNetRuntime', [StringComparison]::OrdinalIgnoreCase) -and
        $_.Condition -and $_.Condition.IndexOf('NOT Installed', [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    if (-not $runtimeSequenced) {
        $errors.Add("CheckDotNetRuntime is missing from InstallExecuteSequence with condition 'NOT Installed'.")
    }

    $articleSequenced = $sequence | Where-Object {
        [string]::Equals($_.Action, 'OpenArticle', [StringComparison]::OrdinalIgnoreCase) -and
        $_.Condition -and $_.Condition.IndexOf('NOT Installed', [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    if (-not $articleSequenced) {
        $errors.Add("OpenArticle is missing from InstallExecuteSequence with condition 'NOT Installed'.")
    }

    $installManifestSequenced = $sequence | Where-Object {
        [string]::Equals($_.Action, 'InstallManifest', [StringComparison]::OrdinalIgnoreCase) -and
        $_.Condition -and $_.Condition.IndexOf('NOT Installed', [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    if (-not $installManifestSequenced) {
        $errors.Add("InstallManifest is missing from InstallExecuteSequence with condition 'NOT Installed'.")
    }

    $uninstallManifestSequenced = $sequence | Where-Object {
        [string]::Equals($_.Action, 'UninstallManifest', [StringComparison]::OrdinalIgnoreCase) -and
        $_.Condition -and $_.Condition.IndexOf('REMOVE="ALL"', [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    if (-not $uninstallManifestSequenced) {
        $errors.Add('UninstallManifest is missing from InstallExecuteSequence with condition REMOVE="ALL".')
    }

    $errorRows = @(
        Get-DsHidMiniMsiTableRows -MsiPath $MsiPath -Sql "SELECT ``Error``,``Message`` FROM ``Error``" -ColumnCount 2 |
            ForEach-Object {
                [pscustomobject]@{ Id = $_[0]; Message = $_[1] }
            }
    )
    $runtimeError = $errorRows | Where-Object {
        [string]::Equals($_.Id, '9001', [StringComparison]::OrdinalIgnoreCase) -and
        $_.Message -and $_.Message.IndexOf('.NET 10 Desktop Runtime', [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
    if (-not $runtimeError) {
        $errors.Add("Error 9001 must mention '.NET 10 Desktop Runtime'.")
    }

    if ($errors.Count -gt 0) {
        throw "Generated MSI is missing the ControlApp packaging contract:`n$($errors -join [Environment]::NewLine)"
    }

    Write-Output 'MSI contract includes ControlApp.exe, DsHidMini.man, the Start Menu shortcut, CheckDotNetRuntime, OpenArticle, and ETW manifest custom actions.'
}

function Assert-DsHidMiniSetupPayload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $ArtifactsRoot
    )

    $required = @(
        (Get-DsHidMiniRequiredAttestedDriverFiles | ForEach-Object { Join-Path 'drivers' $_ })
        'bin\ControlApp.exe'
    )
    $missing = @()
    foreach ($relative in $required) {
        $path = Join-Path $ArtifactsRoot $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $missing += $path
        }
    }

    if ($missing.Count -gt 0) {
        throw "Setup payload is incomplete, missing:`n$($missing -join [Environment]::NewLine)"
    }
}

function Get-DsHidMiniAllowlistedPublisherIdentities {
    [pscustomobject]@{
        Subjects    = @('Nefarius Software Solutions e.U.')
        Thumbprints = @()
    }
}

function Test-DsHidMiniIsAllowlistedPublisher {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Certificate
    )

    $allow = Get-DsHidMiniAllowlistedPublisherIdentities
    $thumbprint = [string]$Certificate.Thumbprint
    $simpleName = ''
    try {
        $simpleName = [string]$Certificate.GetNameInfo(
            [System.Security.Cryptography.X509Certificates.X509NameType]::SimpleName,
            $false)
    }
    catch {
        $simpleName = ''
    }

    foreach ($name in @($allow.Subjects)) {
        if ($simpleName -and $simpleName.Equals($name, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($thumb in @($allow.Thumbprints)) {
        if ($thumbprint -and $thumbprint.Equals($thumb, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Assert-DsHidMiniAllowlistedPublisher {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Certificate,

        [Parameter(Mandatory)]
        [string] $Path
    )

    if (Test-DsHidMiniIsAllowlistedPublisher -Certificate $Certificate) {
        return
    }

    throw "Signer on $Path is not the allowlisted publisher: Subject='$($Certificate.Subject)'; Thumbprint='$($Certificate.Thumbprint)'."
}

function Assert-DsHidMiniAttestedDriverSignatures {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $DriversRoot
    )

    foreach ($relative in Get-DsHidMiniRequiredAttestedDriverBinaries) {
        $file = Join-Path $DriversRoot $relative
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            throw "Missing attested driver file: $file"
        }

        $sig = Get-AuthenticodeSignature -LiteralPath $file
        if ($sig.Status -ne 'Valid' -or -not $sig.SignerCertificate) {
            throw "Attested binary is not validly signed: $file (Status=$($sig.Status))."
        }

        $subject = $sig.SignerCertificate.Subject
        if ($subject -notmatch 'Nefarius Software Solutions e\.U\.' -and $subject -notmatch 'Microsoft') {
            throw "Unexpected signer on $file : $subject"
        }

        Write-Output "Valid: $file ($subject)"
    }
}

function New-DsHidMiniSetupProvenance {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $DriverTag,

        [Parameter(Mandatory)]
        [string] $SetupVersion,

        [Parameter(Mandatory)]
        [string] $SetupTag,

        [Parameter(Mandatory)]
        [string] $DriverVersion,

        [Parameter(Mandatory)]
        [string] $DriverCommit,

        [Parameter(Mandatory)]
        [string] $SetupCommit,

        [Parameter(Mandatory)]
        [long] $BuildRunId,

        [Parameter(Mandatory)]
        [long] $DriversRunId,

        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [string] $MsiName,

        [Parameter(Mandatory)]
        [string] $MsiSha256
    )

    $identity = ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag $DriverTag
    if ($SetupVersion -ne $identity.SetupVersion) {
        throw "SetupVersion '$SetupVersion' does not match driver tag '$($identity.DriverTag)'."
    }

    $escaped = [regex]::Escape($identity.SetupVersion)
    if ($SetupTag -ne $identity.SetupTagBase -and $SetupTag -notmatch "^setup-v$escaped-r([1-9][0-9]*)$") {
        throw "Setup tag '$SetupTag' is not $($identity.SetupTagBase) or a -rN re-spin."
    }

    if ($DriverVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or
        -not $DriverVersion.StartsWith("$($identity.SetupVersion).", [StringComparison]::Ordinal)) {
        throw "DriverVersion '$DriverVersion' is not derived from '$($identity.SetupVersion)'."
    }

    foreach ($name in @('DriverCommit', 'SetupCommit')) {
        $value = Get-Variable $name -ValueOnly
        if ($value -notmatch '^[0-9a-f]{40}$') {
            throw "$name is missing or not a full SHA: '$value'."
        }
    }

    if ($BuildRunId -le 0 -or $DriversRunId -le 0) {
        throw 'BuildRunId and DriversRunId must be positive.'
    }

    if ($MsiName -ne (Get-DsHidMiniSetupMsiFileName -SetupVersion $identity.SetupVersion)) {
        throw "MSI name '$MsiName' does not match setup version '$($identity.SetupVersion)'."
    }

    if ($MsiSha256 -notmatch '^[0-9a-f]{64}$') {
        throw "MSI SHA-256 is invalid: '$MsiSha256'."
    }

    [ordered]@{
        schemaVersion = 1
        driverTag     = $identity.DriverTag
        setupVersion  = $identity.SetupVersion
        setupTag      = $SetupTag
        driverVersion = $DriverVersion
        driverCommit  = $DriverCommit
        setupCommit   = $SetupCommit
        buildRunId    = $BuildRunId
        driversRunId  = $DriversRunId
        repository    = $Repository
        files         = [ordered]@{
            msi = [ordered]@{
                name   = $MsiName
                sha256 = $MsiSha256
            }
        }
    }
}

function Write-DsHidMiniUtf8NoBomJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Object,

        [Parameter(Mandatory)]
        [string] $Path
    )

    $directory = Split-Path -Parent $Path
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $json = $Object | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($false))
}

function Write-DsHidMiniGitHubOutput {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable] $Values,

        [Parameter(Mandatory)]
        [string] $GitHubOutput
    )

    $directory = Split-Path -Parent $GitHubOutput
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $lines = foreach ($key in $Values.Keys) {
        "$key=$($Values[$key])"
    }

    Add-Content -LiteralPath $GitHubOutput -Value $lines -Encoding utf8
    return $lines
}

function Get-DsHidMiniGitHubArtifactNames {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [long] $RunId
    )

    $payload = gh api "repos/$Repository/actions/runs/$RunId/artifacts" | ConvertFrom-Json
    if ($LASTEXITCODE) {
        throw "Failed to list artifacts for run $RunId."
    }

    @($payload.artifacts | Where-Object { -not $_.expired } | ForEach-Object { [string]$_.name })
}

function Save-DsHidMiniGitHubArtifact {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [long] $RunId,

        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Directory
    )

    if (Test-Path -LiteralPath $Directory) {
        Remove-Item -LiteralPath $Directory -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    gh run download $RunId --repo $Repository --name $Name --dir $Directory *>$null
    if ($LASTEXITCODE) {
        throw "Failed to download artifact '$Name' from run $RunId."
    }
}

function Read-DsHidMiniReleaseMetadataFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $file = Get-ChildItem -LiteralPath $Path -Recurse -File -Filter 'release-metadata.json' |
        Select-Object -First 1
    if (-not $file) {
        throw "release-metadata.json was not found under $Path."
    }

    Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
}

function Get-DsHidMiniExistingSetupReleaseTags {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [string] $SetupVersion
    )

    if ($SetupVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "SetupVersion must be MAJOR.MINOR.PATCH. Got: '$SetupVersion'."
    }

    $prefix = "setup-v$SetupVersion"
    $refs = gh api "repos/$Repository/git/matching-refs/tags/$prefix" | ConvertFrom-Json
    if ($LASTEXITCODE) {
        throw "Failed to list tags matching $prefix."
    }

    @($refs | ForEach-Object { [string]$_.ref -replace '^refs/tags/', '' })
}

function Resolve-DsHidMiniSetupArtifactRuns {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [string] $DriverTag
    )

    $identity = ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag $DriverTag
    $seen = [System.Collections.Generic.HashSet[long]]::new()
    $candidates = [System.Collections.Generic.List[object]]::new()

    # Call gh directly. A PowerShell array-of-arrays flattens, and splatting a
    # string then invokes `gh r u n` ("unknown command r").
    $jsonFields = 'databaseId,headSha,createdAt,event,headBranch,url'
    $listedJson = [System.Collections.Generic.List[string]]::new()
    $listedJson.Add([string](gh run list --repo $Repository --workflow build.yml --branch $identity.DriverTag --status success --limit 20 --json $jsonFields))
    if ($LASTEXITCODE) {
        throw "Failed to list Build workflow runs for $($identity.DriverTag)."
    }

    $listedJson.Add([string](gh run list --repo $Repository --workflow build.yml --event workflow_dispatch --status success --limit 30 --json $jsonFields))
    if ($LASTEXITCODE) {
        throw "Failed to list Build workflow runs for $($identity.DriverTag)."
    }

    foreach ($json in $listedJson) {
        $runs = $json | ConvertFrom-Json
        foreach ($run in @($runs)) {
            $id = [int64]$run.databaseId
            if ($seen.Add($id)) {
                $candidates.Add($run)
            }
        }
    }

    $candidates = @($candidates | Sort-Object { Get-Date $_.createdAt } -Descending)
    $stage = Join-Path ([IO.Path]::GetTempPath()) ("dshidmini-setup-resolve-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    try {
        foreach ($run in $candidates) {
            $runId = [int64]$run.databaseId
            $names = Get-DsHidMiniGitHubArtifactNames -Repository $Repository -RunId $runId
            if ($names -notcontains 'release-metadata' -or $names -notcontains 'control-app') {
                Write-Host "Skipping Build run $runId; missing release-metadata or control-app."
                continue
            }

            $metaDir = Join-Path $stage "build-$runId"
            Save-DsHidMiniGitHubArtifact -Repository $Repository -RunId $runId -Name 'release-metadata' -Directory $metaDir
            $metadata = Read-DsHidMiniReleaseMetadataFile -Path $metaDir
            try {
                Assert-DsHidMiniReleaseMetadataMatchesTag -Metadata $metadata -DriverTag $identity.DriverTag
            }
            catch {
                Write-Host "Skipping Build run ${runId}: $($_.Exception.Message)"
                continue
            }

            $driversRunId = $null
            if ($names -contains 'dshidmini-microsoft-drivers') {
                $driversRunId = $runId
            }
            else {
                $driversRunId = Find-DsHidMiniMicrosoftDriversRun -Repository $Repository -DriverTag $identity.DriverTag -Stage $stage
            }

            if (-not $driversRunId) {
                throw "Build run $runId has tools for $($identity.DriverTag), but dshidmini-microsoft-drivers is missing. Wait for Partner Center signing to finish, then re-dispatch."
            }

            return [pscustomobject]@{
                BuildRunId    = $runId
                DriversRunId  = [int64]$driversRunId
                DriverVersion = [string]$metadata.driverVersion
                DriverCommit  = [string]$metadata.commit
                Metadata      = $metadata
            }
        }
    }
    finally {
        if (Test-Path -LiteralPath $stage) {
            Remove-Item -LiteralPath $stage -Recurse -Force
        }
    }

    throw "No successful Build run with release-metadata and control-app was found for $($identity.DriverTag)."
}

function Find-DsHidMiniMicrosoftDriversRun {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [string] $DriverTag,

        [Parameter(Mandatory)]
        [string] $Stage
    )

    $identity = ConvertTo-DsHidMiniSetupReleaseIdentity -DriverTag $DriverTag
    $partnerRuns = gh run list --repo $Repository --workflow partner-signing.yml --status success --limit 50 --json databaseId,createdAt |
        ConvertFrom-Json
    if ($LASTEXITCODE) {
        throw 'Failed to list Partner Center signing runs.'
    }

    foreach ($run in @($partnerRuns | Sort-Object { Get-Date $_.createdAt } -Descending)) {
        $runId = [int64]$run.databaseId
        $names = Get-DsHidMiniGitHubArtifactNames -Repository $Repository -RunId $runId
        if ($names -notcontains 'dshidmini-microsoft-drivers') {
            continue
        }

        if ($names -notcontains 'release-metadata') {
            Write-Host "Partner Center run $runId has attested drivers but no release-metadata; skipping identity check fallback."
            continue
        }

        $metaDir = Join-Path $Stage "partner-$runId"
        Save-DsHidMiniGitHubArtifact -Repository $Repository -RunId $runId -Name 'release-metadata' -Directory $metaDir
        $metadata = Read-DsHidMiniReleaseMetadataFile -Path $metaDir
        try {
            Assert-DsHidMiniReleaseMetadataMatchesTag -Metadata $metadata -DriverTag $identity.DriverTag
        }
        catch {
            Write-Host "Skipping Partner Center run ${runId}: $($_.Exception.Message)"
            continue
        }

        return $runId
    }

    return $null
}

function New-DsHidMiniSetupGitTag {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Repository,

        [Parameter(Mandatory)]
        [string] $Tag,

        [Parameter(Mandatory)]
        [string] $Sha
    )

    if ($Sha -notmatch '^[0-9a-f]{40}$') {
        throw "Tag target SHA is missing or not a full commit: '$Sha'."
    }

    if ($Tag -notmatch '^setup-v\d+\.\d+\.\d+$' -and $Tag -notmatch '^setup-v\d+\.\d+\.\d+-r[1-9][0-9]*$') {
        throw "Refusing to create unexpected setup tag '$Tag'."
    }

    gh api -X POST "repos/$Repository/git/refs" -f ref="refs/tags/$Tag" -f sha=$Sha
    if ($LASTEXITCODE) {
        throw "Failed to create tag $Tag at $Sha."
    }

    return $Tag
}
