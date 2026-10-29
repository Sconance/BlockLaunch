# EmbeddedMicrosoftOAuthUI

在应用内 WebView2 中承载微软 OAuth 授权页，只替换浏览器表面。

- **类型：** `public sealed class`，基类/实现：`IWebUI`。
- **主要方法：** `DisplayDialogAndInterceptUri`、`DisplayDialogAndNavigateUri`、`GetAuthCodeResult`、`IsMicrosoftAuthorization`。
- **构造依赖：** `MainWindow host`、`string profileDirectory`。
- **源码注释：** Uses the library's OAuth URL checker and token exchange; only the browser surface changes.
- **源码：** [`EmbeddedMicrosoftOAuthUI.cs`](../../../src/MCUGLauncher/Authentication/EmbeddedMicrosoftOAuthUI.cs)。
