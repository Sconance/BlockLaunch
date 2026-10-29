# LauncherWorkspace

组合根：构造各功能模块、连接页面动作与 Core 服务，本身不保存业务状态。

- **类型：** `internal sealed partial class`，基类/实现：无。
- **主要方法：** `CreateInstancePackExportDialog`、`CreateLaunchOptionsDialog`、`CreateSketchContentManagerDialog`、`CreateVersionSettingsPages`、`RoutePageAction`。
- **构造依赖：** `MainWindow window`、`string configurationDirectory`。
- **源码注释：** Composition root. Feature state belongs to the individual modules.
- **源码：** [`InstanceDialogComposition.cs`](../../../src/MCUGLauncher/Composition/InstanceDialogComposition.cs)、[`LauncherPageRouter.cs`](../../../src/MCUGLauncher/Composition/LauncherPageRouter.cs)、[`LauncherWorkspace.cs`](../../../src/MCUGLauncher/Composition/LauncherWorkspace.cs)、[`ResourceComposition.cs`](../../../src/MCUGLauncher/Composition/ResourceComposition.cs)、[`VersionSettingsComposition.cs`](../../../src/MCUGLauncher/Composition/VersionSettingsComposition.cs)。
