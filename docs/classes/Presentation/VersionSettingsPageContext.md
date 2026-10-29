# VersionSettingsPageContext

版本设置页的实例数据与重绘入口。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `InstanceCatalog Catalog`、`Func<string> DataDirectory`、`InstanceDescriptor Descriptor`、`string Key`、`Action<string> Render`、`PackRequirements Requirements`、`string Root`、`VersionSettingsView View`。
- **源码注释：** Per-dialog data and presentation lifetime, without a window or workspace service locator.
- **源码：** [`VersionSettingsPageContext.cs`](../../../src/MCUGLauncher/Presentation/VersionSettingsPageContext.cs)。
