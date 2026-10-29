# InstanceManagerBindings

内容管理与宿主的刷新绑定。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Action<Action?> SetContentRefresh`、`Action<string?> SetCurrentKey`、`Action<Action> SubscribeVersions`、`Action<Action> UnsubscribeVersions`。
- **源码：** [`SketchContentManagerDialog.cs`](../../../src/MCUGLauncher/Presentation/SketchContentManagerDialog.cs)。
