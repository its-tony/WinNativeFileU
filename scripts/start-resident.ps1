param([string]$RunFile, [switch]$StartHidden)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
if ($RunFile) { $run = (Get-Content -LiteralPath $RunFile -Raw | ConvertFrom-Json).run }
else {
    $run = Join-Path $project ('evidence/private/m4-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $run -ErrorAction Stop | Out-Null
}
if (!(Test-Path -LiteralPath $run -PathType Container)) { throw 'Existing resident test directory required.' }
$exe = Join-Path $project 'src/App/bin/Release/net10.0-windows10.0.22000.0/win-x64/WinNativeFileU.App.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build Release before starting the resident.' }
$key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
try { $value = if ($key) { $key.GetValue('WinNativeFileU', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) } else { $null } }
finally { if ($key) { $key.Dispose() } }
$fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $value -Compress))))
$settingsPath = Join-Path $run 'settings.json'
$before = if (Test-Path -LiteralPath $settingsPath -PathType Leaf) { (Get-FileHash -LiteralPath $settingsPath -Algorithm SHA256).Hash } else { $null }
$arguments = @('--resident', '--resident-root', ('"'+$run+'"'))
if ($StartHidden) { $arguments += '--resident-minimized' }
$started = (Get-Date).ToString('o')
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
$meta = @{ run=$run; executable=$exe; pid=$process.Id; date=$started; startupFingerprint=$fingerprint; settingsBeforeHash=$before; isolated=$true; startHidden=[bool]$StartHidden }
$metadataPath = Join-Path $run ('launch-'+[guid]::NewGuid().ToString('N')+'.json')
$meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $metadataPath
Write-Output $metadataPath
