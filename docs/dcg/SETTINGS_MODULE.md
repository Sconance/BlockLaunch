# 设置模块

设置业务现在位于与 `MainWindow` 同级的 `src/MCUGLauncher/Settings/`，由两个前端
共同使用。这里保存配置模型、配置文件读写、网络策略和设置快照校验；不保存任何
WPF 页面或 Front 页面。

## 当前文件

| 文件 | 职责 |
|---|---|
| `Preferences.cs` | 配置模型、`SettingsStore`、版本迁移、损坏配置备份和原子保存 |
| `NetworkPolicy.cs` | 网络配置校验、代理、镜像切换、下载限速和 HTTP 策略 |
| `LauncherSettingsService.cs` | 设置快照校验、目录规范化、Java 路径检查和安全保存 |

`Settings/` 的源码由 `MCUGLauncher.Core` 链接编译基础类型，由
`MCUGLauncher.Application` 链接编译设置服务。这样 Core 不需要反向引用前端项目，
两个前端仍使用相同的设置模型和保存规则。

## 前端边界

```text
MainWindow ─┐
            ├─> Settings/ ─> Core 基础能力
Front ──────┘
```

以下代码仍属于界面适配，不是共享设置业务：

- 旧 UI：`MainWindow/UI/Settings/PreferencesUI.cs`
- 旧 UI：`Controllers/NetworkSettingsController.cs`
- 新 UI：`Front/Views/SettingsView.xaml`、`SettingsViewModel.cs`

它们只能读取输入、调用共享服务和显示结果；设置规则不能在两个前端重复实现。

## 验证结果

- `python3 scripts/check-layout.py`：通过。
- `python3 scripts/check-frontend-boundaries.py`：通过。
- Core 和 Application Release 编译：通过。
- Front 移除旧前端后的隔离编译：通过。
- 旧前端移除 Front 后的隔离编译：通过。
- `CompatibilityChecks --business-services`：通过。
- `CompatibilityChecks --front-settings`：通过。

Windows 原生设置窗口、代理实际请求、不同 DPI 显示和完整游戏运行流程仍需在
Windows 设备上验收。
