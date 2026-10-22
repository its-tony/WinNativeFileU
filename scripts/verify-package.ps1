param([Parameter(Mandatory)][string]$PayloadDirectory, [switch]$Installed)
$ErrorActionPreference='Stop'
$payload=[IO.Path]::GetFullPath($PayloadDirectory).TrimEnd('\')
$manifest=Get-Content -LiteralPath (Join-Path $payload 'files.json') -Raw | ConvertFrom-Json
if($manifest.schema -ne 1 -or !$manifest.files.Count) { throw 'Unsupported or empty package manifest.' }
$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($entry in $manifest.files) {
    if([IO.Path]::IsPathFullyQualified($entry.path) -or $entry.path -match '[:\\]' -or @($entry.path.Split('/') | Where-Object {$_ -in '', '.', '..'}).Count) { throw 'Unsafe manifest path.' }
    if(!$seen.Add($entry.path)) { throw 'Duplicate manifest path.' }
    $file=Join-Path $payload $entry.path
    $item=Get-Item -LiteralPath $file
    if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Ordinary payload files required.' }
    if($item.Length -ne $entry.length -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Payload mismatch: $($entry.path)" }
}
$all=@(Get-ChildItem -LiteralPath $payload -Recurse -Force)
if(@($all | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Payload reparse point rejected.' }
$files=@($all | Where-Object {!$_.PSIsContainer})
$installerNames=@('unins000.exe','unins000.dat','unins000.msg')
$installerFiles=if($Installed) { @($files | Where-Object { $_.DirectoryName -eq $payload -and $_.Name -in $installerNames }) } else { @() }
if($files.Count -ne $seen.Count+1+$installerFiles.Count) { throw 'Unlisted file in payload.' }
foreach($file in $files) {
    $relative=[IO.Path]::GetRelativePath($payload,$file.FullName).Replace('\','/')
    if($relative -ne 'files.json' -and !$seen.Contains($relative) -and !($Installed -and $relative -in $installerNames)) { throw 'Unlisted file in payload.' }
    if($file.Extension -in '.pdb','.cs','.pfx','.key','.pem','.nupkg') { throw 'Development/private artifact in payload.' }
}
foreach($name in @('WinNativeFileU.App.exe','coreclr.dll','hostfxr.dll','hostpolicy.dll','Microsoft.UI.Xaml.dll','LICENSE.txt','DISTRIBUTION-TERMS.txt','THIRD-PARTY-NOTICES.json','USER-GUIDE.md','build-info.json')) {
    if(!(Test-Path -LiteralPath (Join-Path $payload $name) -PathType Leaf)) { throw "Required package file missing: $name" }
}
$config=Get-Content -LiteralPath (Join-Path $payload 'WinNativeFileU.App.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
if($config.runtimeOptions.ContainsKey('framework') -or $config.runtimeOptions.ContainsKey('frameworks') -or !$config.runtimeOptions.includedFrameworks) { throw 'Self-contained runtime configuration required.' }
Write-Output "PASS: $($seen.Count) payload files match manifest; local runtime and notices present. This is integrity checking, not signature trust or a clean-machine test."
