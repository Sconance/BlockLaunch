# ContentUpdateSelectionView

内容更新选择视图。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `Close`、`Show`。
- **构造依赖：** `Func<bool> isBusy`、`Func<IReadOnlyList<ContentVersion>, Task> preview`、`Func<string, RoutedEventHandler, Button> createButton`、`Func<string, StackPanel, InlineDialog> createDialog`、`IEnumerable<ContentUpdate> updates`、`object owner`、`string? project`。
- **源码：** [`ContentUpdateSelectionView.cs`](../../../../src/MCUGLauncher/Views/Updates/ContentUpdateSelectionView.cs)。
