# GameWindowBehaviorController

在游戏启动期间应用进程优先级、工作集、游戏标题和启动器可见性设置，并在游戏退出后恢复隐藏的启动器窗口。

- **主要入口：** `Apply(process, settings, session)`。
- **协作：** 由游戏启动 UI 调用；使用 `GameSession` 跟踪游戏进程完成状态，通过窗口 Dispatcher 安全更新可见性，并通过日志回调报告失败。
- **检查：** `VersionUiChecks` 使用短时子进程验证隐藏及退出后恢复行为。
- **源码：** [`GameWindowBehaviorController.cs`](../../../src/MCUGLauncher/Controllers/GameWindowBehaviorController.cs)。
