# VersionModActions

版本 Mod 页面所需的操作。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Func<string, Task> CheckUpdates`、`Func<bool> IsInstalled`、`Func<string, Task> RestoreContent`、`Func<string, Task> Scan`、`Action SearchMods`。
- **源码：** [`VersionModPage.cs`](../../../../src/MCUGLauncher/Views/VersionSettings/VersionModPage.cs)。
