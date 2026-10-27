# JavaManagerController

构建 Java 管理窗口并协调候选运行时扫描、过滤、验证和选择回传。

- **主要入口：** `Open(root, profile, select, owner)`。
- **协作：** 使用 Java 发现/兼容性服务；调用者通过回调接收确认的 Java 路径。
- **源码：** [`JavaManagerController.cs`](../../../src/MCUGLauncher/MainWindow/Controllers/JavaManagerController.cs)。
