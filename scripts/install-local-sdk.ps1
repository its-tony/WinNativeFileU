param([string]$Version = '10.0.402')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$private = Join-Path $root 'evidence/private'
$tools = Join-Path $root '.tools'
New-Item -ItemType Directory -Force -Path $private,$tools | Out-Null
$metadataPath = Join-Path $private 'dotnet-releases.json'
if (!(Test-Path -LiteralPath $metadataPath)) {
    Invoke-WebRequest 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json' -OutFile $metadataPath
}
$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
$sdk = @($metadata.releases | ForEach-Object { $_.sdks } | Where-Object version -EQ $Version)[0]
if (!$sdk) { throw 'Requested SDK not found in official metadata' }
$file = @($sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.name.EndsWith('.zip') })[0]
if (!$file -or ([uri]$file.url).Host -ne 'builds.dotnet.microsoft.com') { throw 'Unexpected SDK origin' }
$archive = Join-Path $tools "dotnet-sdk-$Version-win-x64.zip"
if (!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest $file.url -OutFile $archive }
$actual = (Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash.ToLowerInvariant()
if ($actual -ne $file.hash.ToLowerInvariant()) { throw 'SDK SHA512 mismatch; archive retained for diagnosis' }
$destination = Join-Path $tools 'dotnet'
if (Test-Path -LiteralPath $destination) { throw 'SDK directory exists; inspect before replacing' }
Expand-Archive -LiteralPath $archive -DestinationPath $destination
@{ version=$Version; url=$file.url; sha512=$actual; verified=$true; date=(Get-Date).ToString('o') } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $private 'sdk-provenance.json')
& (Join-Path $destination 'dotnet.exe') --info
exit $LASTEXITCODE
