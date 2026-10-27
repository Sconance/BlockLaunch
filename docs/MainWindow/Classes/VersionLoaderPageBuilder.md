# VersionLoaderPageBuilder

构造版本加载器修改页面，提供加载器版本查询、安装/重装和相关实例维护入口。

- **主要入口：** `Build()`。
- **协作：** 使用 `VersionSettingsPageContext` 取得当前版本信息与窗口；安装选择交给加载器控制器和操作队列。
- **源码：** [`VersionLoaderPageBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/VersionLoaderPageBuilder.cs)。
