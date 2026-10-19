param([switch]$LockedRestore)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $local = Join-Path $root '.tools/dotnet/dotnet.exe'
    $dotnet = if (Test-Path -LiteralPath $local) { $local } else { (Get-Command dotnet -ErrorAction Stop).Source }
    $env:DOTNET_CLI_HOME = Join-Path $root '.tools/cli-home'
    $env:NUGET_PACKAGES = Join-Path $root '.tools/packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'
    $env:TEMP = Join-Path $root '.tmp'
    $env:TMP = $env:TEMP
    New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
    $restoreArgs = @('restore', 'WinNativeFileU.slnx', '-p:Platform=x64')
    if ($LockedRestore) { $restoreArgs += '--locked-mode' }
    & $dotnet @restoreArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $dotnet build WinNativeFileU.slnx --no-restore -c Release -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $dotnet test tests/Core.Tests/WinNativeFileU.Core.Tests.csproj --no-build --no-restore -c Release --logger 'trx;LogFileName=core.trx' --results-directory artifacts/test-results
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $dotnet test tests/WindowsIntegration.Tests/WinNativeFileU.WindowsIntegration.Tests.csproj --no-build --no-restore -c Release --logger 'trx;LogFileName=windows-integration.trx' --results-directory artifacts/test-results
    exit $LASTEXITCODE
}
finally { Pop-Location }
