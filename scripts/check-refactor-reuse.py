"""Check the explicitly unchanged components inherited from the pinned refactor revision."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', help='Read target files from a Git revision instead of the working tree')
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    manifest = json.loads((root / 'scripts/refactor-reuse.json').read_text(encoding='utf-8'))
    failures = []
    for item in manifest['files']:
        if args.revision:
            content = subprocess.check_output(['git', 'show', f"{args.revision}:{item['target']}"], cwd=root)
        else:
            content = (root / item['target']).read_bytes()
        normalized = content.decode('utf-8-sig').replace('\r\n', '\n').encode('utf-8')
        if hashlib.sha256(normalized).hexdigest() != item['sha256_lf']:
            failures.append(item['target'])
    if failures:
        print('Reference copies changed; review and update the exact/adapted provenance classification:', file=sys.stderr)
        print('\n'.join(failures), file=sys.stderr)
        return 1
    print(f"PASS: {len(manifest['files'])} whole files exactly match refactor {manifest['reference'][:7]} (line endings normalized).")
    return 0


if __name__ == '__main__':
    sys.exit(main())
