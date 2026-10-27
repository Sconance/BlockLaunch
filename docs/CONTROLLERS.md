# 窗口控制器索引

`src/MCUGLauncher/MainWindow/Controllers/` 中的控制器负责一项边界明确的窗口交互或展示职责。`MainWindow` 在构造期间创建控制器并提供所需控件、配置或服务；XAML 事件和仍未迁移的 `partial MainWindow` 代码可能通过窗口转发方法调用控制器。

当前拆分仍在进行中：这些控制器并不代表整个窗口已经脱离 `MainWindow`，部分控制器仍显式接收窗口实例。新增或迁移功能时，应继续收窄依赖，并保持既有事件和测试入口的行为。

## 控制器

| 类 | 文件 | 职责与入口 |
| --- | --- | --- |
| `AvatarController` | [`AvatarController.cs`](../src/MCUGLauncher/MainWindow/Controllers/AvatarController.cs) | 更新当前角色名称和头像；读取离线皮肤、微软皮肤或 MUA 皮肤，管理缓存、请求取消与过期响应。由账号状态变化、头像刷新按钮和主页头像点击触发。 |
| `FileDropController` | [`FileDropController.cs`](../src/MCUGLauncher/MainWindow/Controllers/FileDropController.cs) | 显示拖放反馈层；确认整合包导入名称，并根据当前实例管理页选择导入目录。窗口的拖放事件仍由主窗口及文件导入逻辑处理。 |
| `InstanceSearchController` | [`InstanceSearchController.cs`](../src/MCUGLauncher/MainWindow/Controllers/InstanceSearchController.cs) | 按名称、实例键、版本或加载器详情筛选主页实例列表，并更新匹配数量。实例源列表刷新后也会重新应用搜索条件。 |
| `JavaManagerController` | [`JavaManagerController.cs`](../src/MCUGLauncher/MainWindow/Controllers/JavaManagerController.cs) | 构建 Java 管理页面；扫描常见位置、指定目录或固定磁盘，过滤兼容版本，校验并保存手动路径，将选择结果回传调用者。 |
| `LauncherTooltipsController` | [`LauncherTooltipsController.cs`](../src/MCUGLauncher/MainWindow/Controllers/LauncherTooltipsController.cs) | 为主页、下载导航以及资源/游戏版本搜索框设置提示文本。 |
| `LoaderInstallationController` | [`LoaderInstallationController.cs`](../src/MCUGLauncher/MainWindow/Controllers/LoaderInstallationController.cs) | 查询 Forge 或 NeoForge 可用版本，让用户选择版本，并通过现有操作队列提交安装。 |
| `NetworkSettingsController` | [`NetworkSettingsController.cs`](../src/MCUGLauncher/MainWindow/Controllers/NetworkSettingsController.cs) | 展示代理和游戏下载源设置，校验后保存配置；设置在重启后生效。 |
| `WindowChromeController` | [`WindowChromeController.cs`](../src/MCUGLauncher/MainWindow/Controllers/WindowChromeController.cs) | 配置无边框窗口的原生 WindowChrome、可拖动/可交互区域、最小化/最大化/关闭按钮及最大化图标状态。 |

## 组合与依赖

- 控制器由 [`MainWindow.xaml.cs`](../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs) 组合。`OpenJavaManager`、`ConfirmPackImport`、`DropTargetDirectory` 等转发方法用于维持尚未迁移代码和现有测试的调用边界。
- `AvatarController` 接收头像相关控件以及 `AvatarState` 提供函数；`AvatarState` 是一次刷新所需的账号、皮肤路径、配置目录和 HTTP 客户端快照。
- `InstanceSearchController` 与 `LauncherTooltipsController` 直接接收它们操作的控件。
- `FileDropController`、`JavaManagerController`、`LoaderInstallationController`、`NetworkSettingsController` 和 `WindowChromeController` 当前接收 `MainWindow`，因为其工作还依赖窗口服务、状态或对话框。后续独立化时应优先将所需能力收敛为明确参数，而不是扩展窗口的公开成员。
- 长时间任务仍使用窗口的 `Run` / 下载队列入口，避免绕开现有互斥、取消和状态更新逻辑。

## 职责命名说明

控制器名描述其当前主要职责，而非 `MainWindow` partial 文件名。`FileDropController` 除反馈提示外还处理整合包确认和目标目录；`LoaderInstallationController` 负责加载器安装选择与排队；`LauncherTooltipsController` 只设置提示文本，并不负责键盘焦点导航。若职责进一步扩大，应拆出独立边界并同步更新本索引。
