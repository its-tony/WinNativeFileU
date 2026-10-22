$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$directory=Join-Path $project '.tools/licenses'
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$items=@(
    @{name='windows-sdk-license.rtf';url='https://download.microsoft.com/download/0/F/F/0FF2B061-47DD-4F55-89B6-FD1D8C44F14D/sdk_license.rtf';sha256='DD07EB178E00C6BBA4148457FC00FF77CD4887EB521D504186FE59C9EC8BBE62'},
    @{name='cswinrt-LICENSE.txt';url='https://raw.githubusercontent.com/microsoft/CsWinRT/8649ee3eeb2445ca2a36d80d878ef60b96a6c65d/LICENSE';sha256='9906940F61B1F0B533FA7D99BAF55178B2808FBE113EA51DFBFAD8572CCD5F2B'}
)
foreach($item in $items) {
    $path=Join-Path $directory $item.name
    if(!(Test-Path -LiteralPath $path)) { Invoke-WebRequest -Uri $item.url -OutFile $path }
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $item.sha256) { throw "Pinned license hash mismatch: $($item.name)" }
}
Write-Output 'Pinned Windows SDK and CsWinRT license sources verified.'
