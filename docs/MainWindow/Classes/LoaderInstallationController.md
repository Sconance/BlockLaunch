# LoaderInstallationController

查询 Forge 或 NeoForge 可用加载器版本，展示选择界面，并将选中的安装任务提交到现有操作/下载队列。

- **主要入口：** `AutomaticLoaderClick` 及版本选择处理。
- **协作：** 依赖 `MainWindow` 的实例、操作状态和队列入口；加载器安装由安装服务执行。
- **源码：** [`LoaderInstallationController.cs`](../../../src/MCUGLauncher/MainWindow/Controllers/LoaderInstallationController.cs)。
