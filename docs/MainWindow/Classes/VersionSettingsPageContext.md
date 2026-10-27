# VersionSettingsPageContext

为版本设置页构建器共享当前实例、目录、需求信息和窗口控件，并提供通用卡片、字段、按钮及身份保存操作。

- **主要成员：** `DataDirectory()`、`VersionCard(...)`、`Button(...)`、`Field(...)`、`Card(...)`、`SaveIdentity()`。
- **协作：** 由 `VersionSettingsDialog` 创建并交给各版本设置页面构建器；`Render` 委托用于请求重绘。
- **源码：** [`VersionSettingsPageContext.cs`](../../../src/MCUGLauncher/MainWindow/Presentation/VersionSettingsPageContext.cs)。
