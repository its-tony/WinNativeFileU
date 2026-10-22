param([Parameter(Mandatory)][string]$PackageFile)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$keyName='Software\Microsoft\Windows\CurrentVersion\Uninstall\WinNativeFileU_is1'
$shortcut=Join-Path ([Environment]::GetFolderPath('Programs')) 'WinNativeFileU.lnk'
function Has-Registration {
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($keyName)
    try { return $null -ne $key } finally { if($key) { $key.Dispose() } }
}
function Startup-Fingerprint {
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
    try { $value=if($key) { $key.GetValue('WinNativeFileU',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) } else { $null } }
    finally { if($key) { $key.Dispose() } }
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $value -Compress))))
}
if((Has-Registration) -or (Test-Path -LiteralPath $shortcut)) { throw 'Existing normal identity or shortcut found; preserve it and stop.' }
if(@(Get-Process -Name WinNativeFileU.App -ErrorAction SilentlyContinue).Count) { throw 'Close WinNativeFileU normally before this fixture test.' }
$packagePath=[IO.Path]::GetFullPath($PackageFile)
$package=Get-Content -LiteralPath $packagePath -Raw | ConvertFrom-Json
$setup=[IO.Path]::GetFullPath($package.installer)
$expected=Join-Path ([IO.Path]::GetDirectoryName($packagePath)) ('WinNativeFileU-'+$package.version+'-win-x64-setup.exe')
if($setup -cne $expected -or !$setup.StartsWith((Join-Path $project 'artifacts')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Expected local normal installer under artifacts.' }
$setupHash=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
$sums=Get-Content -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($packagePath)) 'SHA256SUMS.txt')
if(@($sums | Where-Object { $_ -ceq ($setupHash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($setup)) }).Count -ne 1) { throw 'Installer does not match candidate checksum file.' }
$run=Join-Path $project ('evidence/private/m5-normal-install-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$installed=Join-Path $run 'installed normal space ñ'
for($current=$run;$current;$current=[IO.Path]::GetDirectoryName($current)) {
    if((Get-Item -LiteralPath $current).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse ancestor rejected.' }
}
$before=Startup-Fingerprint
$meta=@{at=(Get-Date).ToString('o');run=$run;installed=$installed;setup=$setup;setupSha256=$setupHash;shortcut=$shortcut;startupBefore=$before;state='Prepared, no installation yet'}
$runFile=Join-Path $run 'run.json'
$meta | ConvertTo-Json | Set-Content -LiteralPath $runFile
$process=Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/DIR="'+$installed+'"'),('/LOG="'+(Join-Path $run 'install.log')+'"')) -WindowStyle Hidden -Wait -PassThru
$meta.installExit=$process.ExitCode;$meta.state='Install returned; verification pending'
$meta | ConvertTo-Json | Set-Content -LiteralPath $runFile
if($process.ExitCode -ne 0 -or !(Has-Registration) -or !(Test-Path -LiteralPath $shortcut)) { throw 'Normal identity installation or shortcut creation failed; inspect retained run.' }
& (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $installed -Installed
$shell=New-Object -ComObject WScript.Shell
try {
    $link=$shell.CreateShortcut($shortcut)
    if($link.TargetPath -ine (Join-Path $installed 'WinNativeFileU.App.exe') -or $link.WorkingDirectory -ine $installed) { throw 'Shortcut target/working directory mismatch; preserve for inspection.' }
    $meta.shortcutTargetVerified=$true
} finally { if($link) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($link) }; [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) }
$foreign=Join-Path $installed 'owner-added.keep'
$profile=Join-Path $run 'profile-preserve.keep'
[IO.File]::WriteAllText($foreign,'Unknown file must remain after normal-identity uninstall.')
[IO.File]::WriteAllText($profile,'External profile fixture must remain.')
$foreignHash=(Get-FileHash -LiteralPath $foreign).Hash;$profileHash=(Get-FileHash -LiteralPath $profile).Hash
$uninstaller=Join-Path $installed 'unins000.exe'
$meta.uninstallerSha256=(Get-FileHash -LiteralPath $uninstaller).Hash
$meta.state='Verified normal identity/shortcut; scoped native uninstall next'
$meta | ConvertTo-Json | Set-Content -LiteralPath $runFile
if(!$run.StartsWith((Join-Path $project 'evidence/private/m5-normal-install-'),[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($run) -notmatch '^m5-normal-install-[0-9a-f]{32}$' -or (Get-Item -LiteralPath $installed).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Unsafe fixture uninstall target.' }
if(@(Get-Process -Name WinNativeFileU.App -ErrorAction SilentlyContinue).Count) { throw 'App started during fixture test; close normally before any uninstall retry.' }
if((Get-FileHash -LiteralPath $uninstaller).Hash -ne $meta.uninstallerSha256 -or (Startup-Fingerprint) -ne $before) { throw 'Uninstaller/startup changed; stop.' }
$process=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $run 'uninstall.log')+'"')) -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0 -or (Has-Registration) -or (Test-Path -LiteralPath $shortcut) -or (Test-Path -LiteralPath (Join-Path $installed 'WinNativeFileU.App.exe'))) { throw 'Normal uninstall/shortcut/registration cleanup failed.' }
if((Get-FileHash -LiteralPath $foreign).Hash -ne $foreignHash -or (Get-FileHash -LiteralPath $profile).Hash -ne $profileHash -or @(Get-ChildItem -LiteralPath $installed -Force).Count -ne 1 -or (Startup-Fingerprint) -ne $before) { throw 'Unknown file/profile/startup preservation failed.' }
$meta.uninstallExit=$process.ExitCode;$meta.state='Complete';$meta.registrationRemoved=$true;$meta.shortcutRemoved=$true;$meta.knownFilesRemoved=$true;$meta.foreignFilePreserved=$true;$meta.profilePreserved=$true;$meta.startupUnchanged=$true
$meta.scope='Normal identity and Start menu shortcut in private redirected path; no UI wizard/default-location/clean-machine/logon or matching Run removal test.'
$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'completion.json')
Write-Output (Join-Path $run 'completion.json')
