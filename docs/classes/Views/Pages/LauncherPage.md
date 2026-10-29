# LauncherPage

独立页面基类，提供动作边界与内容物化。PageHost 在物化时记录原内容宿主，供组合层直接绑定与导航，避免按主标签索引查找。

- **类型：** `public class`，基类/实现：`UserControl`。
- **主要方法：** `Materialize`、`RaisePageAction`。
- **源码注释：** A standalone markup view with an explicit action boundary.
- **源码：** [`LauncherPage.cs`](../../../../src/MCUGLauncher/Views/Pages/LauncherPage.cs)。
