# PlayerIdentityController

更新当前玩家名称和头像，支持离线本地皮肤、微软皮肤和 MUA 皮肤；管理网络请求取消、缓存及过期响应。

- **主要入口：** `UpdateName()`、`Refresh(bypassCache)`、`Cancel()`。
- **协作：** 从 `PlayerIdentityState` 快照读取账号与路径，通过头像渲染工具更新 WPF 控件。
- **源码：** [`PlayerIdentityController.cs`](../../../src/MCUGLauncher/MainWindow/Controllers/PlayerIdentityController.cs)。
