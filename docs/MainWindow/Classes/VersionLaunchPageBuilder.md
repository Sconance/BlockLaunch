# VersionLaunchPageBuilder

构造单个实例的启动设置页面，包括启动选项、内存及 Java 配置，并连接检测、选择和保存操作。

- **主要入口：** `Build()`。
- **协作：** 使用 `VersionSettingsPageContext` 访问当前实例和窗口，使用 `LaunchOptionsForm` 构建设置表单。
- **源码：** [`VersionLaunchPageBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/VersionLaunchPageBuilder.cs)。
