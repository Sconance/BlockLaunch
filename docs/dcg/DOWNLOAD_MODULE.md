# 统一下载入口与模块边界

## 当前结构

下载业务集中在 `src/MCUGLauncher/Downloads/`，与 `MainWindow/` 同级；由独立 Application 项目链接编译，新旧前端引用同一业务程序集。

```text
src/MCUGLauncher/
├── Downloads/
│   ├── DownloadQueueService.cs    入队、取任务、执行、状态变更、切换、排序与退出挂起
│   ├── DownloadQueueModels.cs     暂停状态、命令结果与暂停存储接口
│   ├── DownloadPauseStore.cs      运行时暂停状态与配置持久化适配
│   ├── DownloadTaskExecutor.cs    游戏、资源、整合包等任务执行
│   └── DownloadFrontendService.cs 契约适配；任务快照与前端操作映射
├── ../MCUGLauncher.SharedUI/Downloads/DownloadTasksView  中立共享任务列表与操作按钮
├── ../MCUGLauncher.SharedUI/Downloads/DownloadTasksViewModel.cs  共享状态与操作适配
├── Contracts/IFrontDownloadTasks.cs  UI 与业务之间的最小契约
└── MainWindow/UI/Downloads/       迷你下载条、任务入口、任务栏及旧页面适配
```

不把窗口 partial 源码搬进 Downloads 冒充解耦。该业务目录不引用 `MainWindow`、WPF 或 Dispatcher。
底层 `OperationCoordinator` 仍在 Core，因为它也协调账号、卸载等非下载操作，不能整体移动或替换成下载专用协调器。

## 统一入口

| 调用方 | 下载模块入口 |
| --- | --- |
| 下载队列历史按钮、任务卡片 | `Change(id, state)` |
| 迷你下载条暂停/继续/重试 | `Toggle(id)`，内部复用 Change |
| 通用取消按钮中的下载分支 | 同一 `ChangeDownloadById` UI 适配方法 → Change |
| 账号操作前确认暂停下载 | Change(Paused)，随后等待原任务清理 |
| 实例卸载时撤销关联下载 | Change(Cancelled) |
| 上移/下移 | Move |
| 关闭窗口时挂起下载 | SuspendForShutdown |
| 游戏/资源/整合包加入队列 | Enqueue；PumpDownloads 通过 NextQueued / Execute 执行 |

UI 适配只负责取得任务 ID、调用服务、显示警告及刷新控件。旧 MainWindow 和 Front 的任务卡片都使用 `DownloadFrontendService`；非下载的取消仍交给 OperationCoordinator，不混入下载状态。

## 状态规则

1. 手动暂停任何可暂停下载：活动任务请求取消清理，排队任务标为 Paused；同时暂停整个队列并记录任务 ID。现在历史按钮与迷你条行为一致。
2. 继续/重试：允许 Paused、Failed、Interrupted 返回 Queued；已经 Queued 的任务也可显式解除全局暂停。运行中、已完成、已取消的任务不能被继续或重试。
3. 取消：保留协调器允许的转换；若取消当前暂停锚点，清空 PausedDownloadId，但不擅自启动其他任务。需显式继续一个排队任务才解除全局暂停。
4. Committing 阶段拒绝暂停、取消、切换及退出挂起，等待提交结束。
5. 关闭窗口的 SuspendForShutdown 不等于手动暂停：清理后回到 Queued，不新增手动暂停标记；下次启动按原规则恢复。

`DownloadPauseStore` 在运行时更新现有偏好对象，避免旧界面读到过期暂停状态；磁盘保存只覆盖最新配置的两个下载暂停字段，避免用旧窗口快照覆盖其他设置。
队列任务记录与 settings.json 并非一个跨文件事务。任务状态改变后若暂停偏好保存失败，返回 `Changed=true` 和非空 Warning；UI 必须显示警告，不能误报任务操作失败或回滚已接受的取消。
磁盘失败时仍保留本次进程的实际暂停状态；不能保证全局暂停偏好跨重启保留。有效操作再次执行会重试保存。

## 当前边界

旧 MainWindow 仍管理自身 WPF 定时器、busy/game 门禁、迷你条和任务栏。新 Front 由 `FrontLauncherRuntime` 组装真实模块，调用不依赖 WPF 的 `Downloads/DownloadQueueRunner` 执行队列，使用相同配置锁与状态规则。
下载任务列表、进度卡片和暂停/继续/取消按钮由 `src/MCUGLauncher.SharedUI/Downloads/DownloadTasksView` 共享，不再属于 Front。Front 现使用正式配置并执行下载；账号/启动及设置 GUI 的未验收项目仍为未测，不因代码接入而被勾选。

## Windows 编译与回归

本轮开发端没有 dotnet；以下命令尚未执行，不能沿用前一轮测试报告。Windows x64 安装 global.json 指定的 SDK 8.0.426，在仓库根目录依次执行：

```powershell
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
dotnet build src/MCUGLauncher/Front/Front.csproj -c Release
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --download-front
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --queue-service
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release
dotnet run --project test/MiniDownloadChecks/MiniDownloadChecks.csproj -c Release
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front-settings
```

每条退出码都应为 0；专项队列检查还须出现 `ALL_DOWNLOAD_QUEUE_SERVICE_CHECKS_PASSED`，迷你条检查须出现 `ALL_MINI_DOWNLOAD_CHECKS_PASSED`。
MiniDownloadChecks 需要 Windows WPF/Dispatcher，使用 fixture 网络，不是实际下载游戏；这些是控制台程序，用 dotnet run，不是 dotnet test。

新增覆盖：全局暂停与取任务门禁、跨入口继续、配置重启恢复、偏好不覆盖、无效状态拒绝、失败重试、活动任务清理、提交保护、退出重入队、排序、配置保存失败警告。
MiniDownloadChecks 追加旧历史按钮 → 任务卡片 → 迷你条的交叉控制，确认同一任务与同一暂停偏好。

手工界面验收仍待执行：在可丢弃配置中运行 fixture 下载，分别用三处按钮操作同一任务，检查暂停提示/继续/取消状态同步；账号操作前暂停、退出清理、卸载关联任务以及排序均不可绕过提交保护。不需要真人账号，不操作重要实例。
记录实际 commit、命令/退出码、首个失败、截图及未测项；本轮不自动提交或推送，也不把历史未完成验收改为通过。

## 共享下载界面验收补充（待测）

调用链：两个宿主 → `DownloadTasksView` → `DownloadTasksViewModel` → `IFrontDownloadTasks` → `Downloads/DownloadFrontendService` → `DownloadQueueService` → Core。
ViewModel 不引用 MainWindow、OperationRecord 或 WPF；所有状态规则、按钮能力与任务排序来自 Downloads。
旧窗口的独立卡片生成代码和 `MainWindow/UI/Downloads/DownloadTaskUI.cs` 已删除；宿主接入保留在 `MainWindow/DownloadTasksHost.cs`，只负责打开共享组件、状态反馈和窗口联动。安装页、入队入口、迷你条与调度宿主仍保留，不能删除整个 `MainWindow/UI/Downloads`。

1. 自动检查 `--download-front` 须输出 `ALL_FRONT_DOWNLOAD_CHECKS_PASSED`，覆盖空队列、任务快照、活动进度、跨调用方控制、失败重试、提交保护、排除非下载任务、错误恢复和释放。
2. 旧窗口打开下载任务，确认显示共享卡片；用历史按钮、共享卡片和迷你条交叉暂停/继续/取消同一任务，并检查重开页面、滚动、定时刷新、缩放和关闭。
3. Front 的“任务与恢复”读取配置目录 `tasks/`；启动恢复后实际执行未暂停任务，切换导航不停止后台调度。真实测试使用可丢弃实例，详见 `FRONT_RUNTIME.md`。
4. Front 与主窗口使用同一后端实现，默认共用正式持久化队列；配置锁禁止两个宿主同时运行。退出后可用另一入口恢复任务，不是两个进程同时控制活动下载。
5. 检查读取失败可刷新恢复、警告可见、已完成/提交中按钮禁用；退出释放定时器，不应出现窗口关闭后的回调异常。

当前仅完成静态检查，以上编译、自动检查及 GUI 项均未运行。Windows 基线测试报告不涵盖这批未提交改动。
