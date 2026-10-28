# ContentUpdateSelectionView

构建已管理内容更新选择窗口，按可选项目过滤更新项，并提供全选、全不选和预览操作。

- **主要入口：** 构造函数接收候选更新和预览回调；`Show()`/`Close()` 管理选择窗口。
- **协作：** 实现 `IContentUpdateSelection`，通过回调传递所选版本；`ConfirmPreview()` 在选择页下显示预览确认。控制器不再读取具体 Dialog 或创建 WPF 视图。
- **源码：** [`ContentUpdateSelectionView.cs`](../../../src/MCUGLauncher/Presentation/ContentUpdateSelectionView.cs)。
