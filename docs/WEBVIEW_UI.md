# WebView2 UI 开发说明

## 分层与入口

- `MainWindow.xaml.cs`：WebView2 生命周期、可信页面限制、Windows 窗口操作与原生文件选择；不实现账号、下载或配置业务。
- `UseCases/LauncherApplication`：唯一消息分发入口，校验消息后调用按职责划分的 Commands，并生成界面状态。
- `UseCases/LauncherContext`：共享配置、账号会话、HTTP 生命周期与发送/刷新回调；不包含控件树，不引用窗口。
- `AccountCommands`、`GameCommands`、`InstanceCommands`、`ResourceCommands`、`SettingsCommands`、`DiagnosticCommands`、`CommunityCommands`：各自负责对应业务流程；复用 `Services/` 与 Core。
- `IDesktopHost`：业务调用原生窗口操作的接口，检查时替换为假实现。
- `Ui/app.js`：页面与演示数据；`Ui/host.js`：真实宿主数据绑定和交互；样式保持原分支。

新增命令应放入所属 Commands，在分发器登记并补协议检查。不要把业务搬回窗口，也不要通过 partial 文件隐藏窗口复杂度。

## 消息协议

页面仅从 `https://app.local` 向宿主发送 JSON 对象，`type` 为命令名。字段名称延续 0.22.3，不改已有页面协议：例如 `saveSettings` 携带 `player/root/java/ram/width/height`；`toggleMod` 携带 `file/enabled`。

分发前校验字段类型，错误以 `{type:"error",message:"..."}` 返回。各业务的响应继续使用 `state`、`settingsSaved`、`launchResult`、`contentList` 等原消息类型。发送由窗口统一切换到 UI 线程；窗口关闭后丢弃响应，并取消使用宿主令牌的请求。启动安装器等尚未接受令牌的 Core API 仍需后续补齐取消支持。

初始化先发送本地状态，再并行读取社区与恢复微软账号，重复 ready 不重复执行在线初始化。在线账号未完成验证时不允许静默以离线身份启动。

## 验证

```powershell
python scripts/check-web-boundaries.py
./scripts/run-checks.ps1 -Name AuthChecks,MuaChecks,DownloadChecks,QueueChecks,CompatibilityChecks,WebBridgeChecks
```

`WebBridgeChecks` 使用隔离目录、假桌面接口和假 HTTP：覆盖真实消息分发、配置持久化、畸形消息、服务器增删、社区本地写入、Mod 开关、下载版本/加载器过滤、下载文件名和离线 UUID。不会读取或覆盖用户账号，也不连接真实服务。

还需人工验收：真实 WebView2 拖动/吸附、多 DPI、微软/MUA 授权、Minecraft 完整启动、各资源类型的真实下载。浏览器页面渲染和隔离检查不能替代这些验收。

## 后续工作

- 按页面拆 `app.js`，按账号/资源/设置拆 `host.js`，保留当前 DOM 与消息协议。
- 将现有字符串命令逐步改为类型明确的请求/响应模型，扩充协议字段及错误状态检查。
- 补运行游戏会话管理与进程退出验收；迁移版不能仅凭“构建成功”宣布旧功能全部恢复。
- 下载任务取消、整合包导入、依赖选择等尚未完成的界面入口应继续明确提示，不能用演示数据冒充完成。
