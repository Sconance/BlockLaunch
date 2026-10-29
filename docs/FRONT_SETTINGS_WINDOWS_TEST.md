# Front 设置页：Windows agent 编译与验收指南

> 历史指南：当前 Front 已切换为真实模块宿主。旧 `--live` / `--simulate-game-running` 和临时预览规则已移除；当前验收以 [FRONT_RUNTIME.md](dcg/FRONT_RUNTIME.md) 为准，不要按本文旧命令操作正式配置。

## 0. 本轮范围与通过条件

本轮只实现“设置页面 → 接口 → 适配器 → 设置业务 → SettingsStore”的独立闭环，不迁移账号、启动或下载页面。
界面可编辑五项：游戏目录、Java 路径、内存 MB、窗口宽度和窗口高度。
账号偏好、玩家名、所选版本、队列暂停、外观、隔离模式、自动内存开关及其他高级启动参数保持原样。
内存输入保存到 `Ram` 与 `Launch.RamMb`；若 `Launch.AutoMemory` 为 true，实际启动仍沿用原自动内存规则，本页不修改它。

所有以下项目均须有实际结果：主程序/预览/检查程序编译、设置隔离自动化检查、相关回归检查、设置 GUI 正常与异常流程、业务门禁与目录隔离。
自动化通过但 GUI 未操作时只能标记“部分完成”。不要删除旧 UI，不要切换正式入口。

预计已装好 SDK 后约 15–25 分钟完成自动化和单一 DPI 的手工流程；首次包还原和多 DPI 验收另计，取决于网络和桌面环境。

### 源码入口

| 文件 | 职责 |
| --- | --- |
| `src/MCUGLauncher/Contracts/IFrontSettings.cs` | 仅含五个可编辑字段的不可变快照和异步读写接口，不暴露 Preferences |
| `src/MCUGLauncher/Adapters/FrontSettingsAdapter.cs` | 串行读写；保存前加载最新偏好，保留界面未编辑的其他字段 |
| `src/MCUGLauncher/Settings/LauncherSettingsService.cs` | 运行门禁、校验、规范化、持久化成功后更新内存 |
| `src/MCUGLauncher/Front/ViewModels/SettingsViewModel.cs` | 加载/保存状态、防重入、失败重试、关闭后的过期结果保护 |
| `src/MCUGLauncher/Front/Views/SettingsView.xaml` | 设置页 XAML；对应 code-behind 只转发界面事件 |
| `tools/FrontPreview/Program.cs` | 独立 Application、隔离配置目录、互斥锁、模拟运行门禁 |
| `test/CompatibilityChecks/FrontSettingsChecks.cs` | 不创建 MainWindow 的后端及 ViewModel 自动化检查 |

## 1. 核对环境与源码

在 Windows x64 的可交互桌面打开 PowerShell，进入仓库根目录。确认网络可还原 NuGet、安装 .NET SDK 8.0.426 或同 feature band 更高补丁。
只有运行库、只有 .NET 9/10 SDK，不等于满足 `global.json`。本轮设置与模拟资源测试不需要 Java、账号或 WebView2 登录。

```powershell
$ErrorActionPreference = 'Stop'
git branch --show-current
git rev-parse HEAD
git status --short
dotnet --info
dotnet --list-sdks
if (!(Test-Path 'src/MCUGLauncher/Front/Views/SettingsView.xaml')) { throw '当前源码缺少设置页，不是本轮版本' }
if (!(Test-Path 'test/CompatibilityChecks/FrontSettingsChecks.cs')) { throw '当前源码缺少本轮测试' }
Select-String -Path 'test/CompatibilityChecks/Program.cs' -Pattern '\-\-front-settings'
```

预期分支 `refactor-dcg`。本轮改动可能以未提交补丁交接，必须同时记录工作区状态；只检查分支名不足以确认拿到新源码。
如果关键文件缺失，停止验收并请求同步源码；不要自动 reset、clean、切分支或用 pull 覆盖用户文件。

建立独立目录并启动日志记录。后续代码块在同一个 PowerShell 会话执行：

```powershell
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$evidence = Join-Path $env:TEMP "MCUG-FrontSettings-Evidence-$stamp"
$testDir = Join-Path $env:TEMP "MCUG Front Settings 中文 $stamp"
New-Item -ItemType Directory -Path $evidence | Out-Null
Start-Transcript -Path (Join-Path $evidence 'windows-test.txt')
git branch --show-current
git rev-parse HEAD
git status --short
dotnet --info
Write-Host "证据目录：$evidence"
Write-Host "配置目录：$testDir"
function Invoke-Dotnet {
    param([string[]]$Arguments)
    Write-Host ('COMMAND: dotnet ' + ($Arguments -join ' '))
    & dotnet @Arguments
    $exitCode = $LASTEXITCODE
    Write-Host "EXIT CODE: $exitCode"
    if ($exitCode -ne 0) { throw "dotnet 失败，退出码 $exitCode；记录首个错误，先不要继续后面的阶段。" }
}
```

## 2. 编译与自动化检查

从仓库根目录依次执行；`dotnet build` 会自动还原，不需要运行 publish：

```powershell
Invoke-Dotnet -Arguments @('build', 'src/MCUGLauncher/MCUGLauncher.csproj', '-c', 'Release')
Invoke-Dotnet -Arguments @('build', 'tools/FrontPreview/FrontPreview.csproj', '-c', 'Release')
Invoke-Dotnet -Arguments @('build', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release')
Invoke-Dotnet -Arguments @('run', '--project', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release', '--no-build', '--', '--front-settings')
```

通过标准：三次构建退出码 0；设置检查退出码 0 且出现 `ALL_FRONT_SETTINGS_CHECKS_PASSED`。警告也须记录，不要假定必须零警告或掩盖新警告。
这些是自定义控制台检查，不要换成 `dotnet test` 后将“未发现测试”当作通过。
正常构建输出分别位于 `src/MCUGLauncher/bin/Release/net8.0-windows/win-x64/`、`tools/FrontPreview/bin/Release/net8.0-windows/win-x64/` 和 `test/CompatibilityChecks/bin/Release/net8.0-windows/`。
预览必须保留输出目录中的 `Fixtures/LauncherApp.xaml` 和依赖文件；不要只把一个 FrontPreview.exe 复制到其他目录运行。优先使用本文的 `dotnet run` 命令。

设置检查覆盖：

| 范围 | 必须验证的行为 |
| --- | --- |
| 读取、保存与重开 | 无 MainWindow 即可调用真实服务；目录规范化；新适配器能读回同一快照 |
| 数据边界 | 内存 512–131072、宽度 320–16384、高度 240–16384 的闭区间；非法数字/不存在 Java/空目录/把文件当目录均拒绝，原文件不变 |
| 偏好保留 | 玩家、账号模式、版本、队列、外观和高级启动参数不被五项输入覆盖；旧快照保存仍保留磁盘上较新的其他偏好 |
| 错误与门禁 | 游戏运行时拒绝保存；调用前取消不读写；高版本配置拒绝覆盖；持久化失败不提前更新内存 |
| ViewModel | 加载前禁止保存；保存错误保留编辑并可重试；重新读取丢弃编辑；读取错误可重试；忙时禁止重入；关闭取消并忽略晚到响应 |

继续运行相关回归，全部记录命令、退出码和 PASS/失败输出：

```powershell
Invoke-Dotnet -Arguments @('run', '--project', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release', '--no-build', '--', '--business-services')
Invoke-Dotnet -Arguments @('run', '--project', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release', '--no-build', '--', '--launch-controller')
Invoke-Dotnet -Arguments @('run', '--project', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release', '--no-build', '--', '--front')
Invoke-Dotnet -Arguments @('run', '--project', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release', '--no-build', '--', '--management')
Invoke-Dotnet -Arguments @('run', '--project', 'test/CompatibilityChecks/CompatibilityChecks.csproj', '-c', 'Release', '--no-build', '--', '--content-update-controller')
Invoke-Dotnet -Arguments @('run', '--project', 'test/QueueChecks/QueueChecks.csproj', '-c', 'Release', '--', '--queue-service')
```

这些指定检查使用临时数据/fixture，不需要真实账号或游戏。不要擅自运行 `IntegrationChecks` 或带 `--live` 的账号检查。
如果机器安装了 Python，可额外运行 `python scripts/check-layout.py`，但该静态检查不能替代以上编译与断言。

## 3. 设置页真实 GUI 验收

```powershell
Invoke-Dotnet -Arguments @('run', '--project', 'tools/FrontPreview/FrontPreview.csproj', '-c', 'Release', '--no-build', '--', '--settings-dir', $testDir)
```

命令会一直等待窗口关闭。验证界面时需要第二个 PowerShell 查看文件；第二个窗口中把 `$testDir` 设为第一个窗口打印的完整路径。
首次启动会在测试目录创建 `.front-preview`、`settings.json` 和 `download-queue.lock`。默认游戏目录为该测试目录下的 `minecraft-preview`。
省略 `--settings-dir` 时默认目录是 `%TEMP%\MCUGLauncher-FrontPreview`，会保留上次预览数据；推荐使用上面带时间戳的独立目录。

| 状态 | 操作 | 通过标准 / 证据 |
| --- | --- | --- |
| 未测 | 点击“启动设置” | 出现五个输入框和保存/重新读取按钮；不再显示未迁移占位；完成读取后允许编辑 |
| 未测 | 游戏目录输入测试目录下新的中文空格子目录；Java 留空；内存 8192、宽 1600、高 900；保存 | 显示已保存；`settings.json` 的 Root/Ram/Width/Height 和 Launch 对应字段一致；不下载、不启动游戏 |
| 未测 | 查看测试目录文件 | 只发生测试配置读写；未创建真实游戏安装；保存路径与窗口左下角打印目录一致 |
| 未测 | 切到内容中心再回设置 | 已编辑/已保存字段仍在；不创建旧 MainWindow；无重复加载覆盖用户编辑 |
| 未测 | 修改内存但不保存，点击“重新读取” | 未保存修改被丢弃，恢复磁盘内容 |
| 未测 | 内存分别输入 abc、511、131073；宽度 319/16385；高度 239/16385；逐次保存 | 页面显示明确错误、能继续编辑和重试，保存前后 settings.json 哈希一致 |
| 未测 | 游戏目录留空或填写 settings.json 文件路径；Java 填不存在的路径 | 保存被拒绝，原文件哈希不变，不崩溃、不永久加载 |
| 未测 | Java 指向实际存在的 java.exe（可选）；无 Java 时使用下方假文件，只验证路径 | 含空格/引号路径去引号后保存；留空也能保存；假文件绝不能执行 |
| 未测 | 修正为合法输入，快速连续点击保存或重新读取 | 忙时按钮/输入禁用；无重入、无旧请求覆盖，结束后恢复可用 |
| 未测 | 关闭后用同一条预览命令重开 | 设置页读回已保存值；未保存的编辑不保留 |
| 未测 | 用原生 Tab/Shift+Tab 输入并点击按钮；100%/150%/200% 缩放、最小窗口 | 字段标签、状态、按钮可读可达，无裁切重叠；保存错误和成功各留截图 |
| 未测 | 在读取/保存期间关闭窗口 | 正常退出，无未处理异常；重开后配置是完整旧值或完整新值，不是半份 JSON |

注意：取消/关闭不是事务回滚；已经开始的原子写入可能完成。不要断言“关闭窗口后一定未保存”。
UI 操作权限不足就把相应项保留“未测”，注明工具限制；截图不等于已验证真实点击。

第二个 PowerShell 可检查内容与哈希（此处 `$testDir` 必须使用第一窗口的实际路径）：

```powershell
Get-Content -Raw (Join-Path $testDir 'settings.json') | ConvertFrom-Json | Format-List Root,Java,Ram,Width,Height
(Get-Content -Raw (Join-Path $testDir 'settings.json') | ConvertFrom-Json).Launch | Format-List RamMb,Java,Width,Height,AutoMemory,Isolation
Get-FileHash (Join-Path $testDir 'settings.json') -Algorithm SHA256
```

没有 Java 时，可在第二窗口创建 `Set-Content -Path (Join-Path $testDir 'fake java.exe') -Value 'fixture only; never execute'`。
在页面填写该文件完整路径只验证存在性，不证明它能启动游戏。完成后将 Java 恢复留空。

## 4. 门禁、目录隔离与页面回归

### 4.1 游戏运行门禁（必测，不启动真实游戏）

关闭普通预览，记录 `settings.json` 的哈希，使用同一目录运行：

```powershell
Invoke-Dotnet -Arguments @('run', '--project', 'tools/FrontPreview/FrontPreview.csproj', '-c', 'Release', '--no-build', '--', '--settings-dir', $testDir, '--simulate-game-running')
```

窗口左下角须标明模拟门禁。设置仍可读取；改值保存时提示“请先关闭游戏再更改设置”，磁盘哈希不变。
关闭窗口，去掉该参数重开，保存应恢复。这个开关验证的是注入的业务规则，不代表 Front 已接入真实游戏会话。
原 MainWindow 的保存路径也注入真实游戏进程状态；其逻辑变化通过主项目构建和服务门禁检查覆盖，完整真实游戏回归不属于本轮。

### 4.2 配置目录独占与误用保护（必测）

保留一个使用 `$testDir` 的预览窗口，在第二窗口用相同路径再启动预览：预期第二个进程退出码 1、打印锁占用错误；第一个窗口继续正常工作，配置不损坏。

第二窗口先进入仓库根目录、设置相同的 `$testDir`，再执行（这里不调用第一窗口定义的函数）：

```powershell
dotnet run --project tools/FrontPreview/FrontPreview.csproj -c Release --no-build -- --settings-dir $testDir
if ($LASTEXITCODE -ne 1) { throw '同目录第二个预览进程应被拒绝并返回 1' }
```

关闭预览后创建一个不带标记、非空的临时目录，模拟不能接管的已有配置目录：

```powershell
$refused = Join-Path $env:TEMP "MCUG-refuse-existing-$stamp"
New-Item -ItemType Directory -Path $refused | Out-Null
Set-Content -Path (Join-Path $refused 'keep.txt') -Value 'do not change'
dotnet run --project tools/FrontPreview/FrontPreview.csproj -c Release --no-build -- --settings-dir $refused
$refusedExitCode = $LASTEXITCODE
if ($refusedExitCode -ne 1) { throw "预期拒绝已有非空目录（退出码 1），实际 $refusedExitCode" }
Get-Content (Join-Path $refused 'keep.txt')
Get-ChildItem -Force $refused
```

预期输出“仅使用空目录或带 .front-preview 标记的测试目录”；keep.txt 不变，不生成 settings.json。不要使用真实配置目录做拒绝测试，也不要手动给正式目录添加标记。

### 4.3 读取失败恢复（必测，仅使用测试副本）

关闭预览。复制 `$testDir` 的 `settings.json` 到 `$evidence` 作为恢复备份，然后仅把测试文件的 `SchemaVersion` 改成 99。
重开同一测试目录，点击设置：预期显示高版本配置读取错误、保存按钮禁用、重新读取可用；原文件不被覆盖。
关闭窗口，从证据目录的备份恢复测试文件，重开后读取/保存恢复正常。不要把 JSON 改坏代替这一步，损坏配置走的是旧 SettingsStore 的备份恢复流程。

可直接执行以下代码。`ConvertTo-Json` 必须指定足够深度，不能因 PowerShell 默认深度而截断其他偏好：

```powershell
$settingsFile = Join-Path $testDir 'settings.json'
$schemaBackup = Join-Path $evidence 'settings.before-schema-test.json'
Copy-Item -LiteralPath $settingsFile -Destination $schemaBackup
$document = Get-Content -Raw $settingsFile | ConvertFrom-Json
$document.SchemaVersion = 99
$document | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $settingsFile -Encoding UTF8
$newerHash = (Get-FileHash $settingsFile -Algorithm SHA256).Hash
Invoke-Dotnet -Arguments @('run', '--project', 'tools/FrontPreview/FrontPreview.csproj', '-c', 'Release', '--no-build', '--', '--settings-dir', $testDir)
# 在窗口中确认读取失败和禁用保存，关闭窗口后继续。
if ((Get-FileHash $settingsFile -Algorithm SHA256).Hash -ne $newerHash) { throw '高版本配置被改变' }
Copy-Item -LiteralPath $schemaBackup -Destination $settingsFile -Force
Invoke-Dotnet -Arguments @('run', '--project', 'tools/FrontPreview/FrontPreview.csproj', '-c', 'Release', '--no-build', '--', '--settings-dir', $testDir)
```

### 4.4 已有资源页与导航回归（必测，不要求网络）

去掉 `--simulate-game-running`，保留模拟资源默认模式：搜索空关键词、`empty`、`error`，验证正常/空/错误与恢复；翻页并查看项目版本，再返回设置。
确认开始页仍为模拟账号/实例、启动按钮禁用；安装、内容管理、日志、任务仍为明确占位页；设置页是本轮新增的真实写入页面。
资源网络查询 `--live` 是可选项，不影响本轮设置必测结论；资源浏览本身仍不安装、不登录、不保存正式配置。

## 5. 记录、交付与删除临时提示词

关闭所有测试预览窗口后结束日志：

```powershell
Stop-Transcript
git diff --check
git status --short
```

证据日志与截图放 `$evidence`，若要跨设备交付，复制到约定位置；文档写清实际路径，不要只写一个其他设备不可访问的“已通过”。
不要提交 `bin/obj`、测试配置或账号数据。临时目录无需强制删除，防止证据丢失；没有用户授权不要清理仓库其他目录。

把下面的模板追加为一次真实执行记录，并更新 `FRONT_ACCEPTANCE.md` 中相关项。历史 `TESTING.md` 的成功记录不代表本轮已通过。

```text
日期 / 测试 agent：
Windows 版本 / 架构 / DPI：
分支 / 提交哈希 / 未提交补丁说明：
实际 dotnet --version：
证据与截图路径：

构建：逐条命令、退出码、警告/首个错误
设置检查：退出码、ALL_FRONT_SETTINGS_CHECKS_PASSED 是否出现
相关回归：逐条命令、退出码、首个失败断言
GUI：第 3 节逐行结果（通过 / 失败 / 未测）及截图
门禁 / 独占 / 拒绝目录 / 高版本读取 / 导航回归：结果与证据
修复：原因、修改文件、修复后重跑的检查
剩余阻塞 / 未测项目：
本轮结论：通过 / 失败 / 部分完成
AGENT.md：全部完成并记录后删除；未完成时保留
```

### 当前记录

| 环境 | 状态 | 证据 |
| --- | --- | --- |
| 开发端 Linux，无 dotnet | 本轮 Windows 编译、控制台检查、GUI 验收均未执行 | 等待 Windows agent 填写，不允许预先勾选通过 |

### 2026-10-28 Windows x64 执行记录

- Windows 11 x64，版本 10.0.22631；.NET SDK 8.0.426；当前分支 `refactor-dcg`，提交 `399a36b6f054486ca2a02d8285fc192c1e43bd2c`。
- 起始工作区干净。修复：`CollectionsUI.cs` 将不存在的 `Modrinth` provider 改为 `new ContentService(http, CurrentToken)`；`DemoResourceBrowser.cs` 补 `System.IO`，以解析模拟错误路径的 `IOException`。
- `dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release`：退出码 0，0 警告、0 错误。
- `dotnet build tools/FrontPreview/FrontPreview.csproj -c Release`：首次因 `DemoResourceBrowser.cs` 缺少 `System.IO` 失败（退出码 1）；修复后重跑退出码 0，0 警告、0 错误。
- `dotnet build test/CompatibilityChecks/CompatibilityChecks.csproj -c Release`：退出码 0，0 警告、0 错误。
- `dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release --no-build -- --front-settings`：退出码 0，出现 `ALL_FRONT_SETTINGS_CHECKS_PASSED`。
- 回归命令及结果：`--business-services`、`--launch-controller`、`--front`、`--management`、`--content-update-controller` 均以退出码 0 通过；`dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --queue-service` 退出码 0。
- 产物：主程序 `src/MCUGLauncher/bin/Release/net8.0-windows/win-x64/MCUGLauncher.exe`；预览完整输出目录 `tools/FrontPreview/bin/Release/net8.0-windows/win-x64/`；设置检查 `test/CompatibilityChecks/bin/Release/net8.0-windows/`。未运行预览 GUI；Computer Use native pipe 不可用，因此 GUI 表格第 3 节所有原生交互项目仍为“未测”。门禁、目录独占/拒绝、高版本配置及资源导航 GUI 回归亦未测。
- 本机没有建立 transcript 或截图证据。`git diff --check` 通过。结论：构建与自动化部分通过，整体验收部分完成；保留根目录 `AGENT.md`，不删除旧 UI。提交情况以本轮 Git 历史为准。
