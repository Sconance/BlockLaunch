# NetworkSettingsController

展示网络设置界面，读取并校验代理与游戏下载源设置，保存用户选择。

- **主要入口：** `Open()`，由网络设置导航事件调用。
- **协作：** 使用 `MainWindow` 的偏好设置、保存入口和网络策略配置；更改按现有行为在重启后生效。
- **源码：** [`NetworkSettingsController.cs`](../../../src/MCUGLauncher/Controllers/NetworkSettingsController.cs)。
