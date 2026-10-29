# ExperienceController

连接体验功能的应用操作。服务器与存档表单已独立到 Views/Settings，通过显式 Actions 接收状态与操作；本类负责绑定工作区、运行任务及服务器连接。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `BuildExperienceUI`、`ConfirmModScan`、`OpenDownloadLimit`、`OpenSaveProtection`、`CreateSaveProtectionPage`、`OpenServers`、`CreateServersPage`、`ProtectWorlds`。
- **构造依赖：** `LauncherWorkspace workspace`。
- **源码：** [`ExperienceController.cs`](../../../../src/MCUGLauncher/Components/Experience/ExperienceController.cs)。
