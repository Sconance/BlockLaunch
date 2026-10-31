"""Insert XML doc comments where missing (temporary helper)."""
import re
import sys
from pathlib import Path

TYPE_RE = re.compile(
    r'^(?P<indent>[ \t]*)(?P<mods>(?:public|internal)\s+(?:(?:sealed|static|abstract|partial|readonly)\s+)*)'
    r'(?P<kind>class|record|enum|interface|struct)\s+(?P<name>\w+)')
MEMBER_RE = re.compile(
    r'^(?P<indent>[ \t]{4,8})(?P<mods>(?:public|internal|private|protected)\s+'
    r'(?:(?:static|async|override|sealed|readonly|partial|virtual|new)\s+)*)'
    r'(?P<type>[\w<>\[\],\.\? ]+?)\s+(?P<name>\w+)\s*(?P<rest>\(|=>|\{)')

VERBS = [
    ('Get', '获取'), ('Set', '设置'), ('Load', '加载'), ('Save', '保存'),
    ('Build', '构建'), ('Send', '发送'), ('Install', '安装'), ('Search', '搜索'),
    ('Delete', '删除'), ('Remove', '移除'), ('Toggle', '切换'), ('Pick', '选择'),
    ('Scan', '扫描'), ('Backup', '备份'), ('Read', '读取'), ('Write', '写入'),
    ('Start', '启动'), ('Stop', '停止'), ('Ping', '查询'), ('Check', '检查'),
    ('Create', '创建'), ('Open', '打开'), ('Close', '关闭'), ('Reset', '重置'),
    ('Update', '更新'), ('Parse', '解析'), ('Format', '格式化'), ('Find', '查找'),
    ('Ensure', '确保'), ('Try', '尝试'), ('Apply', '应用'), ('Restore', '恢复'),
    ('Import', '导入'), ('Export', '导出'), ('Prepare', '准备'), ('Register', '注册'),
    ('Dispatch', '分发'), ('Extract', '解压'), ('Handle', '处理'), ('Initialize', '初始化'),
    ('Login', '登录'), ('Logout', '退出'), ('Publish', '发布'), ('Verify', '校验'),
    ('Test', '测试'), ('Clone', '克隆'), ('Copy', '复制'), ('Move', '移动'),
    ('Detect', '检测'), ('Resolve', '解析'), ('Collect', '收集'), ('Normalize', '规范化'),
    ('Is', '判断是否为'), ('Can', '判断能否'), ('Has', '判断是否包含'), ('Should', '判断是否'),
]

TOKENS = {
    'Folder': '文件夹', 'File': '文件', 'Server': '服务器', 'Settings': '设置',
    'Java': 'Java', 'Version': '版本', 'Mod': '模组', 'Content': '内容',
    'Log': '日志', 'SystemInfo': '系统信息', 'Window': '窗口', 'State': '状态',
    'Community': '社区', 'Account': '账号', 'Launch': '启动', 'Path': '路径',
    'Root': '根目录', 'Data': '数据', 'Config': '配置', 'Instance': '实例',
    'Download': '下载', 'Update': '更新', 'Backup': '备份', 'Profile': '档案',
    'Session': '会话', 'Token': '令牌', 'Password': '密码', 'Credential': '凭据',
    'Skin': '皮肤', 'Avatar': '头像', 'Queue': '队列', 'Task': '任务',
    'Tasks': '任务', 'Items': '项目', 'Item': '项目', 'List': '列表',
    'Group': '分组', 'Groups': '分组', 'Tag': '标签', 'Tags': '标签',
    'Name': '名称', 'Address': '地址', 'Id': 'ID', 'Type': '类型',
    'Info': '信息', 'Result': '结果', 'Error': '错误', 'Message': '消息',
    'Theme': '主题', 'Page': '页面', 'Menu': '菜单', 'Dialog': '对话框',
    'Owner': '所有者', 'Event': '事件', 'Handler': '处理器', 'Command': '命令',
    'Commands': '命令', 'Bridge': '桥接', 'Router': '路由', 'Cache': '缓存',
    'Builder': '构建器', 'Coordinator': '协调器', 'Feed': '数据源',
    'Status': '状态', 'Ping': '服务器状态', 'Info': '信息',
}


def split_pascal(text):
    return re.findall(r'[A-Z][a-z0-9]*|[A-Z]+(?![a-z])', text)


def humanize(text):
    words = split_pascal(text)
    return ''.join(TOKENS.get(word, word) for word in words)


def summary_for_type(kind, name):
    if kind == 'record':
        return f'表示 {humanize(name)} 数据。'
    if kind == 'enum':
        return f'定义 {humanize(name)} 的取值。'
    if kind == 'interface':
        return f'定义 {humanize(name)} 的契约。'
    return f'提供 {humanize(name)} 相关功能。'


def summary_for_member(name):
    name = re.sub(r'Async$', '', name)
    for prefix, verb in VERBS:
        if name.startswith(prefix):
            rest = name[len(prefix):]
            return f'{verb}{humanize(rest)}。' if rest else f'{verb}。'
    return f'执行 {humanize(name)}。'


def has_doc(lines, index):
    i = index - 1
    while i >= 0 and lines[i].strip() == '':
        i -= 1
    if i >= 0 and lines[i].strip().startswith('///'):
        return True
    if i >= 0 and lines[i].strip().startswith('['):
        j = i - 1
        while j >= 0 and lines[j].strip().startswith('['):
            j -= 1
        return j >= 0 and lines[j].strip().startswith('///')
    return False


def process(path):
    lines = path.read_text(encoding='utf-8').splitlines()
    output = []
    changed = 0
    for index, line in enumerate(lines):
        stripped = line.strip()
        doc = None
        if not stripped.startswith(('///', '//')) and not has_doc(lines, index):
            match = TYPE_RE.match(line)
            if match:
                doc = summary_for_type(match.group('kind'), match.group('name'))
            else:
                member = MEMBER_RE.match(line)
                if member and not stripped.startswith(('return', 'throw', 'if', 'for', 'while', 'switch', 'using', 'var ')):
                    name = member.group('name')
                    if member.group('rest') == '(':
                        doc = summary_for_member(name)
                    elif member.group('rest') == '{':
                        doc = f'获取或设置 {humanize(name)}。'
        if doc:
            pad = ' ' * (len(line) - len(line.lstrip()))
            output.append(pad + '/// <summary>')
            output.append(pad + f'/// {doc}')
            output.append(pad + '/// </summary>')
            changed += 1
        output.append(line)
    if changed:
        path.write_text('\n'.join(output) + '\n', encoding='utf-8')
    return changed


total = 0
for arg in sys.argv[1:]:
    base = Path(arg)
    files = [base] if base.is_file() else sorted(base.rglob('*.cs'))
    for file in files:
        if any(part in ('obj', 'bin') for part in file.parts):
            continue
        total += process(file)
print('inserted', total, 'doc blocks')
