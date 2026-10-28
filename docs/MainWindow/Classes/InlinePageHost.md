# InlinePageHost

管理主窗口中的内嵌页面栈，包括显示/隐藏底层页、返回焦点、标题栏适配、Esc/返回操作及页面关闭。

- **主要入口：** `Build()`、`Push(page)`、`Pop(page)`、`Activate(page)`、`RefreshTitle(page)`。
- **协作：** 接收 `MainWindow`、内嵌页面列表和导航控件；窗口关闭时按栈逆序强制关闭页面。
- **源码：** [`InlinePageHost.cs`](../../../src/MCUGLauncher/Presentation/InlinePageHost.cs)。
