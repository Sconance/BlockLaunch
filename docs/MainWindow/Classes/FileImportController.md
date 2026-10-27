# FileImportController

负责拖放导入界面的反馈状态、整合包导入确认和导入目标目录选择。

- **主要入口：** `Build()`、`ConfirmPackImport(...)`、`DropTargetDirectory()`。
- **协作：** 接收 `MainWindow` 与拖放/实例选择状态；文件分类及实际导入仍由窗口和 Core 导入服务执行。
- **源码：** [`FileImportController.cs`](../../../src/MCUGLauncher/MainWindow/Controllers/FileImportController.cs)。
