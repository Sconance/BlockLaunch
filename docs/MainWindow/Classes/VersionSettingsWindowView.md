# VersionSettingsWindowView

构建版本设置窗口的纯视图结构：标题栏、侧边导航、滚动页面容器和实例信息头部。

- **主要入口：** 构造函数接收页面选择/关闭回调；`SelectPage(...)` 设置初始页；`BeginPage()` 清理内容并添加头部。
- **协作：** 页面选择委托给 `VersionSettingsDialog`；可附加 `VersionSettings` 菜单扩展页；视图不执行实例保存或安装业务。
- **源码：** [`VersionSettingsWindowView.cs`](../../../src/MCUGLauncher/Presentation/VersionSettingsWindowView.cs)。
