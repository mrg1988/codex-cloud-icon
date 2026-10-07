[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework 4.x C# compiler is required.' }
$binaryDirectory = Join-Path $projectRoot 'bin'
New-Item -ItemType Directory -Path $binaryDirectory -Force | Out-Null
$buildLog = Join-Path $binaryDirectory 'build.log'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ ('/out:' + (Join-Path $binaryDirectory 'CodexIconLauncher.exe')) ('/win32icon:' + (Join-Path $projectRoot 'assets\codex-cloud.ico')) (Join-Path $projectRoot 'src\CodexIconLauncher.cs') *> $buildLog
if ($LASTEXITCODE -ne 0) { throw ('Build failed. Read ' + $buildLog) }
Write-Host 'Built CodexIconLauncher.exe (Windows x64).'
