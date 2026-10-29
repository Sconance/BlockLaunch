# Front 界面与共享业务验收

## 范围与安全

本说明适用于本次 Front 界面重构提交。先记录 `git status --short`、分支和提交；不要重置工作区。只使用测试程序创建的临时目录，或自行创建的专用测试配置与游戏目录。默认 Front 会读取正式配置并恢复排队下载，未经授权不要直接运行默认入口。

**最新交接状态：用户要求跳过 Linux 本地测试，直接在 Windows 构建和测试。最新实例克隆、启动脚本导出、参数解析和启动确认增量未编译、未运行；此前构建成功记录只属于较早代码。若构建失败，先修复并记录失败位置，不标记验收通过。**

开发环境是 Linux，以下原生 WPF 测试未执行；交叉编译通过不是点击或视觉验收通过。测试结果追加到本文件末尾，所有当前必测项完成后才删除根目录 `AGENT.md`。

## 1. 构建和自动化断言

在 Windows x64 安装 `global.json` 指定 SDK，从仓库根目录打开 PowerShell：

```powershell
.\scripts\build-windows.ps1 -Frontend Both
dotnet run --project test/BuilderChecks/BuilderChecks.csproj -c Release
dotnet run --project test/FrontRuntimeChecks/FrontRuntimeChecks.csproj -c Release
dotnet run --project test/FrontRuntimeChecks/FrontRuntimeChecks.csproj -c Release -- --ui-structure
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --business-services
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --management
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front-settings
python scripts/check-frontend-boundaries.py --build
```

逐条确认退出码，不要只看最后一条命令。`FrontRuntimeChecks` 普通模式预期包含 `ALL_FRONT_RUNTIME_CHECKS_PASSED`；UI 模式预期包含 `ALL_FRONT_UI_STRUCTURE_CHECKS_PASSED`。其他模式未必有专用结束标记，检查退出码和逐项 PASS。

UI 结构模式在 STA 主线程加载 Front 资源，不显示主窗口，不查询互联网，不输入账号，不启动游戏。通过注入空资源服务，重复切换下载、Mod、设置、启动、本地整合包，检查控件父级释放及重复导航。它不是 Windows 鼠标或截图验收。

## 2. 以独立配置打开界面

```powershell
$testDir = Join-Path $env:TEMP ('MCUG-ui-' + [guid]::NewGuid().ToString('N'))
$gameDir = Join-Path $testDir 'minecraft'
New-Item -ItemType Directory -Path $testDir, $gameDir | Out-Null
@{ SchemaVersion = 2; Root = $gameDir; Player = 'Fixture'; Ram = 4096; Width = 1280; Height = 720; DownloadsPaused = $true } |
  ConvertTo-Json | Set-Content (Join-Path $testDir 'settings.json') -Encoding utf8
& '.\artifacts\windows\front\Release\build\MCUGLauncher.Front.exe' --settings-dir $testDir
```

打开资源页会发起真实 Modrinth 查询，刷新版本会查询在线版本清单。无网络时记录错误提示，不把空列表当功能通过。新旧启动器共用同一测试配置时必须先退出另一端；同时打开应明确报告目录已占用。

## 3. 视觉与操作清单

| 检查面 | 操作与预期 | 状态 |
|---|---|---|
| 主窗口 | 对照旧 UI 截图；1060×740、最小 940×650；48px 标题栏；拖动、双击、最大化、还原、缩放、关闭 | 未测 |
| 主页 | 左侧账号卡在启动卡上方；右侧版本区默认隐藏；点击 `>` 展开/收回；切换账号模式不丢失页面 | 未测 |
| 导航 | 下载→原版→Mod→设置→启动→下载→本地整合包→更多/内容中心，重复 3 次，无重复父级异常 | 未测 |
| 下载页 | 8 类按钮可见且高亮正确；原版/加载器显示版本表单；本地整合包只显示导入内容 | 未测 |
| 资源页 | 搜索/Enter/分页；项目进入详情；介绍/版本切换；返回保留结果；取消/断网有状态提示 | 未测 |
| 设置 | 目录与 Java 文件选择；非法内存拒绝；高级启动设置保存；外观重启恢复；代理重启生效 | 未测 |
| 外观 | 四种主题色与深浅主题；背景路径清空、错误图片、大图片；100%/150%/200% DPI 不截断关键按钮 | 未测 |
| 版本管理 | 仅用获授权的测试实例；概览、设置、修改、Mod 管理、导出左侧导航；保存后重新打开核对 | 未测 |
| 版本资源 | 测试包导入/切换状态/打开目录；存档只列含 level.dat 的世界；导出不包含凭据 | 未测 |
| 日志 | 实时输出、自动滚动、复制；选中实例后读取 latest.log；缺失日志有提示，大日志仅末尾读取 | 未测 |
| 运行状态 | 获授权后验证游戏运行时停止按钮、启动禁用、保存门禁；关闭等待不强杀游戏 | 未测 |

视觉与原生运行项每项记录 Windows 版本、DPI、截图路径和复现步骤；不具备真实实例或授权时标记未测。

## 4. 当前明确差异

参见 `FRONT_UI_PARITY.md`。旧 UI 的卸载、更新与事务回退、整合包升级、Java 管理器、在线头像与备份恢复尚未全部迁移。克隆与启动脚本已增加代码入口，但未经编译和运行验收。不要删除旧 UI，不要把未显示入口当成已通过。

## 5. 克隆与脚本增量（未编译、未测）

1. 仅使用获授权的测试实例，进入版本设置→修改→复制为独立实例。默认不勾选存档、截图和服务器列表。核对新实例可在关闭设置窗口后刷新并选中，模组/配置复制、数据目录独立、原实例不变。分别勾选可选内容并检查对应文件；空名称、过长名称、源实例不完整、游戏运行和下载占用时必须拒绝或显示错误，不留下半成品。
2. 进入版本设置→导出→导出启动脚本，分别保存 `.cmd` 与 `.ps1`。脚本应指向 `MCUGLauncher.Front.exe` 并包含 `--root`、`--instance`、`--account`、`--settings-dir`；不含密码或账号令牌。非 `.cmd/.ps1` 目标应被拒绝。测试含空格、中文、单引号、百分号的路径；PowerShell 脚本执行策略按设备规则处理，不修改全局策略。
3. 退出 Front 后运行导出的脚本。必须先显示 Front 启动确认；取消时不得启动游戏或改变账号选择。确认后才进行账号选择、授权和启动预检。使用未安装实例、过期游戏目录或重复/缺失参数时应明确拒绝，不自动覆盖当前游戏目录。真实授权和游戏启动需要设备所有者许可。
4. 检查旧 UI 的实例克隆和脚本导出回归。两个入口共用 `Versions/VersionManagementService.Clone` 与 `Builders/LaunchScriptExporter`；旧脚本仍使用原有旧入口 CLI 行为，不应被改为调用 Front。脚本路径校验和字符转义由共享 Builders 模块处理。

当前 `BuilderChecks` 不包含全部新增参数解析/克隆断言；Windows agent 可补充针对性自动化用例，不能只凭旧测试通过宣称增量已验收。记录每项操作与证据，发现实现错误先修复再重测。

## 验收记录

待 Windows 设备填写：分支/提交/工作区、SDK 与系统版本、每条命令退出码、清单结果、问题修复、截图与日志位置。
