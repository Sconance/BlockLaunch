# ContentUpdateSelectionView

构建已管理内容更新选择窗口，按可选项目过滤更新项，并提供全选、全不选和预览操作。

- **主要入口：** 构造函数接收候选更新和预览回调；`Show()`/`Close()` 管理选择窗口。
- **协作：** 与 `ContentUpdateController` 协作；`Dialog` 属性供预览确认窗口设置为其子窗口。
- **源码：** [`ContentUpdateSelectionView.cs`](../../../src/MCUGLauncher/MainWindow/Presentation/ContentUpdateSelectionView.cs)。
