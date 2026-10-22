param([Parameter(Mandatory)][ValidateSet('Prepare','AfterExit')][string]$Stage, [string]$PackageFile, [string]$RunFile)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$testKey='Software\Microsoft\Windows\CurrentVersion\Uninstall\WinNativeFileU.LocalInstallerTest_is1'
function Test-Registration {
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($testKey)
    try { return $null -ne $key } finally { if($key) { $key.Dispose() } }
}
function Startup-Fingerprint {
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
    try { $value=if($key) { $key.GetValue('WinNativeFileU',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) } else { $null } }
    finally { if($key) { $key.Dispose() } }
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $value -Compress))))
}
function Run-Setup([string]$Setup,[string]$Destination,[string]$Log) {
    $process=Start-Process -FilePath $Setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/NOICONS',('/DIR="'+$Destination+'"'),('/LOG="'+$Log+'"')) -WindowStyle Hidden -Wait -PassThru
    return $process.ExitCode
}
function Run-Uninstall([string]$Uninstaller,[string]$Log) {
    $process=Start-Process -FilePath $Uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+$Log+'"')) -WindowStyle Hidden -Wait -PassThru
    return $process.ExitCode
}
if($Stage -eq 'Prepare') {
    if(!$PackageFile) { throw 'PackageFile required for Prepare.' }
    if(Test-Registration) { throw 'A previous test identity is registered; inspect it instead of replacing it.' }
    $packagePath=[IO.Path]::GetFullPath($PackageFile)
    $package=Get-Content -LiteralPath $packagePath -Raw | ConvertFrom-Json
    $setup=[IO.Path]::GetFullPath($package.testInstaller)
    $expected=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($packagePath)) 'test/WinNativeFileU-test-setup.exe'))
    if($setup -cne $expected -or !$setup.StartsWith((Join-Path $project 'artifacts')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Expected local test-identity installer under artifacts.' }
    $run=Join-Path $project ('evidence/private/m5-install-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $run | Out-Null
    $installed=Join-Path $run 'installed space ñ';$occupied=Join-Path $run 'occupied'
    New-Item -ItemType Directory -Path $occupied | Out-Null
    $sentinel=Join-Path $occupied 'preserve.keep'
    [IO.File]::WriteAllText($sentinel,'Installer negative fixture, preserve this existing file.')
    $sentinelHash=(Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash
    $before=Startup-Fingerprint
    $refused=Run-Setup $setup $occupied (Join-Path $run 'occupied.log')
    if($refused -eq 0 -or (Get-FileHash -LiteralPath $sentinel -Algorithm SHA256).Hash -ne $sentinelHash -or @(Get-ChildItem -LiteralPath $occupied -Force).Count -ne 1 -or (Test-Registration)) { throw 'Nonempty destination refusal/preservation failed.' }
    $install=Run-Setup $setup $installed (Join-Path $run 'install.log')
    if($install -ne 0 -or !(Test-Registration)) { throw "Test installation failed: $install" }
    & (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $installed -Installed
    $uninstaller=Join-Path $installed 'unins000.exe'
    $unknown=Join-Path $installed 'owner-added.keep'
    [IO.File]::WriteAllText($unknown,'Added after installation. The uninstaller must preserve this unrecognized file.')
    $profile=Join-Path $run 'profile-preserve.keep'
    [IO.File]::WriteAllText($profile,'Profile/settings/results outside the installation must remain.')
    $meta=@{at=(Get-Date).ToString('o');run=$run;installed=$installed;packageFile=$packagePath;setup=$setup;setupSha256=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash;startupBefore=$before;nonemptyRefusalExit=$refused;installExit=$install;uninstallerSha256=(Get-FileHash -LiteralPath $uninstaller -Algorithm SHA256).Hash;unknownHash=(Get-FileHash -LiteralPath $unknown -Algorithm SHA256).Hash;profileHash=(Get-FileHash -LiteralPath $profile -Algorithm SHA256).Hash;state='Installed, runtime inspection pending'}
    $RunFile=Join-Path $run 'run.json';$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $RunFile
    $launchFile=& (Join-Path $PSScriptRoot 'start-resident.ps1') -RunFile $RunFile -Executable (Join-Path $installed 'WinNativeFileU.App.exe') -ShowWindow
    $launch=Get-Content -LiteralPath $launchFile -Raw | ConvertFrom-Json
    $ready=$false
    for($attempt=0;$attempt -lt 80;$attempt++) {
        $journal=@(Get-ChildItem -LiteralPath $run -Filter 'resident-*.jsonl' -File)
        if($journal.Count -eq 1 -and (Get-Content -LiteralPath $journal[0].FullName -Raw) -match '"name":"ResidentReady"') { $ready=$true;break }
        Start-Sleep -Milliseconds 250
    }
    if(!$ready) { throw 'Installed application did not log Ready; inspect preserved process/logs.' }
    $modules=@((Get-Process -Id $launch.pid).Modules | Where-Object { $_.ModuleName -in 'coreclr.dll','Microsoft.UI.Xaml.dll' } | ForEach-Object { @{name=$_.ModuleName;path=$_.FileName} })
    if($modules.Count -ne 2 -or @($modules | Where-Object { !$_.path.StartsWith($installed+'\',[StringComparison]::OrdinalIgnoreCase) }).Count) { throw 'Expected bundled CLR and WinUI modules from installation.' }
    $busyInstall=Run-Setup $setup (Join-Path $run 'busy-destination') (Join-Path $run 'busy-install.log')
    $busyUninstall=Run-Uninstall $uninstaller (Join-Path $run 'busy-uninstall.log')
    if($busyInstall -eq 0 -or $busyUninstall -eq 0 -or !(Test-Registration) -or !(Test-Path -LiteralPath (Join-Path $installed 'WinNativeFileU.App.exe')) -or (Test-Path -LiteralPath (Join-Path $run 'busy-destination'))) { throw 'Running-application refusal failed.' }
    $meta.launchFile=$launchFile;$meta.modules=$modules;$meta.busyInstallExit=$busyInstall;$meta.busyUninstallExit=$busyUninstall;$meta.state='Ready for normal UI close and installed conversion test'
    $meta | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $RunFile
    if((Startup-Fingerprint) -ne $before) { throw 'Startup entry changed during installer test.' }
    Write-Output $RunFile
} else {
    if(!$RunFile) { throw 'RunFile required after normal UI exit.' }
    $meta=Get-Content -LiteralPath $RunFile -Raw | ConvertFrom-Json
    $run=[IO.Path]::GetFullPath($meta.run)
    $prefix=(Join-Path $project 'evidence/private/m5-install-')
    $installed=Join-Path $run 'installed space ñ'
    if(!$run.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($run) -notmatch '^m5-install-[0-9a-f]{32}$' -or [IO.Path]::GetFullPath($meta.installed) -cne $installed) { throw 'Uninstall target is not the exact private test installation.' }
    for($current=$installed;$current;$current=[IO.Path]::GetDirectoryName($current)) {
        if((Get-Item -LiteralPath $current).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse uninstall ancestor rejected.' }
    }
    $exe=Join-Path $installed 'WinNativeFileU.App.exe'
    if(@(Get-Process -Name WinNativeFileU.App -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }).Count) { throw 'Close the installed app normally before finishing the test.' }
    $uninstaller=Join-Path $installed 'unins000.exe'
    if((Get-FileHash -LiteralPath $uninstaller -Algorithm SHA256).Hash -ne $meta.uninstallerSha256) { throw 'Uninstaller changed; refusing execution.' }
    $code=Run-Uninstall $uninstaller (Join-Path $run 'uninstall.log')
    if($code -ne 0) { throw "Uninstall failed: $code" }
    if((Test-Registration) -or (Test-Path -LiteralPath $exe)) { throw 'Owned installation files/registration remain.' }
    if((Get-FileHash -LiteralPath (Join-Path $installed 'owner-added.keep') -Algorithm SHA256).Hash -ne $meta.unknownHash -or (Get-FileHash -LiteralPath (Join-Path $run 'profile-preserve.keep') -Algorithm SHA256).Hash -ne $meta.profileHash) { throw 'Retained fixture changed during uninstall.' }
    if(@(Get-ChildItem -LiteralPath $installed -Force).Count -ne 1) { throw 'Unexpected files remain; inspect instead of deleting.' }
    $startupUnchanged=(Startup-Fingerprint) -eq $meta.startupBefore
    if(!$startupUnchanged) { throw 'Startup entry changed.' }
    @{at=(Get-Date).ToString('o');uninstallExit=$code;registrationRemoved=$true;ownedFilesRemoved=$true;unknownFilePreserved=$true;profilePreserved=$true;startupUnchanged=$startupUnchanged;scope='Test identity and private path only; normal Start menu/default location/real startup removal/clean machine untested.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'completion.json')
    Write-Output (Join-Path $run 'completion.json')
}
