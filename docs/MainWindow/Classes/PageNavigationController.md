# PageNavigationController

根据主导航项切换标签页、更新标题和副标题，并处理紧凑壳层下内容中心与版本管理页的特殊路由。

- **主要入口：** `Navigate(sender, e)`。
- **协作：** 通过传入的委托刷新内容、任务和下载分类；不直接构建功能页面。
- **源码：** [`PageNavigationController.cs`](../../../src/MCUGLauncher/Controllers/PageNavigationController.cs)。
