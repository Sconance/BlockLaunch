# ResourceFavoriteQuery

收藏查询的不可变输入，避免异步过程读取控件。

- **类型：** `internal sealed record`，基类/实现：无。
- **构造依赖：** `int Channel`、`string Kind`、`int Offset`、`string Query`、`int Sort`、`InstanceTarget Target`。
- **源码注释：** Immutable search input keeps asynchronous queries independent of WPF controls.
- **源码：** [`ResourceFavoriteSearch.cs`](../../../src/MCUGLauncher/Presentation/ResourceFavoriteSearch.cs)。
