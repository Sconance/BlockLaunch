# VersionModManagementPageBuilder

构造当前实例的 Mod 管理页面，提供搜索、扫描、刷新、启用/禁用、移除、更新、回退和导入操作。

- **主要入口：** `Build()`；页面内 `RefreshMods()` 刷新当前实例列表。
- **协作：** 使用 `VersionSettingsPageContext` 获取实例目录、加载器需求和窗口；内容变更通过现有服务/窗口操作入口执行。
- **源码：** [`VersionModManagementPageBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/VersionModManagementPageBuilder.cs)。
