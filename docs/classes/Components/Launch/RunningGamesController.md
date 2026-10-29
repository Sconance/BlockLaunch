# RunningGamesController

运行会话的状态与命令协调，通过 IRunningGamesView 展示，不引用 WPF。

- **类型：** `internal sealed class`。
- **主要方法：** ShowRunningGames、RefreshRunningGames、ObserveGame。
- **构造依赖：** RunningGameRegistry、IRunningGamesView、RunningGameActions；不接收整个 LauncherWorkspace。
- **源码：** [`RunningGamesController.cs`](../../../../src/MCUGLauncher/Components/Launch/RunningGamesController.cs)。
