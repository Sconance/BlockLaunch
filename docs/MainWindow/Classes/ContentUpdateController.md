# ContentUpdateController

协调已管理内容的更新检查、依赖解析、预览确认和下载入队，并提供最近一次内容操作的回退。

- **主要入口：** `Open(...)`、`CheckAll()`、`Restore(directory)`。
- **协作：** 经 `MainWindow.Run` 调用 `ContentUpdates`/`ContentService`；选择和确认 UI 由 `ContentUpdateSelectionView`、`ContentUpdatePreviewView` 构建。
- **源码：** [`ContentUpdateController.cs`](../../../src/MCUGLauncher/MainWindow/Controllers/ContentUpdateController.cs)。
