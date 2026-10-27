# InstancePackExportDialog

为当前实例构造 MultiMC / Prism 兼容整合包导出选项，并在用户确认后执行受操作协调器保护的导出任务。

- **主要入口：** `Show(key)`。
- **协作：** 读取实例描述及版本需求，复用主窗口的内嵌对话框、停止状态检查、操作队列和日志入口。
- **检查：** `InlineUiChecks` 验证五类内容选项和明确的导出操作入口。
- **源码：** [`InstancePackExportDialog.cs`](../../../src/MCUGLauncher/MainWindow/Presentation/InstancePackExportDialog.cs)。
