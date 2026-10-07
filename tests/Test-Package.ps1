[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $projectRoot 'scripts\Common.ps1')
Import-IconUtilities $projectRoot
$results = Join-Path $projectRoot 'test-results'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$checks = New-Object 'System.Collections.Generic.List[string]'
function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $checks.Add($Message)
}
Get-ChildItem -LiteralPath $projectRoot -Recurse -Filter '*.ps1' | Where-Object { $_.FullName -notmatch '\\(bin|dist|test-results)\\' } | ForEach-Object {
    $parseErrors = $null; $tokens = $null
    [void][Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$parseErrors)
    Assert-Condition ($parseErrors.Count -eq 0) ('PowerShell syntax: ' + $_.Name)
}
& (Join-Path $projectRoot 'scripts\Build.ps1')
$icon = Join-Path $projectRoot 'assets\codex-cloud.ico'
foreach ($size in @(16,24,32,48,64,128,256)) {
    $handle = [ShellIconCheck]::Extract($icon, $size)
    Assert-Condition ($handle -ne [IntPtr]::Zero) ('Native icon extraction: ' + $size + 'px')
    [void][ShellIconCheck]::DestroyIcon($handle)
}
$install = Join-Path $results ('installation with spaces-' + [Guid]::NewGuid().ToString('N'))
& (Join-Path $projectRoot 'Install.ps1') -InstallDirectory $install -NoShortcuts -NoAttach
$physicalMarker = Get-PhysicalFilePath (Join-Path $install 'installation.json')
$physicalRoot = Split-Path -Parent $physicalMarker
$launcher = Join-Path $physicalRoot 'CodexIconLauncher.exe'
$config = Get-Content -LiteralPath (Join-Path $physicalRoot 'launcher.ini')
Assert-Condition ($config.Count -eq 2) 'Configuration has separate application and family lines'
Assert-Condition ($config[0] -like 'AppId=OpenAI.Codex_*!*') 'Installed application ID points to Codex'
Assert-Condition ($config[1] -like 'PackageFamily=OpenAI.Codex_*') 'Installed package family points to Codex'
$status = Get-Content -LiteralPath (Join-Path $physicalRoot 'taskbar-icon-status.txt') -Raw
Assert-Condition ($status.StartsWith('Check passed.')) 'Helper validates installed configuration and icon without changing windows'
Copy-Item -LiteralPath (Join-Path $physicalRoot 'taskbar-icon-status.txt') -Destination (Join-Path $results 'helper-check.txt')
$configPath = Join-Path $physicalRoot 'launcher.ini'
try {
    Set-Content -LiteralPath $configPath -Value 'PackageFamily=OpenAI.ChatGPT-Desktop_example' -Encoding UTF8
    $invalidCheck = Start-Process -FilePath $launcher -ArgumentList '--check' -WindowStyle Hidden -Wait -PassThru
    Assert-Condition ($invalidCheck.ExitCode -ne 0) 'Invalid and non-Codex configuration is rejected'
} finally { $config | Set-Content -LiteralPath $configPath -Encoding UTF8 }
$testShortcut = Join-Path $physicalRoot 'test-shortcut.lnk'
$wsh = New-Object -ComObject WScript.Shell
$link = $wsh.CreateShortcut($testShortcut)
$link.TargetPath = $launcher
$link.IconLocation = (Join-Path $physicalRoot 'codex-cloud.ico') + ',0'
$link.Save()
[CodexShortcut]::SetAppId($testShortcut, 'Local.Codex.OriginalLogo')
$shellHandle = [ShellIconCheck]::GetHandle($testShortcut)
Assert-Condition ($shellHandle -ne [IntPtr]::Zero) 'Shortcut AppUserModelID and shell icon creation'
[void][ShellIconCheck]::DestroyIcon($shellHandle)
Remove-Item -LiteralPath $testShortcut
& (Join-Path $projectRoot 'Install.ps1') -InstallDirectory $install -NoShortcuts -NoAttach
Assert-Condition (@(Get-ChildItem -LiteralPath $physicalRoot -Directory -Filter 'backup-*').Count -eq 1) 'Upgrade preserves the previous installed files'
$sentinel = Join-Path $physicalRoot 'unrelated-file.txt'
Set-Content -LiteralPath $sentinel -Value 'Must survive uninstall.'
& (Join-Path $projectRoot 'Uninstall.ps1') -InstallDirectory $install
Assert-Condition (-not (Test-Path -LiteralPath $launcher)) 'Uninstall removes its own launcher'
Assert-Condition (Test-Path -LiteralPath $sentinel) 'Uninstall preserves unrelated files'
Assert-Condition (@(Get-ChildItem -LiteralPath $physicalRoot -Directory -Filter 'backup-*').Count -eq 1) 'Uninstall preserves upgrade backups'
$checks | Set-Content -LiteralPath (Join-Path $results 'checks.txt') -Encoding UTF8
Write-Host ('Passed ' + $checks.Count + ' checks. Report: ' + (Join-Path $results 'checks.txt'))
