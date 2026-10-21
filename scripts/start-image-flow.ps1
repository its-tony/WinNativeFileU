param([switch]$IncludeCancellationFixtures)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$run = Join-Path $project ('evidence/private/m5-flow-'+[guid]::NewGuid().ToString('N'))
$inputs = Join-Path $run 'inputs'; $outputs = Join-Path $run 'outputs'
New-Item -ItemType Directory -Path $inputs,$outputs -ErrorAction Stop | Out-Null
function Write-Bmp([string]$Path, [int]$Width, [int]$Height, [switch]$Grid) {
    $stride = [int]([Math]::Ceiling($Width*3/4.0)*4)
    $bytes = [byte[]]::new(54+$stride*$Height)
    $bytes[0]=66; $bytes[1]=77
    [BitConverter]::GetBytes([int]$bytes.Length).CopyTo($bytes,2)
    [BitConverter]::GetBytes([int]54).CopyTo($bytes,10)
    [BitConverter]::GetBytes([int]40).CopyTo($bytes,14)
    [BitConverter]::GetBytes($Width).CopyTo($bytes,18)
    [BitConverter]::GetBytes([int](-$Height)).CopyTo($bytes,22)
    [BitConverter]::GetBytes([int16]1).CopyTo($bytes,26)
    [BitConverter]::GetBytes([int16]24).CopyTo($bytes,28)
    if ($Grid) {
        [byte[]]$colors=@(0,0,255,0,255,0,255,0,0,0,255,255,255,0,255,255,255,255)
        [Array]::Copy($colors,0,$bytes,54,9); [Array]::Copy($colors,9,$bytes,54+$stride,9)
    }
    $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
}
Write-Bmp (Join-Path $inputs 'one.bmp') 3 2 -Grid
Write-Bmp (Join-Path $inputs 'niño dos.bmp') 3 2 -Grid
$cancelInputs = $null
if ($IncludeCancellationFixtures) {
    if (([IO.DriveInfo]::new([IO.Path]::GetPathRoot($run))).AvailableFreeSpace -lt 600MB) { throw 'Insufficient free space for disposable cancellation fixtures.' }
    $cancelInputs=Join-Path $run 'cancel-inputs'
    New-Item -ItemType Directory -Path $cancelInputs -ErrorAction Stop | Out-Null
    for ($index=1;$index -le 8;$index++) { Write-Bmp (Join-Path $cancelInputs ("large-$index.bmp")) 4000 4000 }
}
$before=@(Get-ChildItem -LiteralPath $inputs -File | ForEach-Object { @{path=$_.FullName;length=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
if ($cancelInputs) { $before+=@(Get-ChildItem -LiteralPath $cancelInputs -File | ForEach-Object { @{path=$_.FullName;length=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} }) }
$meta=@{run=$run;inputs=$inputs;outputs=$outputs;cancelInputs=$cancelInputs;before=$before;date=(Get-Date).ToString('o')}
$runFile=Join-Path $run 'run.json';$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $runFile
$launchFile=& (Join-Path $PSScriptRoot 'start-resident.ps1') -RunFile $runFile
$session=@{runFile=$runFile;launchFile=$launchFile;run=$run}
$sessionPath=Join-Path $run 'session.json';$session | ConvertTo-Json | Set-Content -LiteralPath $sessionPath
Write-Output $sessionPath
