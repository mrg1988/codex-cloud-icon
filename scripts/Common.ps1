$ErrorActionPreference = 'Stop'

function Import-IconUtilities {
    param([string]$ProjectRoot)
    if (-not ('ShellIconCheck' -as [type])) { Add-Type -Path (Join-Path $ProjectRoot 'src\ShellUtilities.cs') }
    if (-not ('CodexShortcut' -as [type])) { Add-Type -Path (Join-Path $ProjectRoot 'src\ShortcutProperties.cs') }
}

function Get-PhysicalFilePath {
    param([string]$Path)
    return ([ShellIconCheck]::PhysicalPath($Path) -replace '^\\\\\?\\', '')
}

function Stop-InstalledIconHelper {
    param([string]$Launcher)
    $helpers = @(Get-Process -Name CodexIconLauncher -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $Launcher })
    if ($helpers.Count -eq 0) { return }
    Start-Process -FilePath $Launcher -ArgumentList '--restore' -WindowStyle Hidden -Wait
    foreach ($helper in $helpers) {
        if (-not $helper.WaitForExit(7000)) { throw 'The icon helper did not exit. No app process has been stopped.' }
    }
}
