param([Parameter(Mandatory)][string]$RunFile)
$ErrorActionPreference = 'Stop'
$meta = Get-Content -LiteralPath $RunFile -Raw | ConvertFrom-Json
$after = @(Get-ChildItem -LiteralPath $meta.inputs -File | ForEach-Object {
    @{ name=$_.Name; length=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$unchanged = $after.Count -eq $meta.before.Count
foreach ($original in $meta.before) {
    if (@($after | Where-Object { $_.name -ceq $original.name -and $_.length -eq $original.length -and $_.sha256 -eq $original.sha256 }).Count -ne 1) { $unchanged = $false }
}
$results = @(Get-ChildItem -LiteralPath $meta.outputs -File | ForEach-Object {
    @{ name=$_.Name; length=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$partials = @(Get-ChildItem -LiteralPath $meta.outputs -Filter '.winnu-*.partial' -File).Count
@{ date=(Get-Date).ToString('o'); originalsUnchanged=$unchanged; partialCount=$partials; outputs=$results; after=$after } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $meta.run ('check-'+[guid]::NewGuid().ToString('N')+'.json'))
if (!$unchanged) { throw 'Input manifest changed; inspect private evidence.' }
if ($partials -ne 0) { throw 'A temporary output remains; investigate instead of claiming cleanup.' }
Write-Output "PASS: inputs unchanged, no partial files; $($results.Count) generated output(s). This does not validate image pixels."
