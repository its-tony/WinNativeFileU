param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$run = Join-Path $root ('evidence/private/m2-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$exe = Join-Path $root 'src/App/bin/Release/net10.0-windows10.0.22000.0/win-x64/WinNativeFileU.App.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build Release before launching the preview.' }
$log = Join-Path $run 'events.jsonl'
@{ date=(Get-Date).ToString('o'); run=$run; log=$log; previewOnly=$true } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'run.json')
Start-Process -FilePath $exe -ArgumentList @('--radial-preview', '--preview-log', ('"'+$log+'"')) -WindowStyle Hidden
Write-Output (Join-Path $run 'run.json')
