# LaunchOptionsDialog

构造并保存全局或实例级启动选项；全局设置同时提供背景、主题色和离线皮肤配置。

- **主要入口：** `Show(descriptor)`。
- **协作：** 使用 `LaunchOptionsForm` 收集设置，经主窗口的停止状态检查和对话框入口完成保存；通过回调应用个性化并打开外观设置。
- **源码：** [`LaunchOptionsDialog.cs`](../../../src/MCUGLauncher/Presentation/LaunchOptionsDialog.cs)。
