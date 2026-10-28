# ContentUpdatePreviewView

构建更新确认窗口，展示更新项数、预估文件大小、保留禁用状态等摘要及变更清单。

- **主要入口：** 构造函数接收 `ContentUpdatePreview`；`Show()` 返回用户是否确认。
- **协作：** 被 `ContentUpdateController` 创建；确认只返回结果，入队由控制器负责。
- **源码：** [`ContentUpdatePreviewView.cs`](../../../src/MCUGLauncher/Presentation/ContentUpdatePreviewView.cs)。
