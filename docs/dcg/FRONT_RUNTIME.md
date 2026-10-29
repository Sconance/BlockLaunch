# Front 真实模块接入与 Windows 验收

> 最新状态：实例克隆与 Front 启动脚本接线已修改，用户要求不做 Linux 本地构建或测试，直接交由 Windows 验收。这批增量尚未编译、未运行；下文历史成功记录不适用于最新代码。先执行 `FRONT_UI_WINDOWS_TEST.md`。

## 当前行为

Front 不再使用模拟资源或临时预览配置。默认读取 `%LOCALAPPDATA%\BlockLaunch`，与旧 MainWindow 共用设置、受保护账号缓存、实例及持久化队列。
两个入口使用同一 `download-queue.lock` 独占配置目录，不允许同时操作同一配置。Front 并非通过隐藏 MainWindow 调用业务，而是自己组装已经拆出的服务。

独立工程位于 `src/MCUGLauncher/Front/Front.csproj`，输出为 `MCUGLauncher.Front.exe`；旧 tools/FrontPreview 工程已移除。
Front 的入口、界面、主题及宿主均由自身工程编译，只引用 Application 与 SharedUI，不引用旧启动器项目。详见 [项目边界与独立构建](FRONTEND_PROJECTS.md)。

| Front 入口 | 调用模块 | 已接入范围 |
| --- | --- | --- |
| 开始游戏 / 账号 | Accounts + Authentication | 离线名称校验/选择、微软交互登录及缓存账号、MUA 登录/角色选择/退出、本地登录清除 |
| 实例与安装 | Versions + Downloads | 真实本地/在线版本、四类加载器兼容版本、游戏安装入队、本地整合包导入 |
| 开始游戏 | Launch | 已安装实例启动、身份刷新、预检确认、正常关闭请求、会话退出与诊断 |
| 内容中心 / 本地内容 | Resources | 真实搜索/版本、外部目录资源下载入队、本地内容读取/启用/禁用 |
| 设置 / 任务 / 日志 | Settings + Downloads + Core | 正式设置保存、下载恢复执行/暂停/继续/取消/重试、当前宿主与游戏脱敏日志 |

主要调用链：`Front/Views → Contracts/IFrontLauncher → Front/Hosting/FrontLauncherRuntime → 独立业务模块 → Core`。
下载列表另经 `IFrontDownloadTasks → Downloads/DownloadFrontendService`；实际执行由 `Downloads/DownloadQueueRunner → DownloadQueueService → DownloadTaskExecutor` 完成。
Front 自己的 WebView2 授权窗口位于 `Front/Hosting/FrontMicrosoftOAuthUI.cs`，不依赖 MainWindow；仍复用账号服务的正式 OAuth 校验和受保护存储。

## 保护规则与未迁移内容

1. 配置独占锁在读取/恢复队列之前获取；启动恢复未完成前不调度下载。未手动暂停的旧下载在启动恢复后会真实继续。
2. 账号操作、设置保存、游戏启动、本地内容修改要求无活动下载且游戏已关闭；需在任务页暂停下载并等待清理。游戏运行时不启动排队任务。
3. 安装、导入、下载与清除账号使用显式按钮；UI 显示真实写入确认。加载器入队必须来自所选游戏版本对应的服务查询快照。
4. 关闭先停止调度、取消交互操作，再将未提交下载挂起重入队；提交中的安装须等待完成。游戏运行时提示用户并请求正常关闭，未退出时保持窗口等待，不强杀游戏。
5. 本轮没有迁移旧前端的全部功能：更新选择/事务回退 UI、实例卸载 UI、整合包升级 UI、完整 Java 管理和备份恢复等仍在旧入口。个性化与高级启动选项已接通。不能整体删除旧 UI。

设置页开放游戏目录、Java、内存、分辨率和高级启动选项，并有网络、外观与备份策略入口；网络配置按宿主启动时快照生效。版本管理支持本地内容列表、切换状态与导入；资源搜索页下载仍保存到游戏目录之外，不自动安装到实例。整合包导入默认不安装可选内容。
安装完成后在启动页展开版本列表并显式刷新。日志页显示当前 Front 进程最近 500 条，也可手动读取选中实例的 `logs/latest.log` 末尾；不是旧窗口完整历史日志查看器。

## Windows 编译与无网络回归（未执行）

要求 Windows x64、`global.json` 指定的 .NET SDK。先关闭所有新旧启动器；测试构建不需要运行正式入口。

```powershell
.\scripts\build-windows.ps1 -Frontend Both
dotnet run --project test/FrontRuntimeChecks/FrontRuntimeChecks.csproj -c Release
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --download-runner
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --management
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --launch-controller
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --business-services
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front-settings
dotnet run --project test/MiniDownloadChecks/MiniDownloadChecks.csproj -c Release
```

各命令退出码必须为 0。新增标记为 `ALL_FRONT_RUNTIME_CHECKS_PASSED`、`ALL_DOWNLOAD_RUNNER_CHECKS_PASSED`、`ALL_FRONT_RESOURCE_BROWSER_CHECKS_PASSED`。
FrontRuntimeChecks 仅操作自行创建的临时配置、离线身份和暂停的 fixture 队列，不读正式账号，不下载、不启动游戏、不执行交互登录。
Runner 检查覆盖宿主门禁、全局暂停、单任务执行、重入、退出重入队、提交期等待、失败释放；资源检查覆盖查询快照入队、错误和释放。

## 真实界面验收（未执行）

不要未经确认直接在有重要实例的正式目录上验收。可指定空测试配置，**这只是显式测试位置，不是隔离预览模式，下载/登录/启动仍是真实行为**：

```powershell
& .\artifacts\windows\front\Release\build\MCUGLauncher.Front.exe --settings-dir "$env:TEMP\MCUG-Front-Runtime-Check"
```

1. 先在设置页把游戏目录设为可丢弃目录；默认新配置的 Root 仍由 Preferences 决定，不能因指定配置目录就认为游戏文件也自动隔离。确认资源下载目录同样可丢弃。
2. 验证离线名称保存及重启恢复；读取已保存账号、微软授权关闭取消、MUA 多角色选择与清除须由设备所有者授权，不收集密码或令牌到报告。
3. 刷新在线/本地版本，查询 Fabric/Quilt/Forge/NeoForge，切换游戏版本确认旧加载器结果失效；确认安装/修复和导入提示，下载后刷新列表。
4. 下载任务页验证暂停/继续/取消/重试，关闭时未提交任务重入队、提交任务等待，重启恢复；启动游戏后队列不执行，运行期间设置/账号/资源修改被拒绝。
5. 验证实际启动、预检取消/确认、游戏输出脱敏、正常停止、带游戏关闭窗口等待退出；验证第二 Front 或旧入口对同目录锁的拒绝，以及不同缩放、导航切换、窗口关闭。

开发端已临时安装 SDK 8.0.426 完成 Linux 交叉编译、双向移除构建及部分无 WPF 回归，结果见 [项目验证记录](FRONTEND_PROJECTS.md)。
当前不是 Windows，未运行 FrontRuntimeChecks 的 WPF 宿主及原生界面验收；历史 2026-10-28 报告不覆盖当前新增宿主，不得据此勾选这些项目。

## 2026-10-29 本轮继续开发结果

本轮新增或修复：

- 统一 `FrontWindow.NavigatePage`，切换页面前解除 `PageHost` 与下载分类容器的父子关系，避免同一个 WPF 控件重复挂载。
- 初始化完成后刷新版本列表；状态计时器同步启动按钮和停止按钮状态。
- 版本管理从占位文本改为 `IFrontVersions`，接通实例元数据、版本专属启动设置、加载器安装入队、实例修复、资源导入、存档列表、整合包导出。
- 资源页改为结果列表→详情页→版本页，显示图标、标签、简介和兼容版本。
- 设置页接通共享网络、外观、存档保护、全局启动设置；旧 UI 也改用共享保存服务。
- 日志页拆为实时输出和游戏 `latest.log`，读取最大 512 KiB 并脱敏。

已执行：

- Front Release 构建：退出码 0，0 警告、0 错误。
- 旧 UI Release 构建：退出码 0，0 警告、0 错误。
- `python3 scripts/check-layout.py`：通过。
- `python3 scripts/check-frontend-boundaries.py`：通过。
- `python3 scripts/check-frontend-boundaries.py --build --dotnet /tmp/mcug-dotnet-sdk/dotnet`：通过，Front 与旧 UI 隔离构建均通过。
- `CompatibilityChecks --business-services` 与 `--management`：通过，分别包含新设置/日志与版本元数据/启动设置断言。
- `FrontRuntimeChecks`：已编译；Linux 运行失败属于 Windows PE 无法在 Linux 执行，不是业务断言结果。

未执行：Windows 原生 UI、DPI、WebView2、真实下载/启动、PowerShell 编译脚本。 `AGENT.md` 继续保留。

原生验收步骤与新 `--ui-structure` 检查见 `FRONT_UI_WINDOWS_TEST.md`；尚未迁移的旧功能见 `FRONT_UI_PARITY.md`。
