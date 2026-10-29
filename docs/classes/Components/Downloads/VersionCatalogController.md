# VersionCatalogController

按需读取在线版本，合并并发请求、限制等待时间并拒绝过期目录响应。

- **类型：** `internal sealed class`。
- **主要方法：** EnsureVersionCatalog、Dispose。
- **构造依赖：** 根目录读取、版本获取/应用、状态刷新与日志回调；可选超时；不接收整个 LauncherWorkspace。
- **源码：** [`VersionCatalogController.cs`](../../../../src/MCUGLauncher/Components/Downloads/VersionCatalogController.cs)。
