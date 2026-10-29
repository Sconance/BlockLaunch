# Windows AI 测试任务提示词（临时文件）

> **最新要求：不再做 Linux 本地构建或测试，直接在 Windows 构建当前提交。** 最新克隆实例、脚本导出与 Front 脚本参数解析代码尚未编译或运行；下文历史成功结果不能作为这批增量的证据。先按 `docs/dcg/FRONT_UI_WINDOWS_TEST.md` 构建新旧入口，执行自动化检查，再按“克隆与脚本增量”验收。修复后重跑受影响项；结果记入该文档。未完成时保留本文件，不自动提交或推送。

> **最新界面增量：先读 `docs/dcg/FRONT_UI_WINDOWS_TEST.md`。** 本轮新增版本管理、资源详情页、外观/高级启动设置、游戏日志页，以及导航父级复用修复。执行 `FrontRuntimeChecks --ui-structure`（Windows STA、无网络 fixture）和普通运行检查。Linux 已交叉编译并运行共享业务断言；原生 WPF、视觉/DPI、真实账号与游戏仍未测。不要使用已删除的 FrontPreview；不要自动提交或推送。所有当前必测项完成并记录后删除本文件。

> **优先执行：独立双前端项目验收。** 先阅读 `docs/dcg/FRONTEND_PROJECTS.md`；新工程为 `src/MCUGLauncher/Front/Front.csproj`，旧 tools/FrontPreview 工程已删除。运行 `scripts/build-windows.ps1 -Frontend Both` 和 `python scripts/check-frontend-boundaries.py --build`（后者需 Python 3，仅操作自动清理的临时副本），再执行 `docs/dcg/FRONT_RUNTIME.md` 的运行回归。下载共用界面现位于 `src/MCUGLauncher.SharedUI/Downloads/`。开发端已用 Linux 临时 SDK 交叉编译，但未在 Windows 启动验收；下文的旧预览路径/模拟参数均为历史记录。保留本文件直到当前必测项完成。

> **当前任务已更新：先阅读 `docs/dcg/FRONT_RUNTIME.md`，构建并验收真实 Front 模块宿主。** 默认入口使用正式配置并恢复执行下载；不得按下文旧隔离预览说明直接运行。旧 `--live` 和 `--simulate-game-running` 已移除，输出为 `MCUGLauncher.Front.exe`。先跑 FrontRuntimeChecks、DownloadRunnerChecks 和文档中的无网络回归；真人账号/真实游戏操作必须得到设备所有者授权。当前新增代码尚未进行 Windows 编译或运行验收，保留本文件直到当前必测项完成并记录。

> 测试完成并把证据、结果与未通过项写入文档后，删除根目录的 `AGENT.md`。
> 如果编译失败、缺少 GUI 权限或仍有必测项未执行，保留本文件并说明阻塞原因，不得声称验收完成。
> 本文件是测试任务交接，不是长期仓库规范；不要另建一个 `AGENTS.md` 代替它。

## 给接手 agent 的任务

你正在 Windows x64 设备上验收 MCUGLaunch 的 Front 设置页解耦。请直接执行下面的任务，不要只提供建议。

1. 阅读 `docs/FRONT_SETTINGS_WINDOWS_TEST.md`，按其中的顺序核对源码、构建项目、运行自动化检查。
2. 启动 FrontPreview，在隔离配置目录验证设置页真实读取、保存、失败提示和重启恢复。
3. 完成游戏运行模拟门禁、配置目录独占、非法输入和资源页回归验证。
4. 将结果与证据追加到 `docs/FRONT_SETTINGS_WINDOWS_TEST.md` 的验收记录，更新 `docs/FRONT_ACCEPTANCE.md` 对应条目。
5. 确认所有本轮必测项已完成、有结果记录后删除 `AGENT.md`。不要自动提交或推送。

## 必须遵守

- 起始分支预期为 `refactor-dcg`，但先记录实际分支、提交哈希和工作区状态。不要切分支、拉取覆盖或丢弃未提交修改。
- 确认当前源码包含 `SettingsView.xaml`、`IFrontSettings.cs`、`FrontSettingsChecks.cs` 和 `--front-settings`。缺少说明拿到的是旧源码，先报告，不要测试旧提交后标记本轮通过。
- SDK 由 `global.json` 指定：8.0.426，允许同 feature band 更新补丁。需要 SDK，不是只安装 .NET Runtime。
- 测试项目是控制台断言程序，用 `dotnet run`，不是 `dotnet test`。
- 首次 NuGet 还原需要网络；不得把还原失败当作业务断言失败。不要擅自升级 SDK、依赖或降低目标框架。
- FrontPreview 的资源列表默认是模拟的，但设置页是真实文件读写。只使用指南中独立的 `$testDir`，不能使用正式启动器配置目录或真实游戏目录。
- 不输入微软/MUA 凭据，不运行真实游戏，不对重要实例执行安装、删除、恢复。不手动给正式目录添加 `.front-preview` 标记。
- `--simulate-game-running` 仅是宿主注入业务门禁的测试开关，不证明真实游戏生命周期已经迁入 Front。
- 只修复本轮设置接口、页面、预览宿主或相关测试引入的问题。发现其他失败先记录，未经授权不进行大范围重构。
- 如需修复，先阅读适用仓库指令，修改后重跑受影响的检查；记录修复文件和失败原因。
- 不以静态检查、截图或窗口成功打开代替自动化断言；也不以断言通过代替视觉与原生点击验收。
- 无法操作桌面时完成可执行的自动化部分，GUI 项标记“未测”，保留本文件。

## 当前交接状态

### 账号与认证模块增量

共享账号与认证策略现集中在 `src/MCUGLauncher/Accounts/`，旧授权窗口与头像渲染移到
`MainWindow/UI/Accounts/`。先阅读 `docs/dcg/ACCOUNTS_MODULE.md`。
开发端账号策略检查、AuthChecks/MuaChecks 编译及双前端移除构建已通过；Windows 原生授权、
取消、静默刷新、凭据恢复与退出后清理仍需验收，不得把编译通过写成真实登录通过。
保留原有未测记录和本文件，直到 Windows 必测项完成。

### 后续增量：统一下载入口（本轮尚未编译运行）

用户已明确暂缓设置 GUI 验收，继续代码拆分；这不代表旧未测项通过。保留本文件和既有未测记录。
下载业务集中在与 MainWindow 同级的 `src/MCUGLauncher/Downloads/`，新增统一暂停状态与持久化，三处旧 UI 共用同一操作规则。
测试本轮请先读 `docs/dcg/DOWNLOAD_MODULE.md`，执行其中的 Windows 主项目构建、QueueChecks 专项/完整检查和 MiniDownloadChecks，再跑设置接口回归。
专项预期输出 `ALL_DOWNLOAD_QUEUE_SERVICE_CHECKS_PASSED`；新增测试未在开发端运行，不可沿用 `docs/TEST_RESULTS_2026-10-28.md` 的旧成功结论。
如暂时无法原生操作 UI，仅记录自动化结果，GUI 项继续保留“未测”。本轮允许修复下载入口、暂停存储及相关测试引入的问题，不自动提交或推送。

- 已实现 Front 设置接口、适配器、ViewModel 和页面，原正式入口仍是 MainWindow。
- 保存业务已增加运行门禁，并在持久化成功后才更新调用方的内存偏好。
- 已添加 `--front-settings` 自动化检查及隔离配置预览参数。
- 开发端没有 `dotnet`、不是 Windows，没有进行本轮 C# 编译、自动化运行或 GUI 验收。
- 历史 `docs/TESTING.md` 的成功记录不能作为这批新改动的验收证据。

## 最终汇报格式

请用中文汇报：实际提交/工作区、Windows 与 SDK 版本、每条构建和测试命令的退出码、GUI 表格结果、发现/修复的问题、日志及截图路径、剩余阻塞项、`AGENT.md` 是否已删除。

任何没有亲自执行的项目只能标记“未测”；删除交接文件不等于功能测试通过。

## 共享下载界面增量验收（未测）

旧 `MainWindow/UI/Downloads/DownloadTaskUI.cs` 已删除，宿主接入位于 `MainWindow/DownloadTasksHost.cs`。
旧窗口和 Front 共用 `Front/Views/DownloadTasksView`，均通过契约调用 `Downloads/DownloadFrontendService` 与 `DownloadQueueService`。
按 `docs/dcg/DOWNLOAD_MODULE.md` 构建主程序、FrontPreview，并执行 `QueueChecks --download-front`（预期 `ALL_FRONT_DOWNLOAD_CHECKS_PASSED`）、队列完整检查和 MiniDownloadChecks。
GUI 验证旧历史按钮/共享卡片/迷你条交叉控制，以及导航切换、刷新、重开和关闭释放；FrontPreview 不执行真实下载，不共享正式进程队列。
这批修改尚未运行 C# / WPF 测试；保留未测项，禁止沿用旧测试报告宣称通过。
