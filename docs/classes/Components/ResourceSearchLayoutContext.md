# ResourceSearchLayoutContext

搜索布局装配所需的控件与动作上下文。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `ResourceFavoritesController Favorites`、`ResourceFiltersController Filters`、`Window Owner`、`TextBox Query`、`Action ResetSearchOffset`、`ListBox Results`、`Func<Task> Search`、`Action<FrameworkElement> SetFilterPanel`。
- **源码：** [`ResourceSearchLayoutController.cs`](../../../src/MCUGLauncher/Components/ResourceSearchLayoutController.cs)。
