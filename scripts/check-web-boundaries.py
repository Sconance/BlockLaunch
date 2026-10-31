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
front = (root / 'src/MCUGLauncher/Ui/host.js').read_text(encoding='utf-8-sig')
router = (root / 'src/MCUGLauncher/UseCases/LauncherApplication.cs').read_text(encoding='utf-8-sig')
commands = set(re.findall(r"post\(\{\s*type:\s*'([^']+)'", front))
handled = set(re.findall(r'case "([^"]+)":', router))
for missing in sorted(commands - handled):
    errors.append(f'Frontend message has no handler: {missing}')
if errors:
    print('\n'.join(errors), file=sys.stderr)
    sys.exit(1)
print(f'PASS: shell/use-case boundaries and {len(commands)} frontend command names.')
