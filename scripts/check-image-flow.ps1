param([Parameter(Mandatory)][string]$SessionFile)
$ErrorActionPreference='Stop'
$session=Get-Content -LiteralPath $SessionFile -Raw | ConvertFrom-Json
$meta=Get-Content -LiteralPath $session.runFile -Raw | ConvertFrom-Json
$unchanged=$true
foreach($item in $meta.before) {
    if (!(Test-Path -LiteralPath $item.path -PathType Leaf)) { $unchanged=$false;continue }
    $file=Get-Item -LiteralPath $item.path
    if($file.Length -ne $item.length -or (Get-FileHash -LiteralPath $item.path -Algorithm SHA256).Hash -ne $item.sha256) { $unchanged=$false }
}
$partials=@(Get-ChildItem -LiteralPath $meta.outputs -File -Filter '.winnu-*.partial').Count
$outputs=@(Get-ChildItem -LiteralPath $meta.outputs -File | ForEach-Object { @{name=$_.Name;length=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
$path=Join-Path $meta.run ('check-images-'+[guid]::NewGuid().ToString('N')+'.json')
@{at=(Get-Date).ToString('o');originalsUnchanged=$unchanged;partialCount=$partials;outputs=$outputs;scope='Hashes and owned partials only, not pixel validation.'} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path
if(!$unchanged) { throw 'Original fixture changed; inspect private evidence.' }
if($partials -ne 0) { throw 'Owned temporary remains; inspect private evidence.' }
Write-Output $path
