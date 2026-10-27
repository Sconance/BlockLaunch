# VersionSettingsDialog

协调单个实例的版本设置窗口生命周期、当前实例上下文、页面切换和页面构建器分发。

- **主要入口：** `Show()`。
- **协作：** 创建 `VersionSettingsWindowView` 与 `VersionSettingsPageContext`，按所选页调用各 `Version*PageBuilder`。
- **生命周期：** 防止同一窗口重复打开；关闭后清除窗口引用并刷新实例列表。
- **源码：** [`VersionSettingsDialog.cs`](../../../src/MCUGLauncher/MainWindow/Presentation/VersionSettingsDialog.cs)。
