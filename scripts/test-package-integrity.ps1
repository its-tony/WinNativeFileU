param([Parameter(Mandatory)][string]$PackageFile)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$package=Get-Content -LiteralPath $PackageFile -Raw | ConvertFrom-Json
$source=[IO.Path]::GetFullPath($package.payload)
$archive=[IO.Path]::GetFullPath($package.archive)
$root=Join-Path $project ('evidence/private/m5-integrity-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$copy=Join-Path $root 'extracted'
$before=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
Expand-Archive -LiteralPath $archive -DestinationPath $copy
& (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $copy
$guide=Join-Path $copy 'USER-GUIDE.md'
[IO.File]::AppendAllText($guide,"`nIntentional test mutation.`n")
$mutationDetected=$false
try { & (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $copy }
catch { if($_.Exception.Message -notlike 'Payload mismatch:*') { throw };$mutationDetected=$true }
if(!$mutationDetected) { throw 'Changed payload was not detected.' }
Copy-Item -LiteralPath $guide -Destination (Join-Path $root 'mutated-guide.md')
Copy-Item -LiteralPath (Join-Path $source 'USER-GUIDE.md') -Destination $guide
& (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $copy
[IO.File]::WriteAllText((Join-Path $copy 'unlisted.keep'),'Unlisted test file must reject this copy.')
$extraDetected=$false
try { & (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $copy }
catch { if($_.Exception.Message -notlike 'Unlisted file*') { throw };$extraDetected=$true }
if(!$extraDetected) { throw 'Unlisted payload file was not detected.' }
if((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $before) { throw 'Archive changed during verification.' }
& (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $source
$result=Join-Path $root 'result.json'
@{at=(Get-Date).ToString('o');zipExtractVerified=$true;changedFileRejected=$mutationDetected;unlistedFileRejected=$extraDetected;sourceAndArchivePreserved=$true;archiveSha256=$before;scope='Integrity negative cases on disposable extracted copy; checksum/manifest are not publisher authentication.'} | ConvertTo-Json | Set-Content -LiteralPath $result
Write-Output $result
