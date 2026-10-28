# 账号、启动、下载与设置业务拆分

## 当前结果

- `Settings/LauncherSettingsService` 接收设置快照，负责数值校验、目录规范化、Java 检查、写回 `Preferences` 和持久化；控件读取仍由 `PreferencesUI` 完成。
- `Downloads/DownloadQueueService` 负责入队、选择下一个任务、执行和状态切换；WPF `DispatcherTimer`、进度条和提示文本仍属于 UI 宿主。
- `Accounts/AccountSessionService` 统一微软/MUA/离线会话模型；微软 OAuth 窗口、MUA 角色选择和账号管理对话框仍是交互适配层。
- `Launch/GameLaunchController` 负责启动编排；`Launch/GameSessionMonitor` 负责等待游戏退出、生成诊断结果；窗口显示诊断和日志仍由 UI 负责。

## 调用关系

```text
WPF UI
 ├─ 读取控件快照 → LauncherSettingsService → SettingsStore
 ├─ 操作队列 → DownloadQueueService → OperationCoordinator → DownloadTaskExecutor
 ├─ 授权交互 → AccountSessionService → MicrosoftAccountService / MuaAccountService
 ├─ 启动按钮 → GameLaunchController → GameSession
 └─ 会话观察适配器 → GameSessionMonitor → Diagnostics
```

## 尚未完全移出的内容

- 下载定时器、任务列表刷新、暂停标记保存和窗口关闭策略仍在 `MainWindow` partial UI 中。
- 账号管理对话框仍负责 OAuth WebView、密码输入、MUA 角色选择和提示文案。
- 启动后的窗口隐藏/恢复、游戏输出队列和诊断页面仍是 UI 适配职责。

这些部分依赖 WPF 生命周期或用户交互，不应直接放入纯业务服务。下一阶段可为它们定义事件/回调接口，再移除 `MainWindow` partial 中的具体实现。

## 验证限制

当前环境没有 `dotnet` 且不是 Windows，无法编译 WPF 或运行真实账号、启动和界面流程。本次可执行的验证应包括 `scripts/check-layout.py`、`git diff --check` 和静态依赖检查；Windows 环境需要再运行兼容性项目。

```powershell
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --business-services
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --launch-controller
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --queue-service
dotnet run --project test/AuthChecks/AuthChecks.csproj -c Release
```

待验收（当前均未运行）：

- [ ] 保存设置后重启，检查内存、分辨率、Java 和游戏目录；非法输入不得保存。
- [ ] 微软缓存刷新、需要交互时重新授权、取消授权、清除凭据。
- [ ] MUA 登录、取消角色选择、刷新与退出；令牌不得写入界面日志。
- [ ] 下载排队、暂停、继续、重试、提交阶段拒绝取消；切换目录不改变原任务快照。
- [ ] 正常退出、主动关闭、异常退出诊断；新会话启动后，旧会话不得覆盖新页面状态。

Front 尚未接入这些写操作，旧 UI 仍不可删除。本次也没有把 Windows DPAPI、OAuth/WebView 或游戏进程替换成跨平台实现。
