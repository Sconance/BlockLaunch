# LoaderOverviewController

加载器版本概览与入队，显式接收控件与回调。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `BuildLoaderOverview`、`LoadConfigurationVersions`、`PrepareLoaderConfiguration`、`QueueConfiguredLoader`。
- **构造依赖：** `Action<LoaderSelection> queue`、`Func<(IReadOnlyList<VersionRow> Rows, string? Selected)> getVersions`、`Func<System.Net.Http.HttpClient> getHttp`、`Window resources`。
- **源码：** [`LoaderOverviewController.cs`](../../../src/MCUGLauncher/Components/LoaderOverviewController.cs)。
