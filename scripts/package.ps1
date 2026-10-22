param([Parameter(Mandatory)][string]$OutputRoot, [switch]$AllowDirtySource)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$output=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $output) { throw 'Package output already exists; use a new directory and preserve previous evidence.' }
$compiler=Join-Path $project '.tools/inno-7.1.0-x64/ISCC.exe'
if(!(Test-Path -LiteralPath $compiler)) { throw 'Prepare the pinned compiler with scripts/install-local-inno.ps1 first.' }
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash -ne 'D06EBD38F38E3CEE60A3C50CC45BD449D77E0BC6A5CABC607EA9886808E4DE1A') { throw 'Pinned compiler bytes changed; inspect provenance.' }
$compilerVersion=& $compiler --version
if($LASTEXITCODE -ne 0 -or "$compilerVersion" -notmatch '7\.1\.0') { throw 'Unexpected compiler version.' }
$productPaths=@('src','build','Directory.Build.props','installer','scripts/package.ps1','scripts/stage-licenses.py','scripts/attribute-payload.py','scripts/verify-package.ps1','scripts/prepare-distribution-licenses.ps1','scripts/install-local-inno.ps1','docs/USER-GUIDE.md')
$initialChanges=@(& git -C $project status --porcelain --untracked-files=all -- @productPaths)
if($initialChanges.Count -gt 0 -and !$AllowDirtySource) { throw 'Commit the product/packaging inputs first, or explicitly use AllowDirtySource for a diagnostic staging.' }
[xml]$versionXml=Get-Content -LiteralPath (Join-Path $project 'build/Version.props')
$version=$versionXml.Project.PropertyGroup.VersionPrefix+'-'+$versionXml.Project.PropertyGroup.VersionSuffix
$numeric=$versionXml.Project.PropertyGroup.FileVersion
[xml]$app=Get-Content -LiteralPath (Join-Path $project 'src/App/WinNativeFileU.App.csproj')
$runtime=$app.Project.PropertyGroup.BundledRuntimeVersion
$dotnet=Join-Path $project '.tools/dotnet/dotnet.exe'
if(!(Test-Path -LiteralPath $dotnet)) { $dotnet=(Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME=Join-Path $project '.tools/cli-home'
$env:NUGET_PACKAGES=Join-Path $project '.tools/packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_NOLOGO='1'
$env:TEMP=Join-Path $project '.tmp';$env:TMP=$env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
$payload=Join-Path $output 'payload'
# The App owns win-x64 in its csproj. A global -r would also change the libraries' locked restore graphs.
& $dotnet publish (Join-Path $project 'src/App/WinNativeFileU.App.csproj') -c Release --self-contained true -p:Platform=x64 -p:RestoreLockedMode=true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false --output $payload
if($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed; output preserved.' }
& (Join-Path $PSScriptRoot 'prepare-distribution-licenses.ps1')
& python (Join-Path $PSScriptRoot 'stage-licenses.py') --payload $payload --packages $env:NUGET_PACKAGES --runtime-version $runtime
if($LASTEXITCODE -ne 0) { throw 'Notice attribution failed; output preserved.' }
& python (Join-Path $PSScriptRoot 'attribute-payload.py') --payload $payload --packages $env:NUGET_PACKAGES
if($LASTEXITCODE -ne 0) { throw 'Binary attribution failed; output preserved.' }
Copy-Item -LiteralPath (Join-Path $project 'docs/USER-GUIDE.md') -Destination (Join-Path $payload 'USER-GUIDE.md')
$source=(& git -C $project rev-parse HEAD).Trim()
$sourceChanges=@(& git -C $project status --porcelain --untracked-files=all -- @productPaths)
if($sourceChanges.Count -gt 0 -and !$AllowDirtySource) { throw 'Product/packaging inputs changed during staging; output preserved for diagnosis.' }
$repositoryChanges=@(& git -C $project status --porcelain)
@{version=$version;fileVersion=$numeric;runtimeVersion=$runtime;sourceCommit=$source;sourceChangesPresent=($sourceChanges.Count -gt 0);repositoryChangesPresent=($repositoryChanges.Count -gt 0);sdk=(& $dotnet --version);platform='Windows11-x64';deployment='self-contained folder';signing='unsigned local candidate'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $payload 'build-info.json')
$entries=@(Get-ChildItem -LiteralPath $payload -Recurse -File | Sort-Object FullName | ForEach-Object { @{path=[IO.Path]::GetRelativePath($payload,$_.FullName).Replace('\','/');length=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
@{schema=1;files=$entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $payload 'files.json')
& (Join-Path $PSScriptRoot 'verify-package.ps1') -PayloadDirectory $payload
$zip=Join-Path $output ("WinNativeFileU-$version-win-x64.zip")
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $zip -CompressionLevel Fastest
$installerArgs=@(("--define=PayloadDir=$payload"),("--define=AppVersion=$version"),("--define=NumericVersion=$numeric"))
& $compiler --quiet @installerArgs ("--output-dir=$output") (Join-Path $project 'installer/WinNativeFileU.iss')
if($LASTEXITCODE -ne 0) { throw 'Installer compile failed; output preserved.' }
$testOutput=Join-Path $output 'test';New-Item -ItemType Directory -Path $testOutput | Out-Null
& $compiler --quiet @installerArgs '--define=TestInstall' ("--output-dir=$testOutput") '--output-filename=WinNativeFileU-test-setup' (Join-Path $project 'installer/WinNativeFileU.iss')
if($LASTEXITCODE -ne 0) { throw 'Test-identity installer compile failed; output preserved.' }
$setup=Join-Path $output ("WinNativeFileU-$version-win-x64-setup.exe")
$releaseFiles=@($zip,$setup)
@($releaseFiles | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($_) }) | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt')
@{at=(Get-Date).ToString('o');version=$version;payload=$payload;archive=$zip;installer=$setup;testInstaller=(Join-Path $testOutput 'WinNativeFileU-test-setup.exe');fileCount=$entries.Count;sourceCommit=$source;sourceChangesPresent=($sourceChanges.Count -gt 0);unsigned=$true;scope='Local release preparation only; not published or clean-machine qualified.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'package.json')
Write-Output (Join-Path $output 'package.json')
