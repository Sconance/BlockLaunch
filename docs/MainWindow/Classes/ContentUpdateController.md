# ContentUpdateController

协调已管理内容的更新检查、依赖解析、预览确认和下载入队，并提供最近一次内容操作的回退。

- **主要入口：** `Open(ResourceInstallTarget, ...)`、`CheckAll()`、`Restore(directory)`。
- **协作：** 直接注入 `ResourceManagementService`，通过 `IContentUpdateInteraction` 执行任务、获取目标快照和入队，通过 `IContentUpdateSelection` 选择并确认。控制器不引用 MainWindow、WPF 或具体视图。
- **窗口适配：** `MainWindow/UI/Content/UpdatesUI.cs` 实现交互接口；具体选择与预览仍由 Presentation 构建。
- **源码：** [`ContentUpdateController.cs`](../../../src/MCUGLauncher/Controllers/ContentUpdateController.cs)。
