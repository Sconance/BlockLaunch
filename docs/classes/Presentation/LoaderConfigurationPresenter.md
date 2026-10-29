# LoaderConfigurationPresenter

加载器目录浏览、取消与选择；安装交给应用回调。

- **类型：** `public sealed class`，基类/实现：`IDisposable`。
- **主要方法：** `Dispose`、`LoadVersions`、`Prepare`、`QueueSelection`。
- **构造依赖：** `Action<LoaderSelection> queue`、`Func<HttpClient> httpProvider`、`LoaderConfigurationView view`。
- **源码注释：** Owns loader catalogue browsing and cancellation; installation is an application callback.
- **源码：** [`LoaderConfigurationPresenter.cs`](../../../src/MCUGLauncher/Presentation/LoaderConfigurationPresenter.cs)。
