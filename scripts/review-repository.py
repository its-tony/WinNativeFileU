"""Review candidate paths before local commits; not a universal secret scanner."""
from pathlib import Path
import json
import subprocess
import sys

root = Path(__file__).resolve().parents[1]

def git(*args):
    return subprocess.check_output(['git', '-C', str(root), *args], text=True).splitlines()

paths = sorted(set(git('ls-files', '--cached', '--others', '--exclude-standard')))
blocked = []
suffixes = {'.exe', '.dll', '.dmg', '.pkg', '.ipa', '.pfx', '.key', '.pem', '.har', '.pcap', '.pcapng', '.zip'}
for name in paths:
    path = Path(name)
    if path.suffix.lower() in suffixes or any(p in {'private', '.tools', 'bin', 'obj', 'research-artifacts'} for p in path.parts):
        blocked.append(name)
    if path.name.startswith('.env') and path.name != '.env.example':
        blocked.append(name)

required = ['overview.md', 'interaction-analysis.md', 'reverse-engineering.md',
            'windows-equivalents.md', 'interaction-state-machine.md', 'feasibility.md', 'sources.md']
missing = [name for name in required if not (root / 'docs/research/tangerine' / name).is_file()]
whitespace = subprocess.run(['git', '-C', str(root), 'diff', '--check'], capture_output=True, text=True)
staged = subprocess.run(['git', '-C', str(root), 'diff', '--cached', '--check'], capture_output=True, text=True)
print(json.dumps({'candidate_count': len(paths), 'blocked_paths': sorted(set(blocked)),
                  'missing_research': missing, 'diff_check_exit': whitespace.returncode,
                  'staged_diff_check_exit': staged.returncode,
                  'scope': 'path policy and whitespace only; manual content review still required'}, indent=2))
sys.exit(1 if blocked or missing or whitespace.returncode or staged.returncode else 0)
