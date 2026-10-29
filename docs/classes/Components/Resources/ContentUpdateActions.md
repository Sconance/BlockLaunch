# ContentUpdateActions

内容更新检查所需的应用操作集合。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Action<string, string, DownloadRequest> EnqueueDownload`、`Func<string, StackPanel, InlineDialog> FeatureDialog`、`Func<HttpClient> GetHttp`、`Func<CancellationToken> GetToken`、`Func<bool> IsBusy`、`Action<string> Log`、`Func<string> ManagedDirectory`、`Window Owner`。
- **源码：** [`ContentUpdatesController.cs`](../../../../src/MCUGLauncher/Components/Resources/ContentUpdatesController.cs)。
