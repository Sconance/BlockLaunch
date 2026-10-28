# InstanceLibraryDialog

为固定的游戏实例构建资料库对话框，展示存档列表与版本信息、存档备份/导入，以及截图浏览、预览和另存操作。

- **主要入口：** `Show()`。
- **协作：** 通过 `MainWindow` 获取目标实例、停止状态、操作队列和目录打开入口；存档操作交给 `WorldArchives`。
- **行为边界：** 导入存档会新建存档目录，不覆盖已有存档。
- **源码：** [`InstanceLibraryDialog.cs`](../../../src/MCUGLauncher/Presentation/InstanceLibraryDialog.cs)。
