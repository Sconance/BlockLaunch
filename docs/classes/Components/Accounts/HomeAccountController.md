# HomeAccountController

首页账号面板的构建与状态更新。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `BuildHomeAccountPanel`、`UpdateAccountPanel`。
- **构造依赖：** `AccountPanelActions actions`、`AccountPanelControls controls`、`Action<Border> configureWelcome`、`Func<(bool Microsoft, bool Mua)> getAccountState`。
- **源码：** [`HomeAccountController.cs`](../../../../src/MCUGLauncher/Components/Accounts/HomeAccountController.cs)。
