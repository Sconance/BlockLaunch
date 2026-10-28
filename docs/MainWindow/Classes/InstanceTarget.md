# InstanceTarget

表示一次内容搜索/安装操作所需的当前游戏版本、Minecraft 版本和加载器三元组。

- **协作：** 由 `VersionManagementService.Target()` 创建，`MainWindow.Target()` 仅转发，供内容筛选和资源查询逻辑使用。
- **源码：** [`VersionManagementModels.cs`](../../../src/MCUGLauncher/Versions/VersionManagementModels.cs)。
