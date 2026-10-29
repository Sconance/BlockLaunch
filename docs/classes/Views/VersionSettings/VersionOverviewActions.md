# VersionOverviewActions

版本概览页面所需的操作。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Action<string, string, DownloadRequest> EnqueueDownload`、`Action ExportScript`、`Func<bool> IsBusy`、`Action<string> OpenFolder`、`Action OpenLibrary`、`Action ReturnToOriginal`、`Action Uninstall`、`Func<string, Task> UpgradePack`。
- **源码：** [`VersionOverviewPage.cs`](../../../../src/MCUGLauncher/Views/VersionSettings/VersionOverviewPage.cs)。
