# VersionPackExportPageBuilder

构造 MultiMC / Prism 兼容整合包导出页面，可选择是否包含 Mod、服务器列表、光影、资源包和存档。

- **主要入口：** `Build()`。
- **协作：** 使用 `VersionSettingsPageContext` 提供的数据目录、版本需求和窗口；通过实例管理服务执行导出。
- **源码：** [`VersionPackExportPageBuilder.cs`](../../../src/MCUGLauncher/MainWindow/Builders/VersionPackExportPageBuilder.cs)。
