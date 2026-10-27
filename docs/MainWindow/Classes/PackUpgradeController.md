# PackUpgradeController

协调整合包升级预览、冲突确认、升级副本创建和返回原实例操作。

- **主要入口：** `Open(...)`。
- **协作：** 使用 Core 升级规划/迁移服务，并通过传入的回调选择目标实例和返回主页。
- **源码：** [`PackUpgradeController.cs`](../../../src/MCUGLauncher/MainWindow/Controllers/PackUpgradeController.cs)。
