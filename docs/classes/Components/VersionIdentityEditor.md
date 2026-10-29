# VersionIdentityEditor

实例名称、描述等身份信息的编辑与保存。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `Edit`、`Save`。
- **构造依赖：** `VersionSettingsPageContext context`、`Func<string, StackPanel, InlineDialog> createDialog`、`Func<bool> isBusy`、`Action requireStopped`。
- **源码注释：** Validates and persists instance identity; page rendering is an injected callback.
- **源码：** [`VersionIdentityEditor.cs`](../../../src/MCUGLauncher/Components/VersionIdentityEditor.cs)。
