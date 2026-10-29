# VersionTaskActions

版本编辑共享的任务执行能力。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Func<HttpClient> GetHttp`、`Func<CancellationToken> GetToken`、`Func<bool> IsBusy`、`Action RequireStopped`、`Func<Func<Task>, string, Task> Run`。
- **源码注释：** Task execution capabilities shared by version editors; no account or global UI access.
- **源码：** [`VersionTaskActions.cs`](../../../src/MCUGLauncher/Presentation/VersionTaskActions.cs)。
