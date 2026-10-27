# WindowChromeBuilder

为主窗口配置原生无边框 WindowChrome、标题栏拖动区域、最小化/最大化/关闭按钮及最大化状态图标。

- **主要入口：** `Build()`。
- **协作：** 在窗口初始化期间接收 `MainWindow`，接线窗口状态和关闭事件；不负责页面导航。
- **源码：** [`WindowChromeBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/WindowChromeBuilder.cs)。
