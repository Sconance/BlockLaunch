"""Add <param>/<returns> details for key command members (temporary helper)."""
import re
from pathlib import Path

DETAILS = {
    'src/MCUGLauncher/Commands/ServerCommands.cs': {
        'OpenFolder': ([('kind', 'mods / saves / screenshots / version，其他值打开游戏根目录')], None),
        'PingAsync': ([('address', '服务器域名或 IP，可带端口'), ('id', '前端请求标识，原样回传')], None),
        'SaveServer': ([('root', '包含 name、address、oldAddress 的消息')], None),
        'DeleteServer': ([('address', '要删除的服务器地址，忽略大小写')], None),
    },
    'src/MCUGLauncher/Commands/ContentCommands.cs': {
        'SendVanillaVersionsAsync': ([], None),
        'InstallVanillaAsync': ([('version', '要安装的原版版本号')], None),
        'SearchContentAsync': ([('root', '包含 category 与 query 的消息')], None),
        'InstallContentAsync': ([('root', '包含 id 与 category 的消息')], None),
        'ToggleMod': ([('file', '模组文件名'), ('enabled', 'true 启用，false 禁用')], None),
        'DeleteMod': ([('file', '要删除的模组文件名')], None),
        'DeleteVersion': ([('version', '要删除的版本目录名')], None),
        'FindMod': ([('file', '模组文件名')], '匹配的模组信息；未找到时为 null'),
        'InstallModAsync': ([('root', '包含 id、mode、path 的消息')], None),
    },
    'src/MCUGLauncher/Commands/SettingsCommands.cs': {
        'SaveSettings': ([('root', '前端提交的设置字段集合')], None),
        'PickFolder': ([('target', '回传目标字段名，如 root')], None),
        'PickFile': ([('target', '回传目标字段名，如 java')], None),
        'ScanJavaAsync': ([], None),
        'BackupSaves': ([], None),
        'ReadLog': ([], None),
        'SendSystemInfo': ([], None),
        'ResetWindow': ([], None),
        'TestSourceAsync': ([('name', '数据源名称'), ('url', '要测试的地址')], None),
        'VerifyKey': ([('role', '密钥角色，如 community/admin'), ('key', '待校验的密钥')], None),
        'SaveCommunityLocal': ([('fileName', '目标 JSON 文件名'), ('root', '消息体'), ('property', '读取的字段名')], None),
        'PublishAsync': ([('root', '包含公告/推荐内容的发布消息')], None),
    },
}


def add(lines, method, params, returns):
    for index, line in enumerate(lines):
        if method + '(' not in line or not re.match(r'\s*(public|internal|private|protected)', line):
            continue
        # find the closing summary line above
        j = index - 1
        while j >= 0 and not lines[j].strip() == '/// </summary>':
            j -= 1
        if j < 0:
            return
        indent = lines[j][:len(lines[j]) - len(lines[j].lstrip())]
        block = []
        for name, text in params:
            block.append(f'{indent}/// <param name="{name}">{text}</param>')
        if returns:
            block.append(f'{indent}/// <returns>{returns}</returns>')
        if block:
            lines[j + 1:j + 1] = block
        return


for filename, members in DETAILS.items():
    path = Path(filename)
    lines = path.read_text(encoding='utf-8').splitlines()
    for method, (params, returns) in members.items():
        add(lines, method, params, returns)
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('updated', path)
