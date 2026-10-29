# LauncherMenuPage

菜单页面定义：ID、标题与内容工厂。

- **类型：** `public sealed record`，基类/实现：无。
- **构造依赖：** `Func<LauncherMenuContext, FrameworkElement> CreateContent`、`string Id`、`string Title`。
- **源码注释：** A real page factory, not a placeholder button. Each host receives its own content instance.
- **源码：** [`LauncherMenuRegistry.cs`](../../../src/MCUGLauncher/Navigation/LauncherMenuRegistry.cs)。
