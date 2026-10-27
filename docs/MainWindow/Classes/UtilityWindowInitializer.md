# UtilityWindowInitializer

统一辅助窗口与内嵌页的尺寸约束和加载时初始化，并在窗口加载后调用主题应用回调。

- **主要入口：** `Configure(...)` 重载。
- **协作：** 由 `MainWindow.ConfigureUtilityWindow` 转发，复用窗口主题器。
- **源码：** [`UtilityWindowInitializer.cs`](../../../src/MCUGLauncher/MainWindow/Initialization/UtilityWindowInitializer.cs)。
