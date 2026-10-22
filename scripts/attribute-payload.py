"""Attribute published binary bytes to locked package files/MSIX entries, or original build outputs."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--payload',type=Path,required=True)
parser.add_argument('--packages',type=Path,required=True)
args=parser.parse_args()
binary_suffixes={'.dll','.exe','.winmd','.mui','.pri','.manifest','.xbf'}
published=[file for file in args.payload.rglob('*') if file.is_file() and file.suffix.lower() in binary_suffixes]
def digest(file):
    with file.open('rb') as stream: return hashlib.file_digest(stream,'sha256').hexdigest()
keys={(file.name.lower(),file.stat().st_size) for file in published}
index={}
notices=json.loads((args.payload/'THIRD-PARTY-NOTICES.json').read_text())
for package in notices['dependencies']:
    name,version=package['id'],package['version'];directory=args.packages/name.lower()/version.lower()
    for file in directory.rglob('*'):
        if file.is_file() and (file.name.lower(),file.stat().st_size) in keys:
            index.setdefault(digest(file),[]).append({'package':name,'version':version,'file':str(file.relative_to(directory)).replace('\\','/')})
        elif file.is_file() and file.suffix.lower()=='.msix' and 'win10-x64' in file.parts:
            with zipfile.ZipFile(file) as archive:
                for entry in archive.infolist():
                    if (Path(entry.filename).name.lower(),entry.file_size) in keys:
                        with archive.open(entry) as stream: sha=hashlib.file_digest(stream,'sha256').hexdigest()
                        index.setdefault(sha,[]).append({'package':name,'version':version,'file':str(file.relative_to(directory)).replace('\\','/')+'!/'+entry.filename})
rows=[];unknown=[]
originals={'WinNativeFileU.App.exe','WinNativeFileU.App.dll','WinNativeFileU.App.pri','WinNativeFileU.Core.dll','WinNativeFileU.Converters.dll','WinNativeFileU.WindowsIntegration.dll'}
for file in sorted(published):
    sha=digest(file);relative=str(file.relative_to(args.payload)).replace('\\','/')
    if file.parent==args.payload and file.name in originals:
        sources=[{'project':'WinNativeFileU','role':'Original compiled/generated output; App.exe includes the .NET apphost template covered by bundled runtime terms'}]
    else: sources=index.get(sha,[])
    if not sources: unknown.append(relative)
    rows.append({'path':relative,'sha256':sha,'sources':sources})
result={'scope':'Exact SHA256 byte matches against local NuGet packages and their x64 MSIX entries; original app outputs classified separately. Attribution is not signature trust, a legal opinion or dependency minimization.',
        'binaries':rows,'unattributed':unknown}
with (args.payload/'BINARY-ATTRIBUTION.json').open('x',encoding='utf-8') as stream: json.dump(result,stream,indent=2)
if unknown: raise ValueError('Unattributed binary payload: '+', '.join(unknown))
print(f'Attributed {len(rows)} binary/resource files; no unknown binary bytes.')
