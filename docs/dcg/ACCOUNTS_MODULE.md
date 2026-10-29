# 账号与认证模块

当前账号与认证业务集中在与 `MainWindow` 同级的 `src/MCUGLauncher/Accounts/`，由
`MCUGLauncher.Application` 编译。该目录是共享业务模块，不属于旧前端；旧前端和
`Front` 都可以通过 Application 调用它。

## 目录职责

| 文件 | 职责 |
|---|---|
| `AccountSessionService.cs`、`AccountSessionModels.cs` | 统一账号会话、账号模式和会话结果 |
| `MicrosoftAccountService.cs` | 微软账号授权、刷新和账号读取 |
| `MicrosoftAuthorizationPolicy.cs` | 微软授权地址白名单校验 |
| `MinecraftOwnershipGuard.cs` | Minecraft 所有权检查 |
| `MuaAccountService.cs` | MUA 登录、角色及会话处理 |
| `ProtectedAccountStorage.cs` | Windows 受保护凭据的读写，保留原路径和加密格式 |
| `IAccountProviders.cs`、`AccountExceptions.cs` | 外部认证提供者契约和账号异常 |

旧授权窗口和头像渲染不属于共享业务，已放在
`src/MCUGLauncher/MainWindow/UI/Accounts/`。其中 `EmbeddedMicrosoftOAuthUI` 只负责
旧 WPF 授权 UI，并复用共享的 `MicrosoftAuthorizationPolicy`。

## 依赖边界

```text
旧前端 ─┐
        ├─> MCUGLauncher.Application ─> Accounts/ ─> Core
Front ──┘

旧前端 ─> MainWindow/UI/Accounts/（旧 WPF 授权与头像界面）
Front ──> 自己的 Views/ViewModels/（新前端界面）
```

`Accounts/` 不引用 `MainWindow`、旧控制器、Front 或 SharedUI，因此删除任一前端不会
影响另一前端编译。会话编排通过接口注入账号提供者，微软服务接收浏览器接口；真实 DPAPI
凭据读写及 WebView2/Windows 界面仍需 Windows 环境，Linux 端只做编译和离线策略测试。

## 已验证内容

从仓库根目录执行：

```bash
DOTNET_CLI_HOME=/tmp/mcug-dotnet-home \
NUGET_PACKAGES=/tmp/mcug-nuget \
/tmp/mcug-dotnet-sdk/dotnet run --project test/AccountChecks/AccountChecks.csproj -c Release

DOTNET_CLI_HOME=/tmp/mcug-dotnet-home \
NUGET_PACKAGES=/tmp/mcug-nuget \
/tmp/mcug-dotnet-sdk/dotnet build test/AuthChecks/AuthChecks.csproj -c Release -p:EnableWindowsTargeting=true --nologo

DOTNET_CLI_HOME=/tmp/mcug-dotnet-home \
NUGET_PACKAGES=/tmp/mcug-nuget \
python3 scripts/check-frontend-boundaries.py --build --dotnet /tmp/mcug-dotnet-sdk/dotnet
```

`AccountChecks` 链接共享账号会话与策略源码，以假的提供者验证离线名称、账号读取、
刷新转交互、取消和移除委托，不访问真实账号；Application 的实际引用由编译检查覆盖。
Windows 下使用 SDK 8.0.426，可直接运行上述 dotnet 命令（无需临时 SDK 路径和环境变量）。
此外运行 `dotnet build test/MuaChecks/MuaChecks.csproj -c Release` 检查 MUA 引用。

2026-10-29 开发端结果：账号策略检查通过，`AuthChecks` 和 `MuaChecks` 编译通过，Front 和旧前端在移除另一前端的
临时副本中均编译通过。Windows 原生登录窗口、DPAPI 实际凭据和游戏启动仍需在
Windows 设备上验收。
