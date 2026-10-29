"""Verify that every top-level type in src/MCUGLauncher has a class page in docs/classes."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
APP = ROOT / 'src/MCUGLauncher'
DOCS = ROOT / 'docs/classes'

TYPE_RE = re.compile(
    r'^(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|unsafe|file|new)\s+)*'
    r'(class|record|struct|enum|interface)\s+([A-Za-z_]\w*)',
    re.MULTILINE)

LINK_RE = re.compile(r'\]\(([^)]+)\)')


def collect_types():
    found = {}
    for source in sorted(APP.rglob('*.cs')):
        if any(part in ('bin', 'obj') for part in source.parts):
            continue
        text = source.read_text(encoding='utf-8-sig')
        relative = source.relative_to(APP).as_posix()
        for match in TYPE_RE.finditer(text):
            line_start = text.rfind('\n', 0, match.start()) + 1
            if text[line_start:match.start()].strip():
                continue
            found.setdefault(match.group(2), set()).add(relative)
    return found


def doc_path(name, files):
    ordered = sorted(files, key=lambda value: (value.count('/'), value))
    root_file = next((value for value in ordered if '/' not in value), ordered[0])
    group = '' if '/' not in root_file else root_file.rsplit('/', 1)[0]
    return DOCS / group / f'{name}.md'


def main():
    errors = []
    types = collect_types()
    expected = {}
    for name, files in types.items():
        expected.setdefault(name, doc_path(name, files))

    for name, path in sorted(expected.items()):
        if not path.is_file():
            errors.append(f'{path.relative_to(ROOT)}: missing page for {name}')
            continue
        text = path.read_text(encoding='utf-8')
        if not text.startswith(f'# {name}\n'):
            errors.append(f'{path.relative_to(ROOT)}: heading must be `# {name}`')
        lines = [line for line in text.splitlines() if line.strip()]
        if len(lines) < 3:
            errors.append(f'{path.relative_to(ROOT)}: responsibility paragraph is missing')
        if '**源码：**' not in text:
            errors.append(f'{path.relative_to(ROOT)}: source link line is missing')
        for target in LINK_RE.findall(text):
            if target.startswith(('http://', 'https://')):
                continue
            resolved = (path.parent / target).resolve()
            if not resolved.exists():
                errors.append(f'{path.relative_to(ROOT)}: broken link {target}')

    readme = DOCS / 'README.md'
    if not readme.is_file():
        errors.append('docs/classes/README.md: index is missing')
    else:
        index = readme.read_text(encoding='utf-8')
        for name, path in sorted(expected.items()):
            href = path.relative_to(DOCS).as_posix()
            if f']({href})' not in index:
                errors.append(f'docs/classes/README.md: missing index entry for {href}')

    known = {path.resolve() for path in expected.values()}
    if DOCS.is_dir():
        for page in sorted(DOCS.rglob('*.md')):
            if page.name == 'README.md':
                continue
            if page.resolve() not in known:
                errors.append(f'{page.relative_to(ROOT)}: page has no matching type')

    if errors:
        print('\n'.join(errors), file=sys.stderr)
        return 1
    print(f'PASS: {len(expected)} class pages cover src/MCUGLauncher; index and links are intact.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
