param([switch]$PrepareOnly)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$run = Join-Path $project ('evidence/private/m3-' + [guid]::NewGuid().ToString('N'))
$inputs = Join-Path $run 'inputs'
$outputs = Join-Path $run 'outputs'
New-Item -ItemType Directory -Path $inputs, $outputs -ErrorAction Stop | Out-Null

function Write-BmpFixture([string]$Path, [int]$Width, [int]$Height, [switch]$ColorGrid) {
    $stride = [int]([Math]::Ceiling($Width * 3 / 4.0) * 4)
    $data = [byte[]]::new(54 + $stride * $Height)
    $data[0] = 66; $data[1] = 77
    [BitConverter]::GetBytes([int]$data.Length).CopyTo($data, 2)
    [BitConverter]::GetBytes([int]54).CopyTo($data, 10)
    [BitConverter]::GetBytes([int]40).CopyTo($data, 14)
    [BitConverter]::GetBytes($Width).CopyTo($data, 18)
    [BitConverter]::GetBytes([int](-$Height)).CopyTo($data, 22)
    [BitConverter]::GetBytes([int16]1).CopyTo($data, 26)
    [BitConverter]::GetBytes([int16]24).CopyTo($data, 28)
    if ($ColorGrid) {
        [byte[]]$colors = @(0,0,255, 0,255,0, 255,0,0, 0,255,255, 255,0,255, 255,255,255)
        [Array]::Copy($colors, 0, $data, 54, 9)
        [Array]::Copy($colors, 9, $data, 54 + $stride, 9)
    }
    $file = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $file.Write($data, 0, $data.Length) } finally { $file.Dispose() }
}

Write-BmpFixture (Join-Path $inputs 'color-grid.bmp') 3 2 -ColorGrid
Write-BmpFixture (Join-Path $inputs 'large-opaque.bmp') 2048 2048
$before = @(Get-ChildItem -LiteralPath $inputs -File | ForEach-Object {
    @{ name=$_.Name; length=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$exe = Join-Path $project 'src/App/bin/Release/net10.0-windows10.0.22000.0/win-x64/WinNativeFileU.App.exe'
$log = Join-Path $run 'events.jsonl'
$metadata = @{ run=$run; inputs=$inputs; outputs=$outputs; log=$log; executable=$exe; before=$before; date=(Get-Date).ToString('o') }
$runFile = Join-Path $run 'run.json'
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $runFile
if (!$PrepareOnly) {
    if (!(Test-Path -LiteralPath $exe)) { throw 'Build Release before launching the workbench.' }
    $arguments = @('--conversion-workbench', '--conversion-root', ('"'+$run+'"'), '--conversion-log', ('"'+$log+'"'))
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.Id | Set-Content -LiteralPath (Join-Path $run 'pid.txt')
}
Write-Output $runFile
