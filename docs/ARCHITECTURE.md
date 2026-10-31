# 架构说明

MCUGLauncher 是 WPF（.NET 8）+ WebView2 的桌面启动器：C# 负责窗口、服务与消息桥接，界面由 `src/MCUGLauncher/Ui` 下的 HTML/CSS/JS 渲染。

## 分层

```text
Ui/ (HTML/CSS/JS)
  │  host.js: postMessage({type,...})
  ▼
MainWindow (WPF 窗口 + WebView2 宿主 + 消息分发)
  │
  ├─ Ui/WindowChromeController   无边框窗口行为（拖动、DWM 圆角、窗口命令）
  ├─ Services/                   配置、版本目录、Mod 下载、服务器 ping、社区
  ├─ Authentication/             微软 / MUA 登录与令牌存储
  └─ MCUGLauncher.Core           BlockLaunch 命名空间：启动、实例、下载、安装、卸载
```

## 应用层模块

| 模块 | 位置 | 职责 |
| --- | --- | --- |
| `MainWindow` | `MainWindow.xaml.cs` | 窗口宿主、WebView2 初始化、消息分发、业务编排 |
| `WindowChromeController` | `Ui/WindowChromeController.cs` | DWM 圆角、拖动、最小化/最大化/关闭、窗口复位 |
| `ConfigStore` | `Services/ConfigStore.cs` | `%LOCALAPPDATA%\MCUGLauncher\config.json` 读写与旧配置导入 |
| `VersionCatalog` | `Services/VersionCatalog.cs` | 版本目录扫描、实例列表与选中项 |
| `ModService` | `Services/ModService.cs` | Modrinth 项目下载到实例 |
| `ServerPingService` | `Services/ServerPingService.cs` | SLP 服务器状态查询 |
| `CommunityService` | `Services/CommunityService.cs` | 社区 JSON 读取与连接测试 |
| `MicrosoftAccountService` / `MuaAccountService` | `Authentication/` | 两种账号体系的登录、会话与令牌存储 |

## 消息协议

`Ui/host.js` 通过 `window.chrome.webview.postMessage` 发送 `{ type, ... }`，`MainWindow.OnWebMessage` 用 `switch` 分发到具体方法，处理结果通过 `PostWebMessageAsJson` 回推（字段名 camelCase）。

## 重构进度

`MainWindow` 原先约 1534 行，按模块逐步抽离；每抽离一个模块同步更新本文件并单独提交。

| 提交 | 抽离模块 | MainWindow 行数 |
| --- | --- | --- |
| 本次 | `WindowChromeController`（窗口行为、P/Invoke、拖动定时器） | 1460 |
