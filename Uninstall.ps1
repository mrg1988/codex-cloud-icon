[CmdletBinding()]
param([string]$InstallDirectory = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CodexCloudIcon'))
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts\Common.ps1')
Import-IconUtilities $PSScriptRoot
$marker = Join-Path ([IO.Path]::GetFullPath($InstallDirectory)) 'installation.json'
if (-not (Test-Path -LiteralPath $marker)) { throw 'No installation marker found. Nothing was removed.' }
$marker = Get-PhysicalFilePath $marker
$root = Split-Path -Parent $marker
$record = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
if ($record.Tool -ne 'CodexCloudIcon' -or $record.Schema -ne 1) { throw 'Unrecognized installation marker. Nothing was removed.' }
$launcher = Join-Path $root 'CodexIconLauncher.exe'
Stop-InstalledIconHelper $launcher
$wsh = New-Object -ComObject WScript.Shell
foreach ($shortcut in @($record.Shortcuts)) {
    if ((Test-Path -LiteralPath $shortcut) -and [IO.Path]::GetExtension($shortcut) -eq '.lnk') {
        $link = $wsh.CreateShortcut($shortcut)
        if ($link.TargetPath -eq $launcher) {
            Remove-Item -LiteralPath $shortcut
            [ShellIconCheck]::Refresh($shortcut)
        }
    }
}
foreach ($name in @('CodexIconLauncher.exe','codex-cloud.ico','launcher.ini','taskbar-icon-status.txt','installation.json')) {
    $path = Join-Path $root $name
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
}
if (@(Get-ChildItem -LiteralPath $root -Force).Count -eq 0) { Remove-Item -LiteralPath $root }
[ShellIconCheck]::RefreshAssociations()
Write-Host 'Removed the cloud icon helper and its shortcuts. App sessions stay open.'
Write-Host 'Unpin any remaining cloud taskbar button manually. Upgrade backups are retained.'
