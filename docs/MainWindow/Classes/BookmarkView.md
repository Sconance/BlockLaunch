# BookmarkView

资源收藏条目的显示模型。保存一个 `ResourceBookmark`，并将其标题、收藏类型和标签格式化为列表项文本。

- **主要成员：** `ToString()`、`KindName(string)`。
- **协作：** 由 `CollectionsUI` 用于收藏列表显示与选择；不持有 UI 控件或执行存储操作。
- **源码：** [`BookmarkView.cs`](../../../src/MCUGLauncher/MainWindow/Models/BookmarkView.cs)。
