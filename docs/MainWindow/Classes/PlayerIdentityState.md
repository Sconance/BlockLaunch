# PlayerIdentityState

`PlayerIdentityController` 刷新角色名称和皮肤头像时读取的一次状态快照。包括账号模式、Microsoft/MUA 账号信息、玩家名称、离线皮肤路径、配置目录和 HTTP 客户端。

- **协作：** 由 `MainWindow.GetPlayerIdentityState()` 提供；控制器每次刷新时通过委托读取最新状态。
- **源码：** [`PlayerIdentityState.cs`](../../../src/MCUGLauncher/MainWindow/Models/PlayerIdentityState.cs)。
