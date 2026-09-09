#Requires -Version 5.1
<#
.SYNOPSIS
    Resolves the DsHidMini driver/setup versions for a GitHub Actions run.

.DESCRIPTION
    Release tags must be exactly vMAJOR.MINOR.PATCH (example: v3.6.0). Those
    produce DriverVersion MAJOR.MINOR.PATCH.(BuildVersionOffset + RunNumber)
    and SetupVersion MAJOR.MINOR.PATCH.

    Non-tag refs (master, pull requests) produce a CI-only DriverVersion of
    0.0.0.(BuildVersionOffset + RunNumber) and no setup version.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-DsHidMiniReleaseVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string] $Ref,

        [Parameter(Mandatory)]
        [int] $RunNumber,

        [Parameter(Mandatory)]
        [int] $BuildVersionOffset
    )

    if ($RunNumber -lt 1) {
        throw "RunNumber must be a positive integer. Got: $RunNumber"
    }

    if ($BuildVersionOffset -lt 0 -or $BuildVersionOffset -gt 60000) {
        throw "BuildVersionOffset must be in 0..60000. Got: $BuildVersionOffset"
    }

    $revision = $BuildVersionOffset + $RunNumber
    if ($revision -lt 0 -or $revision -gt 65535) {
        throw "Computed file-version revision $revision is outside 0..65535."
    }

    $tag = $Ref.Trim()
    if ($tag -match '^refs/tags/(.+)$') {
        $tag = $Matches[1]
    }

    $isTagRef = $Ref -match '^refs/tags/' -or $tag -match '^v\d'

    if ($isTagRef) {
        if ($tag -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
            throw "Release tags must be vMAJOR.MINOR.PATCH (example: v3.6.0). Got: '$tag'"
        }

        $major = [int]$Matches[1]
        $minor = [int]$Matches[2]
        $patch = [int]$Matches[3]
        foreach ($part in @($major, $minor, $patch)) {
            if ($part -gt 65535) {
                throw "Version component $part exceeds the 16-bit file-version maximum (65535)."
            }
        }

        $setup = "$major.$minor.$patch"
        return [pscustomobject]@{
            IsRelease     = $true
            Tag           = $tag
            SetupVersion  = $setup
            DriverVersion = "$setup.$revision"
            Revision      = $revision
        }
    }

    return [pscustomobject]@{
        IsRelease     = $false
        Tag           = ''
        SetupVersion  = ''
        DriverVersion = "0.0.0.$revision"
        Revision      = $revision
    }
}

function Write-DsHidMiniReleaseVersionOutputs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Version,

        [Parameter(Mandatory)]
        [string] $GitHubOutput
    )

    $lines = @(
        "is-release=$($Version.IsRelease.ToString().ToLowerInvariant())"
        "tag=$($Version.Tag)"
        "setup-version=$($Version.SetupVersion)"
        "driver-version=$($Version.DriverVersion)"
        "revision=$($Version.Revision)"
    )

    $directory = Split-Path -Parent $GitHubOutput
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Add-Content -LiteralPath $GitHubOutput -Value $lines -Encoding utf8
    return $lines
}
