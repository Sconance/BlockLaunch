from pathlib import Path
import re
import sys
root = Path(__file__).resolve().parent.parent
errors = []
shell = (root / 'src/MCUGLauncher/MainWindow.xaml.cs').read_text(encoding='utf-8-sig')
for token in ('new HttpClient', 'new ConfigStore', 'LaunchService.', 'JsonDocument.Parse', 'File.WriteAllText'):
    if token in shell:
        errors.append(f'MainWindow must delegate application work: {token}')
for source in (root / 'src/MCUGLauncher/UseCases').glob('*.cs'):
    text = source.read_text(encoding='utf-8-sig')
    for token in ('using System.Windows', 'Browser.', 'MainWindow', 'new Button', 'new Grid'):
        if token in text:
            errors.append(f'{source.name}: UI dependency {token}')
front = '\n'.join(p.read_text(encoding='utf-8-sig') for p in (root / 'src/MCUGLauncher/Ui').glob('*.js'))
router = (root / 'src/MCUGLauncher/UseCases/LauncherApplication.cs').read_text(encoding='utf-8-sig')
commands = set(re.findall(r"(?:post|postMessage)\(\{\s*type:\s*'([^']+)'", front))
handled = set(re.findall(r'case "([^"]+)":', router))
for missing in sorted(commands - handled):
    errors.append(f'Frontend message has no handler: {missing}')
if len(shell.splitlines()) > 300:
    errors.append('MainWindow exceeds shell size budget; review responsibilities before adding code.')
for source in (root / 'src/MCUGLauncher').rglob('*.cs'):
    if 'obj' in source.parts or 'bin' in source.parts or source.name == 'MainWindow.xaml.cs':
        continue
    if 'partial class MainWindow' in source.read_text(encoding='utf-8-sig'):
        errors.append(f'Business must not hide in a MainWindow partial: {source.name}')
if 'fetch(' in front:
    errors.append('Frontend transfers must use host commands and the Core network policy.')
if errors:
    print('\n'.join(errors), file=sys.stderr)
    sys.exit(1)
print(f'PASS: shell/use-case boundaries and {len(commands)} frontend command names.')
