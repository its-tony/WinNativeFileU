param([Parameter(Mandatory)][string]$RunFile)
$ErrorActionPreference = 'Stop'
$meta = Get-Content -LiteralPath $RunFile -Raw | ConvertFrom-Json
$after = @(Get-ChildItem -LiteralPath $meta.fixtures -File | ForEach-Object {
    @{ name=$_.Name; length=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$unchanged = $after.Count -eq $meta.before.Count
foreach ($before in $meta.before) {
    $match = @($after | Where-Object { $_.name -ceq $before.name -and $_.length -eq $before.length -and $_.sha256 -eq $before.sha256 })
    if ($match.Count -ne 1) { $unchanged = $false }
}
@{ date=(Get-Date).ToString('o'); unchanged=$unchanged; before=$meta.before; after=$after } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $meta.run ('check-'+[guid]::NewGuid().ToString('N')+'.json'))
if (!$unchanged) { throw 'Fixture manifest changed; investigate before another drag experiment.' }
Write-Output 'PASS: fixture names, lengths and SHA256 hashes unchanged.'
