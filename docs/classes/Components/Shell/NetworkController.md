# NetworkController

网络设置的编辑与页内保存，使用显式回调。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `NetworkClick`、`CreateNetworkPage`。
- **构造依赖：** 网络偏好读取、保存、旧入口页面展示和错误提示回调。页面直接返回 SettingsPageContent，复用独立 NetworkSettingsForm。
- **源码注释：** Coordinates editing, persistence and inline hosting through explicit callbacks.
- **源码：** [`NetworkController.cs`](../../../../src/MCUGLauncher/Components/Shell/NetworkController.cs)。
