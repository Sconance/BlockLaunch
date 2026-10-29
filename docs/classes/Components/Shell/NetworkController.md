# NetworkController

网络设置的编辑与页内保存，使用显式回调。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `NetworkClick`。
- **构造依赖：** `Func<string, StackPanel, InlineDialog> createDialog`、`Func<NetworkSettings> getSettings`、`Action<NetworkSettings> saveSettings`。
- **源码注释：** Coordinates editing, persistence and inline hosting through explicit callbacks.
- **源码：** [`NetworkController.cs`](../../../../src/MCUGLauncher/Components/Shell/NetworkController.cs)。
