# RunningGamesController

运行会话的页内列表、按会话关闭与日志/诊断观察。

- **类型：** `internal sealed class`。
- **主要方法：** ShowRunningGames、RefreshRunningGames、ObserveGame。
- **构造依赖：** 运行入口按钮、窗口关闭状态、日志/页内展示/诊断回调；不接收整个 LauncherWorkspace。
- **源码：** [`RunningGamesController.cs`](../../../../src/MCUGLauncher/Components/Launch/RunningGamesController.cs)。
