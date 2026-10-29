# ResourceFavoriteActions

收藏操作与存储访问的显式回调。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `Func<ResourceBookmarkStore?> Bookmarks`、`Func<string> ContentKind`、`Func<System.Net.Http.HttpClient> Http`、`Action<string> Log`、`SelectionChangedEventHandler ProjectChanged`、`Action RefreshDecoration`、`Func<int> ReleaseChannel`、`Action ResetSearchOffset`。
- **源码注释：** Only resource-favorite operations cross this component boundary.
- **源码：** [`ResourceFavoritesController.cs`](../../../src/MCUGLauncher/Components/ResourceFavoritesController.cs)。
