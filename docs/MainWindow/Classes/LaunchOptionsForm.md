# LaunchOptionsForm

根据 `LaunchSettings` 构建启动选项、分辨率、内存、Java 和高级参数表单，并将控件值读回设置对象。

- **主要入口：** 构造函数、`Panel`、`SetJava(path)`、`Read()`。
- **协作：** 由启动选项页面构建逻辑使用；具体保存和 Java 检测由调用者负责。
- **源码：** [`LaunchOptionsForm.cs`](../../../src/MCUGLauncher/MainWindow/Builders/LaunchOptionsForm.cs)。
