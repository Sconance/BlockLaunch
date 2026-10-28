# InstanceUninstallationController

展示实例删除预览、确认删除范围，取消相关待处理任务并执行实例及共享数据清理，随后刷新版本列表。

- **主要入口：** `Open(...)`。
- **协作：** 使用 `MainWindow` 的操作协调器、确认界面、实例目录服务和刷新入口。
- **安全边界：** 删除前明确显示受影响目录，并遵循 Core 实例管理的删除规则。
- **源码：** [`InstanceUninstallationController.cs`](../../../src/MCUGLauncher/Controllers/InstanceUninstallationController.cs)。
