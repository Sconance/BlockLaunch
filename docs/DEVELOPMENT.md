# 开发导览

MCUG Launcher 是 .NET 8 / WPF 应用。先从本文件定位功能，再阅读对应入口。
构建、打包和运行检查见 [BUILD.md](BUILD.md)。

## 先读哪些文件

1. `src/MCUGLauncher/MainWindow/MainWindow.xaml.cs`：窗口初始化、服务组装与主操作。
2. `src/MCUGLauncher/MainWindow.xaml`、`App.xaml`：命名控件和公共样式。
3. `src/MCUGLauncher/MainWindow/UI/` 对应区域的 `*UI.cs`：页面构建、事件和状态展示。
4. `src/MCUGLauncher.Core/`：实际业务服务；UI 调用这些服务。
5. `test/` 对应检查程序：合成输入、预期行为和失败边界。

窗口功能由 `partial MainWindow` 与独立组件共同实现，代码位于
`src/MCUGLauncher/MainWindow/`；窗口 partial 按功能放在 `UI/`，交互控制器放在 `Controllers/`，构建器放在 `Builders/`，展示模型放在 `Models/`，生命周期初始化放在 `Initialization/`，页面组合放在 `Presentation/`，视觉样式放在 `Styling/`。
新组件通过构造参数接收窗口或控件依赖。
`MainWindow` 仍负责 WPF/XAML 事件入口与控制器组装；尚未迁移的功能仍共享窗口状态，**不是互相独立的组件**。
初始化顺序集中在窗口构造函数中；调整父子控件关系前，应检查后续布局方法是否仍会读取它们。
本次保留了这个兼容边界，没有为了整洁重写启动、认证或实例存储。

## 功能定位

| 想修改的内容 | 界面入口 | 服务或数据 |
| --- | --- | --- |
| 账号面板、登录方式与头像 | `UI/Accounts/HomeAccountUI.cs`、`Controllers/MicrosoftAccountController.cs`、`Controllers/PlayerIdentityController.cs` | `Authentication/` |
| 主导航、窗口拖动、大小适配 | `UI/Shell/ModernUI.cs`、`UI/Shell/CompactShellUI.cs`、`Controllers/PageNavigationController.cs`、`Builders/WindowChromeBuilder.cs`、`Initialization/LauncherTooltipInitializer.cs`、`Styling/ModernThemeStyler.cs`、`Styling/WindowThemeStyler.cs` | 窗口自身 |
| 内嵌管理页栈与呈现 | `Presentation/InlinePageHost.cs` | `InlineDialog` 页面及窗口标题栏 |
| 主页版本选择、版本搜索与内容管理 | `UI/Instances/SketchUI.cs`、`UI/Instances/VersionSettingsUI.cs`、`Controllers/InstanceSearchController.cs`、`Controllers/InstanceUninstallationController.cs` | Core `Domain/Instances.cs`、`Application/InstanceManagement.cs` |
| 版本和资源下载 | `UI/Downloads/DownloadPageUI.cs`、`UI/Content/ContentUI.cs`、`Builders/ContentSearchFilterBuilder.cs`、`Controllers/LoaderInstallationController.cs`、`Controllers/ContentUpdateController.cs` | Core `GameInstaller.cs`、`ContentService.cs` |
| 下载队列与任务操作 | `UI/Downloads/DownloadQueueUI.cs`、`UI/Downloads/DownloadTaskUI.cs`、`UI/Downloads/MiniDownloadUI.cs` | Core `Application/OperationCoordinator.cs` |
| 文件拖入、资源导入 | `Controllers/FileImportController.cs`、`UI/Instances/InstanceLibraryUI.cs`、`Presentation/InstanceLibraryDialog.cs` | Core `Application/LocalImportClassifier.cs`、`WorldArchives.cs` |
| 整合包导入、导出、升级 | `UI/Packs/PackUI.cs`、`Controllers/PackUpgradeController.cs` | Core `PackService.cs`、`Application/PackUpgrade.cs` |
| Java 与游戏启动 | `Controllers/JavaManagerController.cs`、`MainWindow.xaml.cs` | Core `Application/Java*.cs`、`LaunchService.cs` |
| 主题、背景 | `UI/Experience/AppearanceUI.cs`、`Styling/WindowThemeStyler.cs`、`Initialization/UtilityWindowInitializer.cs`、`App.xaml` | Core `Domain/Preferences.cs` |
| 下载与网络设置 | `Controllers/NetworkSettingsController.cs` | `NetworkPolicyHandler`、Core 网络策略 |
| 崩溃分析与反馈 | `UI/Support/DiagnosticsUI.cs` | Core `Application/Diagnostics.cs` |

所有 MainWindow 类/记录类型的逐类说明和源码链接见 [组件文档索引](MainWindow/README.md)。

表中的 Core 路径相对于 `src/MCUGLauncher.Core/`；界面代码路径相对于
`src/MCUGLauncher/MainWindow/`，组件路径再包含对应职责目录；显式列出的 XAML 路径相对于
`src/MCUGLauncher/`。

## 如何扩展

- 先在 Core 定义业务操作，接收明确参数、取消令牌和进度回调；不要让服务访问 WPF 控件。
- UI 负责读取输入、调用操作和展示结果。长时间任务沿用现有 `Run` / 下载队列入口。
- 账号面板的三种内容分别在 `BuildOfflineIdentity`、`BuildMicrosoftIdentity`、`BuildMuaIdentity` 中构建；
  显示切换统一由 `UpdateAccountPanel` 处理。认证请求仍进入 `Authentication/` 服务。
- 下载任务卡片由 `BuildDownloadTaskCard` 创建；状态变更统一进入 `ChangeDownloadById`，
  再交给协调器处理。不要在按钮回调里直接修改任务文件。
  持久化任务模型在 Core `Domain/Operations.cs`，单次执行上下文在 `Application/OperationContext.cs`，
  异步提交边界在 `Application/OperationExecution.cs`。
- 为实际业务补充对应 `test/*Checks` 的边界检查。不要把预览窗口当作回归检查。
- 默认分支包含当前源码，Release 附件必须从同一提交构建。历史标签保持原样。

## 编码约定

- 一条执行语句一行，4 空格缩进。类型、公开成员使用 PascalCase；现有窗口私有成员保留当前 camelCase。
- 方法按职责命名；长方法优先按明确步骤提取。避免为了少写几行把多层条件、状态变更和副作用揉在一起。
- 普通条件用 `if`，状态映射可用 `switch`；三元表达式只用于容易读懂的值选择。
- 新增控件样式优先放在 XAML；C# 负责动态数据、事件和必要的控件组合。
- 注释解释约束、顺序、兼容性或原因。不要逐句翻译代码，也不要写无依据的保证。
- 不记录密码、令牌或个人游戏路径。不要随意改变 JSON 字段名、账号加密熵和存储目录。
- 内部 `BlockLaunch` 命名空间和目录仍用于兼容已有配置；用户可见品牌是 MCUG Launcher。

## 格式检查

从仓库根目录执行，使用 .NET 8 SDK 自带的 Roslyn 组件，不增加启动器运行时依赖：

```powershell
dotnet run --project tools/SourceFormatter -- --write
dotnet run --project tools/SourceFormatter
```

第二条只检查，未通过时返回非零退出码。工具排除 `bin` / `obj`，写入前逐一比较 C# token，
防止格式化改变字符串或表达式。原始字符串的行尾会保留，避免改变其运行时内容。
源码格式化不能证明业务正确；仍需构建与相应行为检查。

## 验证边界

多数检查采用隔离目录、模拟 HTTP 和合成实例，适合验证取消、恢复、文件提交和界面状态。
真实微软/MUA 授权、Java 兼容性和不同游戏整合包启动仍需实际环境验证，不能由格式检查代替。
