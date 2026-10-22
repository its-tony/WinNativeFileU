# Official compiler, used only for building installers. Portable preparation does not register the tool.
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$tools=Join-Path $project '.tools'
$private=Join-Path $project 'evidence/private'
$destination=Join-Path $tools 'inno-7.1.0-x64'
if(Test-Path -LiteralPath $destination) { throw 'Compiler directory already exists; inspect before replacing it.' }
New-Item -ItemType Directory -Force -Path $tools,$private | Out-Null
$source='https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe'
$signatureSource='https://files.jrsoftware.org/is/7/innosetup-7.1.0-x64.exe.issig'
$archive=Join-Path $tools 'innosetup-7.1.0-x64.exe'
$signatureFile=Join-Path $tools 'innosetup-7.1.0-x64.exe.issig'
if(!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri $source -OutFile $archive }
if(!(Test-Path -LiteralPath $signatureFile)) { Invoke-WebRequest -Uri $signatureSource -OutFile $signatureFile }
$signatureText=Get-Content -LiteralPath $signatureFile -Raw
if($signatureText -notmatch '(?m)^file-hash ([0-9a-fA-F]{64})\s*$') { throw 'Expected upstream signature-file hash not found.' }
$expected=$Matches[1]
$actual=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
if($actual -ine $expected -or $actual -ne '0362A383ED217D4C4239B5933866DD96D3EB2102737DA92F80F6057A4B40DF2F') { throw 'Compiler download hash mismatch; files preserved for diagnosis.' }
$auth=Get-AuthenticodeSignature -LiteralPath $archive
if($auth.Status -ne 'Valid' -or $auth.SignerCertificate.Subject -notmatch '(?:^|,\s*)O=Pyrsys B\.V\.(?:,|$)') { throw 'Expected valid Pyrsys B.V. Authenticode signature.' }
$log=Join-Path $private ('inno-portable-'+[guid]::NewGuid().ToString('N')+'.log')
$proc=Start-Process -FilePath $archive -ArgumentList @('/PORTABLE=1','/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/DIR="'+$destination+'"'),('/LOG="'+$log+'"')) -WindowStyle Hidden -Wait -PassThru
if($proc.ExitCode -ne 0) { throw "Portable compiler preparation failed: $($proc.ExitCode)" }
$compiler=Join-Path $destination 'ISCC.exe'
if(!(Test-Path -LiteralPath $compiler)) { throw 'Compiler missing after portable preparation.' }
$provenance=Join-Path $private ('inno-provenance-'+[guid]::NewGuid().ToString('N')+'.json')
@{at=(Get-Date).ToString('o');version='7.1.0';source=$source;signatureSource=$signatureSource;sha256=$actual;hashMatches=$true;authenticode=$auth.Status.ToString();signer=$auth.SignerCertificate.Subject;compilerSha256=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash;portable=$true;log=$log} | ConvertTo-Json | Set-Content -LiteralPath $provenance
& $compiler /?
if($LASTEXITCODE -notin 0,1) { throw 'Unexpected compiler help result.' }
Write-Output $provenance
