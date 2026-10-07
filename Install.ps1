[CmdletBinding()]
param(
    [string]$InstallDirectory = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CodexCloudIcon'),
    [switch]$NoShortcuts,
    [switch]$NoAttach
)
$ErrorActionPreference = 'Stop'
if (-not [Environment]::Is64BitProcess) { throw 'Use 64-bit PowerShell on Windows.' }
. (Join-Path $PSScriptRoot 'scripts\Common.ps1')
Import-IconUtilities $PSScriptRoot
$package = Get-AppxPackage -Name OpenAI.Codex | Sort-Object Version -Descending | Select-Object -First 1
if (-not $package) { throw 'Install the official Codex desktop app before installing this helper.' }
$manifest = Get-AppxPackageManifest -Package $package.PackageFullName
$applications = @($manifest.Package.Applications.Application)
$application = $applications | Where-Object { $_.Id -eq 'App' } | Select-Object -First 1
if (-not $application -and $applications.Count -eq 1) { $application = $applications[0] }
if (-not $application) { throw 'Could not determine the Codex application ID.' }
$appId = $package.PackageFamilyName + '!' + $application.Id
if ($package.PackageFamilyName -notlike 'OpenAI.Codex_*') { throw 'Unexpected package identity.' }
# Build before changing an existing installation or stopping its helper.
& (Join-Path $PSScriptRoot 'scripts\Build.ps1')
$InstallDirectory = [IO.Path]::GetFullPath($InstallDirectory)
$marker = Join-Path $InstallDirectory 'installation.json'
$previous = $null
if (Test-Path -LiteralPath $InstallDirectory) {
    if (-not (Test-Path -LiteralPath $marker)) { throw 'The destination already exists and is not owned by this helper. Choose another directory.' }
    $previous = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
    if ($previous.Tool -ne 'CodexCloudIcon' -or $previous.Schema -ne 1) { throw 'Unrecognized installation marker.' }
} else { New-Item -ItemType Directory -Path $InstallDirectory | Out-Null }
if (-not $previous) {
    @{Tool='CodexCloudIcon';Schema=1;Version='0.1.0';Shortcuts=@()} | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8
}
# MSIX can redirect AppData writes. Resolve the actual on-disk path before making shortcuts.
$marker = Get-PhysicalFilePath $marker
$InstallDirectory = Split-Path -Parent $marker
$launcher = Join-Path $InstallDirectory 'CodexIconLauncher.exe'
Stop-InstalledIconHelper $launcher
if ($previous) {
    $backup = Join-Path $InstallDirectory ('backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path $backup | Out-Null
    foreach ($name in @('CodexIconLauncher.exe','codex-cloud.ico','launcher.ini','installation.json')) {
        $old = Join-Path $InstallDirectory $name
        if (Test-Path -LiteralPath $old) { Copy-Item -LiteralPath $old -Destination $backup }
    }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\CodexIconLauncher.exe') -Destination $launcher -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\codex-cloud.ico') -Destination (Join-Path $InstallDirectory 'codex-cloud.ico') -Force
@(('AppId=' + $appId), ('PackageFamily=' + $package.PackageFamilyName)) | Set-Content -LiteralPath (Join-Path $InstallDirectory 'launcher.ini') -Encoding UTF8
$shortcuts = @()
if ($previous) { $shortcuts = @($previous.Shortcuts) }
if (-not $NoShortcuts) {
    $destinations = @([Environment]::GetFolderPath('DesktopDirectory'), [Environment]::GetFolderPath('Programs'))
    $wsh = New-Object -ComObject WScript.Shell
    foreach ($destination in $destinations) {
        $linkPath = Join-Path $destination 'Codex (Cloud Icon).lnk'
        $link = $wsh.CreateShortcut($linkPath)
        if ((Test-Path -LiteralPath $linkPath) -and $link.TargetPath -ne $launcher) { throw ('An unrelated shortcut already exists: ' + $linkPath) }
        $link.TargetPath = $launcher
        $link.WorkingDirectory = $InstallDirectory
        $link.Arguments = ''
        $link.IconLocation = (Join-Path $InstallDirectory 'codex-cloud.ico') + ',0'
        $link.Description = 'Codex with a cloud icon; unofficial local customization'
        $link.Save()
        $linkPath = Get-PhysicalFilePath $linkPath
        [CodexShortcut]::SetAppId($linkPath, 'Local.Codex.OriginalLogo')
        $shortcuts += $linkPath
        # Record each successful shortcut so partial installations can be removed safely.
        @{Tool='CodexCloudIcon';Schema=1;Version='0.1.0';Shortcuts=@($shortcuts | Select-Object -Unique)} | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8
    }
}
@{Tool='CodexCloudIcon';Schema=1;Version='0.1.0';Shortcuts=@($shortcuts | Select-Object -Unique)} | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8
[ShellIconCheck]::Refresh($launcher)
[ShellIconCheck]::RefreshAssociations()
$check = Start-Process -FilePath $launcher -ArgumentList '--check' -WindowStyle Hidden -Wait -PassThru
if ($check.ExitCode -ne 0) { throw ('Helper validation failed. Read ' + (Join-Path $InstallDirectory 'taskbar-icon-status.txt')) }
if (-not $NoAttach) { Start-Process -FilePath $launcher -ArgumentList '--attach' -WindowStyle Hidden }
Write-Host ('Installed: ' + $InstallDirectory)
Write-Host 'Launch Codex (Cloud Icon). Right-click its running cloud button and choose Pin to taskbar.'
Write-Host 'The official pinned entry can be unpinned manually. Existing app sessions stay open.'
