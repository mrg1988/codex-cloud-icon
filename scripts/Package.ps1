[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Build.ps1')
$dist = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$release = Join-Path $dist ('codex-cloud-icon-0.1.0-windows-x64-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.zip')
$include = @('Install.cmd','Install.ps1','Uninstall.cmd','Uninstall.ps1','README.md','LICENSE','ASSETS.md','CHANGELOG.md','VALIDATION.md','src','scripts','tests','assets')
Compress-Archive -LiteralPath @($include | ForEach-Object { Join-Path $projectRoot $_ }) -DestinationPath $release
$hash = Get-FileHash -LiteralPath $release -Algorithm SHA256
($hash.Hash.ToLowerInvariant() + '  ' + (Split-Path -Leaf $release)) | Set-Content -LiteralPath ($release + '.sha256') -Encoding ASCII
Write-Host ('Shareable ZIP: ' + $release)
