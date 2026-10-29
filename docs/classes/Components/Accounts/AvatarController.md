# AvatarController

头像与名称刷新，显式接收控件与状态回调。

- **类型：** `internal sealed class`，基类/实现：无。
- **主要方法：** `RefreshAvatar`、`RefreshAvatarClick`、`UpdateAvatarName`。
- **构造依赖：** `Func<bool> isBusy`、`Func<PlayerIdentityState> getState`、`Func<Task> refreshMicrosoftProfile`、`Image skinAvatar`、`TextBlock avatarCaption`、`TextBlock displayName`。
- **源码：** [`AvatarController.cs`](../../../../src/MCUGLauncher/Components/Accounts/AvatarController.cs)。
