"""Stage unchanged local package notices. This is an inventory, not legal approval."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--payload',type=Path,required=True)
parser.add_argument('--packages',type=Path,required=True)
parser.add_argument('--runtime-version',required=True)
args=parser.parse_args()
target=args.payload/'licenses'
target.mkdir(exist_ok=False)
graph={}
for group in json.loads((root/'src/App/packages.lock.json').read_text())['dependencies'].values():
    for name,info in group.items():
        if 'resolved' in info: graph[(name,info['resolved'])]=info['type']
excluded={'Microsoft.Windows.SDK.BuildTools','Microsoft.Windows.SDK.BuildTools.MSIX'}
graph[('Microsoft.NETCore.App.Runtime.win-x64',args.runtime_version)]='Bundled .NET runtime'
deps=json.loads((args.payload/'WinNativeFileU.App.deps.json').read_text())
for name,info in deps['libraries'].items():
    if info['type']=='runtimepack':
        ident,version=name.split('/',1)
        graph[(ident.removeprefix('runtimepack.'),version)]='Published runtime pack'
inventory=[]
for (name,version),kind in sorted(graph.items()):
    if name in excluded: continue
    directory=args.packages/name.lower()/version.lower()
    if not directory.is_dir(): raise ValueError(f'Missing package for notice attribution: {name}/{version}')
    specs=list(directory.glob('*.nuspec'))
    if len(specs)!=1: raise ValueError(f'Expected one nuspec: {name}')
    document=ET.parse(specs[0]).getroot()
    metadata=next(node for node in document if node.tag.split('}')[-1]=='metadata')
    license_node=next((node for node in metadata if node.tag.split('}')[-1]=='license'),None)
    files=[file for file in directory.rglob('*') if file.is_file() and file.suffix.lower() in {'.txt','.md','.rtf'}
           and any(token in file.name.lower() for token in ('license','notice','eula','redist'))]
    if license_node is not None and license_node.attrib.get('type')=='file':
        declared=directory/(license_node.text or '')
        if not declared.is_file(): raise ValueError(f'Declared license missing: {name}')
        if declared not in files: files.append(declared)
    output=target/(name+'-'+version)
    notices=[]
    if name=='Microsoft.Windows.SDK.NET.Ref':
        if version!='10.0.22000.57': raise ValueError('SDK projection version changed; review its runtime notices before updating the baseline')
        files=[root/'.tools/licenses/windows-sdk-license.rtf',root/'.tools/licenses/cswinrt-LICENSE.txt']
        for file in files:
            if not file.is_file(): raise ValueError('Prepare pinned SDK projection licenses first')
            dest=output/file.name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(file,dest)
            notices.append({'path':str(dest.relative_to(args.payload)).replace('\\','/'),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
        files=[]
    elif not files: raise ValueError(f'No local notice/license found: {name}')
    for file in sorted(files):
        relative=file.relative_to(directory);dest=output/relative
        dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(file,dest)
        notices.append({'path':str(dest.relative_to(args.payload)).replace('\\','/'),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
    inventory.append({'id':name,'version':version,'role':kind,'scope':'Notices staged from dependency graph; exact binary attribution reviewed separately','notices':notices})
inno=root/'.tools/inno-7.1.0-x64/license.txt'
if not inno.is_file(): raise ValueError('Pinned installer compiler license missing')
shutil.copyfile(inno,target/'Inno-Setup-7.1.0.txt')
shutil.copyfile(root/'LICENSE',args.payload/'LICENSE.txt')
sdk_license=args.packages/'microsoft.windowsappsdk/2.5.1/license.txt'
terms=("WinNativeFileU source code is licensed under the MIT License included below.\n"
       "Bundled third-party binaries retain their own licenses. Their applicable terms and notices are included under licenses/.\n"
       "The Microsoft Windows App SDK binary terms follow the source license and apply to those components.\n"
       "Additional Windows SDK projection terms and CsWinRT notices are included under licenses/Microsoft.Windows.SDK.NET.Ref-10.0.22000.57/.\n"
       "Inno Setup is used to build this installer; its notices are preserved.\n\n"
       + (root/'LICENSE').read_text(encoding='utf-8')+'\n\n'+sdk_license.read_text(encoding='utf-8-sig'))
(args.payload/'DISTRIBUTION-TERMS.txt').write_text(terms,encoding='utf-8')
(args.payload/'THIRD-PARTY-NOTICES.json').write_text(json.dumps({'scope':'Local notices and locked App graph plus bundled .NET runtime; build tools excluded, no blanket MIT relicense or legal approval','dependencies':inventory,'installer':'Inno Setup7.1.0, original runtime notices preserved'},indent=2)+'\n',encoding='utf-8')
print(f'Staged notices for {len(inventory)} package/version pairs plus Inno Setup; build tools excluded.')
