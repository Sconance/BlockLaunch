import argparse
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parent.parent
LEGACY = Path('src/MCUGLauncher/MCUGLauncher.csproj')
FRONT = Path('src/MCUGLauncher/Front/Front.csproj')
APPLICATION = Path('src/MCUGLauncher.Application/MCUGLauncher.Application.csproj')
SHARED_UI = Path('src/MCUGLauncher.SharedUI/MCUGLauncher.SharedUI.csproj')
CORE = Path('src/MCUGLauncher.Core/MCUGLauncher.Core.csproj')


def references(project):
    document = ET.parse(project)
    return {(project.parent / item.attrib['Include']).resolve()
            for item in document.iter('ProjectReference')}


def application_sources():
    project = ROOT / APPLICATION
    sources = list(project.parent.glob('*.cs'))
    for item in ET.parse(project).iter('Compile'):
        include = item.attrib.get('Include')
        if include:
            sources.extend(project.parent.glob(include))
    return sources


def check():
    expected = {
        LEGACY: {APPLICATION, SHARED_UI},
        FRONT: {APPLICATION, SHARED_UI},
        SHARED_UI: {APPLICATION},
        APPLICATION: {CORE},
        CORE: set(),
    }
    for project, targets in expected.items():
        assert references(ROOT / project) == {(ROOT / target).resolve() for target in targets}, project
    for source in application_sources():
        text = source.read_text(encoding='utf-8')
        for forbidden in ('System.Windows', 'MainWindow', 'BlockLaunch.Front',
                          'BlockLaunch.SharedUI', 'InlineDialog', 'InlineFilePicker'):
            assert forbidden not in text, f'{source}: {forbidden}'
    for directory, forbidden in (
        (ROOT / 'src/MCUGLauncher/MainWindow', 'BlockLaunch.Front'),
        (ROOT / 'src/MCUGLauncher/Front', 'EmbeddedMicrosoftOAuthUI'),
        (ROOT / 'src/MCUGLauncher.SharedUI', 'BlockLaunch.Front'),
        (ROOT / 'src/MCUGLauncher.SharedUI', 'MainWindow'),
    ):
        for source in directory.rglob('*.cs'):
            if 'obj' not in source.parts and 'bin' not in source.parts:
                assert forbidden not in source.read_text(encoding='utf-8'), source
    assert not (ROOT / 'tools/FrontPreview/FrontPreview.csproj').exists()
    assert 'XDocument.Load' not in (ROOT / FRONT.parent / 'Program.cs').read_text(encoding='utf-8')
    print('PASS: frontend project graph, shared business and cross-frontend boundaries')


def build_isolated(dotnet):
    for target, project in (('Front', FRONT), ('Legacy', LEGACY)):
        with tempfile.TemporaryDirectory(prefix=f'MCUG-{target}-isolation-') as temporary:
            root = Path(temporary)
            shutil.copy2(ROOT / 'global.json', root / 'global.json')
            shutil.copytree(ROOT / 'src', root / 'src',
                            ignore=shutil.ignore_patterns('bin', 'obj'))
            launcher = root / 'src/MCUGLauncher'
            if target == 'Legacy':
                shutil.rmtree(launcher / 'Front')
            else:
                for directory in ('MainWindow', 'Controllers', 'Presentation', 'Assets'):
                    shutil.rmtree(launcher / directory)
                for name in ('MCUGLauncher.csproj', 'App.xaml', 'App.xaml.cs',
                             'MainWindow.xaml', 'InlineDialog.cs', 'InlineFilePicker.cs',
                             'Resources/Notices.txt'):
                    (launcher / name).unlink()
            subprocess.run([dotnet, 'build', str(root / project), '-c', 'Release',
                            '-p:EnableWindowsTargeting=true', '--nologo'], cwd=root, check=True)
            print(f'PASS: {target} builds without the other frontend', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Check frontend boundaries; optional builds use disposable copies only.')
    parser.add_argument('--build', action='store_true')
    parser.add_argument('--dotnet', default='dotnet')
    arguments = parser.parse_args()
    check()
    if arguments.build:
        build_isolated(arguments.dotnet)
