"""Keep the native window a shell and page markup in standalone views."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


def main():
    root = Path(__file__).resolve().parent.parent
    app = root / 'src/MCUGLauncher'
    errors = []
    shell = app / 'MainWindow.xaml.cs'
    for source in app.rglob('*.cs'):
        if any(part in ('bin', 'obj') for part in source.parts):
            continue
        text = source.read_text(encoding='utf-8-sig')
        if source != shell and re.search(r'\bclass\s+MainWindow\b', text):
            errors.append(f'{source.relative_to(root)} reintroduces MainWindow implementation')
    text = shell.read_text(encoding='utf-8-sig')
    if len(text.splitlines()) > 150:
        errors.append('MainWindow code-behind exceeds the shell boundary; move behavior to its owning module')
    for forbidden in ('new HttpClient', 'new MinecraftLauncher', 'Process.Start', 'JsonSerializer.'):
        if forbidden in text:
            errors.append(f'MainWindow contains application behavior: {forbidden}')
    # These adapters have explicit dependencies. Keep the composition root out of them.
    isolated = ('AvatarController', 'HomeAccountController', 'KeyboardNavigationController',
                'LoaderOverviewController', 'NetworkController', 'WindowChromeController',
                'GameWindowBehaviorController', 'InstanceSearchController', 'VersionIdentityEditor', 'ContentUpdatesController',
                'ResourceFavoritesController', 'ResourceSearchLayoutController',
                'RunningGamesController', 'VersionCatalogController', 'SettingsNavigationController')
    components = {path.stem: path for path in (app / 'Components').rglob('*.cs')}
    for name in isolated:
        source = components.get(name)
        if source is None:
            errors.append(f'Missing component module: {name}')
            continue
        if re.search(r'\bLauncherWorkspace\b', source.read_text(encoding='utf-8-sig')):
            errors.append(f'{name} must receive its controls, state or callbacks explicitly')
    settings_controller = (app / 'Components/Shell/SettingsNavigationController.cs').read_text(encoding='utf-8-sig')
    if re.search(r'System\.Windows|\b(UIElement|FrameworkElement|StackPanel|ScrollViewer|InlineDialog)\b', settings_controller):
        errors.append('SettingsNavigationController must select plain categories through its presentation port')
    session_controller = (app / 'Components/Launch/RunningGamesController.cs').read_text(encoding='utf-8-sig')
    if re.search(r'System\.Windows|\b(Button|UIElement|StackPanel|TextBox|ScrollViewer|FeatureUiElements|SharedLogs|Dispatcher)\b', session_controller):
        errors.append('RunningGamesController must use the presentation port, not WPF or file readers')
    session_view = (app / 'Views/RunningGamesView.cs').read_text(encoding='utf-8-sig')
    if re.search(r'\b(RunningGameRegistry|GameSession|SharedLogs|Process|Diagnostics)\b', session_view):
        errors.append('RunningGamesView must render value rows and raise commands, not control processes or read files')
    for folder in ('Builders', 'Models', 'Views/Forms', 'Views/Shared'):
        for source in (app / folder).glob('*.cs'):
            if re.search(r'\bLauncherWorkspace\b', source.read_text(encoding='utf-8-sig')):
                errors.append(f'{source.relative_to(root)} depends on the full workspace')
    for name in ('SketchContentManagerDialog', 'LaunchOptionsDialog', 'InstancePackExportDialog', 'FeatureDialogBuilder'):
        source = app / 'Presentation' / f'{name}.cs'
        if re.search(r'\b(LauncherWorkspace|MainWindow)\b', source.read_text(encoding='utf-8-sig')):
            errors.append(f'{name} must receive scoped dependencies instead of a complete window/workspace')
    query = (app / 'Presentation/ResourceFavoriteSearch.cs').read_text(encoding='utf-8-sig')
    if re.search(r'\b(LauncherWorkspace|MainWindow|TextBox|ComboBox|ListBox)\b|System\.Windows', query):
        errors.append('Resource favorite search must consume a value snapshot, not WPF or workspace state')
    version_pages = app / 'Views/VersionSettings'
    for name in ('VersionOverviewPage', 'VersionLaunchPage', 'VersionLoaderPage', 'VersionModPage', 'VersionExportPage'):
        source = version_pages / f'{name}.cs'
        if not source.is_file():
            errors.append(f'Missing independent version page: {name}')
        elif re.search(r'\b(LauncherWorkspace|MainWindow)\b', source.read_text(encoding='utf-8-sig')):
            errors.append(f'{name} must use per-instance context and explicit actions')
    if len((app / 'Components/Instances/VersionSettingsController.cs').read_text(encoding='utf-8-sig').splitlines()) > 200:
        errors.append('Version settings coordinator exceeds its lifetime/navigation boundary')
    presentation = '{http://schemas.microsoft.com/winfx/2006/xaml/presentation}'
    document = ET.parse(app / 'MainWindow.xaml')
    for tab in document.iter(presentation + 'TabItem'):
        children = list(tab)
        if len(children) != 1 or not children[0].tag.startswith('{clr-namespace:BlockLaunch}'):
            errors.append('A main tab embeds page layout instead of a standalone view')
    pages = list((app / 'Views/Pages').glob('*.xaml'))
    for page in pages:
        view = ET.parse(page).getroot()
        if view.tag != '{clr-namespace:BlockLaunch}LauncherPage':
            errors.append(f'{page.relative_to(root)} must use the page action boundary')
        if not page.with_suffix('.xaml.cs').is_file():
            errors.append(f'{page.relative_to(root)} has no code-behind')
    project = ET.parse(app / 'MCUGLauncher.csproj')
    if any('TestSupport' in str(item.attrib) or '/test/' in str(item.attrib) for item in project.iter('Compile')):
        errors.append('Test-only module inspection must not ship in the launcher')
    if errors:
        print('\n'.join(errors), file=sys.stderr)
        return 1
    print(f'PASS: native window stays a shell; {len(pages)} independent markup views; no MainWindow partial modules.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
