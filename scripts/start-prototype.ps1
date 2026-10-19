param([switch]$PrepareOnly,
    [ValidateSet('FunctionKeys', 'Letters')][string]$ShortcutProfile = 'FunctionKeys')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $root "evidence/private/m1-$id"
$fixtures = Join-Path $run 'fixtures'
New-Item -ItemType Directory -Path $fixtures -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $fixtures 'fixture-one.txt'), 'Disposable M1 fixture one.')
[IO.File]::WriteAllText((Join-Path $fixtures 'niño-dos.txt'), 'Disposable Unicode fixture two.')
$before = @(Get-ChildItem -LiteralPath $fixtures -File | ForEach-Object {
    @{ name=$_.Name; length=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$exe = Join-Path $root 'src/App/bin/Release/net10.0-windows10.0.22000.0/win-x64/WinNativeFileU.App.exe'
$log = Join-Path $run 'events.jsonl'
$profileName = if ($ShortcutProfile -eq 'Letters') { 'letters' } else { 'function-keys' }
$metadata = @{ run=$run; fixtures=$fixtures; log=$log; executable=$exe; shortcutProfile=$profileName; before=$before; date=(Get-Date).ToString('o') }
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'run.json')
if (!$PrepareOnly) {
    if (!(Test-Path -LiteralPath $exe)) { throw 'Build Release before launching the prototype.' }
    # Hidden startup avoids a launcher console; the prototype's WinUI is the test surface.
    $process = Start-Process -FilePath $exe -ArgumentList @('--prototype-root', ('"'+$fixtures+'"'), '--prototype-log', ('"'+$log+'"'), '--prototype-shortcuts', $profileName) -WindowStyle Hidden -PassThru
    $process.Id | Set-Content -LiteralPath (Join-Path $run 'pid.txt')
}
Write-Output (Join-Path $run 'run.json')
