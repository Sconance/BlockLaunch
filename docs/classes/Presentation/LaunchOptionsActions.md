# LaunchOptionsActions

启动选项读写所需的应用回调。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Func<string, StackPanel, InlineDialog> FeatureDialog`、`Func<Preferences> GetPreferences`、`Func<bool> IsBusy`、`Action RefreshLaunchSettingsFields`、`Action RequireStopped`、`Action SavePreferences`。
- **源码：** [`LaunchOptionsDialog.cs`](../../../src/MCUGLauncher/Presentation/LaunchOptionsDialog.cs)。
