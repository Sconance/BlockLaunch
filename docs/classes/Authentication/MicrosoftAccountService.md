# MicrosoftAccountService

微软账号的登录、会话刷新、保存与退出。

- **类型：** `public sealed class`，基类/实现：无。
- **主要方法：** `Authenticate`、`Forget`、`ReadSavedAccount`、`Translate`、`ValidateSession`。
- **构造依赖：** `HttpClient client`、`string directory`、`XboxAuthNet.OAuth.CodeFlow.IWebUI? browser`。
- **源码：** [`MicrosoftAccountService.cs`](../../../src/MCUGLauncher/Authentication/MicrosoftAccountService.cs)。
