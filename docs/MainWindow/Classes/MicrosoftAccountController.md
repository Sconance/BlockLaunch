# MicrosoftAccountController

封装微软正版账号的本地凭据读取、交互式登录、会话刷新、账号管理和清除流程。

- **主要入口：** `Load()`、`Login(interactive)`、`EnsureLogin()`、`OpenManagement()`。
- **协作：** 通过 `MicrosoftAccountService` 完成认证；更新 `MainWindow` 的账号状态并复用窗口任务运行、配置保存和 UI 对话框入口。
- **安全边界：** 凭据存储和清除委托给账号服务；不把密码或令牌写入 UI 文档或日志。
- **源码：** [`MicrosoftAccountController.cs`](../../../src/MCUGLauncher/Controllers/MicrosoftAccountController.cs)。
