"""Replace generated summaries with detailed XML docs for the app layer (temporary helper)."""
import re
import sys
from pathlib import Path

DOCS = {
    'src/MCUGLauncher/Commands/ServerCommands.cs': {
        'class': ('服务器与目录命令。', '依赖 <see cref="ConfigStore"/> 与 <see cref="AppConfig"/> 保存收藏；'
                  '<see cref="VersionCatalog"/> 解析当前实例路径；<see cref="ServerPingService"/> 查询 SLP 状态；'
                  '<see cref="UiBridge"/> 回推结果；构造时注入 <c>SendState</c> 回调刷新界面。'),
        'OpenFolder': ('用资源管理器打开实例子目录。', '参数 <paramref name="kind"/> 取 mods/saves/screenshots/version，'
                       '其他值回退到游戏根目录；目录不存在时自动创建。'),
        'PingAsync': ('查询服务器 SLP 状态。', '地址为 host 或 host:port；id 原样回传；超时与连接失败写入 result.Error。'),
        'SaveServer': ('新增或更新一条收藏服务器。', 'address 必填；oldAddress 与 address 不同则先移除旧记录；'
                       '保存后回推 serverResult 并刷新状态。'),
        'DeleteServer': ('按地址删除收藏服务器。', '地址匹配忽略大小写；不存在的地址静默忽略。'),
    },
    'src/MCUGLauncher/Commands/ContentCommands.cs': {
        'class': ('内容与版本命令。', '依赖 <see cref="ConfigStore"/>、<see cref="AppConfig"/> 读写配置与实例，'
                  '<see cref="HttpClient"/> 访问 Mojang/Modrinth，<see cref="UiBridge"/> 回推，'
                  '<see cref="VersionCatalog"/>、<c>GameInstaller</c>、<c>ContentService</c> 完成安装与下载；'
                  '构造时注入 <c>SendState</c> 回调。网络与安装均异步执行，异常转换为 contentError/toast。'),
        'SendVanillaVersionsAsync': ('读取 Mojang 版本清单并返回最近的原版版本。', '只取 type=release 的前 24 条；'
                                     '失败回推 contentError。'),
        'InstallVanillaAsync': ('安装指定原版版本。', '调用 GameInstaller 下载并安装；成功后写入 LastVersion 并刷新状态。'),
        'SearchContentAsync': ('在 Modrinth 搜索模组/整合包/资源包/光影。', '结合当前实例的加载器与 Minecraft 版本做 facets 过滤；'
                               '请求超时 12 秒；结果回推 contentList。'),
        'InstallContentAsync': ('下载并安装 Modrinth 项目文件。', '按 category 解析目标目录（mods/resourcepacks/shaderpacks/datapacks），'
                                '整合包下载到数据目录 downloads；完成后刷新状态。'),
        'ToggleMod': ('启用或禁用本地模组。', '按文件名在当前实例 mods 中查找并调用 VersionCatalog.SetEnabled。'),
        'DeleteMod': ('删除本地模组文件。', '目标不存在时静默返回。'),
        'DeleteVersion': ('删除整个版本目录。', '拒绝空名称与包含非法文件名字符的版本。'),
        'FindMod': ('按文件名查找本地模组。', '返回匹配的 ModInfo，未找到返回 null。'),
        'InstallModAsync': ('按项目 id 安装 Mod 到实例或指定目录。', 'mode=instance 时安装到当前实例 mods，'
                            '否则使用 path 指定目录。'),
    },
    'src/MCUGLauncher/Commands/SettingsCommands.cs': {
        'class': ('设置与系统命令。', '依赖 <see cref="ConfigStore"/>、<see cref="AppConfig"/> 持久化设置，'
                  '<see cref="UiBridge"/> 回推，<see cref="HttpClient"/> 访问网络，<see cref="CommunityService"/> 测试数据源，'
                  '构造时传入 <see cref="System.Windows.Window"/> 作为对话框 owner 和 resetWindow 回调。'),
        'SaveSettings': ('保存设置并刷新界面。', '接受 root/player/java/ram/width/height/anonymous 等字段，'
                         '数值做范围限制后写入 config.json。'),
        'PickFolder': ('选择文件夹并回推 pickedPath。', '初始目录为当前游戏根目录。'),
        'PickFile': ('选择文件并回推 pickedPath。', 'target=java 时过滤 java.exe/javaw.exe。'),
        'ScanJavaAsync': ('扫描本机 Java 安装并回推 javaList。', '结果包含路径与版本，供设置页选择。'),
        'BackupSaves': ('把当前实例的 saves 压缩到数据目录 backups。', '文件名为 saves-时间戳.zip。'),
        'ReadLog': ('读取 launcher.log 末尾内容。', '最多返回最近 400 行，供诊断页展示。'),
        'SendSystemInfo': ('回推应用、.NET、WebView2、系统与路径信息。', 'WebView2 版本不可用时回退为“未知”。'),
        'ResetWindow': ('把窗口恢复到 1200×780 并提示。', '通过构造注入的 resetWindow 回调操作窗口。'),
        'TestSourceAsync': ('测试社区数据源连通性。', '请求 url 并返回 testResult（ok/message/耗时）。'),
        'VerifyKey': ('校验社区/管理密钥格式。', '长度不足 8 位直接失败；结果回推 keyResult。'),
        'SaveCommunityLocal': ('保存本地公告或推荐 JSON。', '写入数据目录下的 fileName，并回推保存结果。'),
        'PublishAsync': ('发布公告/推荐到远端或保存为待发布。', '未配置 WriteEndpoint 时仅保存本地并标记 pending。'),
    },
}


def replace_summary(lines, name, summary, remarks):
    for i, line in enumerate(lines):
        if name in line and ('class ' in line or re.match(r'\s*(public|internal|private|protected)', line)):
            # find preceding doc block start
            j = i - 1
            if j >= 0 and lines[j].strip() == '/// </summary>':
                k = j
                while k >= 0 and not lines[k].strip().startswith('/// <summary>'):
                    k -= 1
                if k >= 0:
                    indent = lines[k][:len(lines[k]) - len(lines[k].lstrip())]
                    block = [indent + '/// <summary>',
                             indent + '/// ' + summary,
                             indent + '/// </summary>']
                    if remarks:
                        block += [indent + '/// <remarks>',
                                  indent + '/// ' + remarks,
                                  indent + '/// </remarks>']
                    lines[k:j + 1] = block
                    return True
    return False


for filename, docs in DOCS.items():
    path = Path(filename)
    lines = path.read_text(encoding='utf-8').splitlines()
    class_line = next(line for line in lines if 'class ' in line and 'sealed class' in line)
    class_name = re.search(r'class (\w+)', class_line).group(1)
    if 'class' in docs:
        summary, remarks = docs['class']
        replace_summary(lines, 'sealed class ' + class_name, summary, remarks)
    for member, (summary, remarks) in docs.items():
        if member == 'class' or member == class_name:
            continue
        print(member, replace_summary(lines, member + '(', summary, remarks))
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('updated', path)
