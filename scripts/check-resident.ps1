param([Parameter(Mandatory)][string]$RunFile, [switch]$ExpectExited, [switch]$MeasureIdle)
$ErrorActionPreference = 'Stop'
$meta = Get-Content -LiteralPath $RunFile -Raw | ConvertFrom-Json
$key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
try { $value = if ($key) { $key.GetValue('WinNativeFileU', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) } else { $null } }
finally { if ($key) { $key.Dispose() } }
$fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $value -Compress))))
$process = Get-Process -Id $meta.pid -ErrorAction SilentlyContinue
$running = $process -and $process.Path -eq $meta.executable
$cpu = $null
if ($MeasureIdle -and $running) {
    $initial = $process.TotalProcessorTime.TotalMilliseconds
    $watch = [Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds 3
    $process.Refresh()
    $cpu = @{ elapsedMs=$watch.Elapsed.TotalMilliseconds; cpuMs=($process.TotalProcessorTime.TotalMilliseconds-$initial); workingSetBytes=$process.WorkingSet64; handles=$process.HandleCount }
}
$partials = @(Get-ChildItem -LiteralPath $meta.run -Filter '*.partial' -File).Count
$result = @{ at=(Get-Date).ToString('o'); startupUnchanged=($fingerprint -eq $meta.startupFingerprint); running=[bool]$running; partialCount=$partials; idleSample=$cpu; scope='Process sample, not a benchmark; no logon or Explorer restart validation.' }
$path = Join-Path $meta.run ('check-'+[guid]::NewGuid().ToString('N')+'.json')
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path
if (!$result.startupUnchanged) { throw 'Sign-in value changed; inspect private evidence.' }
if ($partials -ne 0) { throw 'Settings temporary remains; inspect private evidence.' }
if ($ExpectExited -and $running) { throw 'The resident is still running.' }
Write-Output $path
