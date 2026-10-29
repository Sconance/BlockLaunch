# InstancePackExportActions

整合包导出所需的数据与操作。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Func<string, StackPanel, InlineDialog> FeatureDialog`、`Func<LaunchSettings> GetGlobalLaunch`、`Func<string> GetRoot`、`Func<CancellationToken> GetToken`、`Func<bool> IsBusy`、`Action<string> Log`、`Func<string, PackRequirements> RequirementsFor`、`Action RequireStopped`。
- **源码：** [`InstancePackExportDialog.cs`](../../../src/MCUGLauncher/Presentation/InstancePackExportDialog.cs)。
