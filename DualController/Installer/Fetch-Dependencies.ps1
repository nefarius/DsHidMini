param([string]$Destination = (Join-Path $PSScriptRoot '../artifacts/dependencies'))
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$items = Get-Content (Join-Path $PSScriptRoot 'dependencies.json') -Raw | ConvertFrom-Json
$manifest = @()
foreach ($item in $items) {
    $path = Join-Path $Destination $item.name
    if (-not (Test-Path $path) -or (Get-FileHash $path -Algorithm $item.algorithm).Hash -ne $item.hash) {
        Write-Host "Downloading pinned dependency: $($item.name)"
        Invoke-WebRequest $item.url -OutFile $path -TimeoutSec 300
    }
    $hash = (Get-FileHash $path -Algorithm $item.algorithm).Hash
    if ($hash -ne $item.hash) { throw "Hash mismatch: $($item.name)" }
    $signature = Get-AuthenticodeSignature $path
    if ($signature.Status -ne 'Valid') { throw "Invalid Authenticode signature: $($item.name): $($signature.Status)" }
    $manifest += [ordered]@{ name = $item.name; url = $item.url; algorithm = $item.algorithm;
        hash = $hash; signer = $signature.SignerCertificate.Subject }
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Destination 'verified-dependencies.json')
Write-Host 'All dependency hashes and Authenticode signatures verified.'
