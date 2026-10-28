# 开发导览

MCUG Launcher 是 .NET 8 / WPF 应用。先从本文件定位功能，再阅读对应入口。
构建、打包和运行检查见 [BUILD.md](BUILD.md)。

## 先读哪些文件

1. `src/MCUGLauncher/MainWindow.xaml.cs`：原生窗口、页面组合和页内对话框衔接。
2. `Composition/LauncherWorkspace.cs`：模块组合；`LauncherControls.cs`：页面控件的强类型绑定。
3. `Views/Pages/`：九个独立页面/导航视图的 XAML；`App.xaml`：公共样式。
4. `Components/`：按功能分开的控制器，拥有各自状态；`Presentation/` 与 `Views/`：已独立提取的视图和 Presenter。
5. `src/MCUGLauncher.Core/`：启动、认证、下载和实例等业务服务。
6. `test/`：原有断言继续执行，测试检查入口改为定位所属模块。

页面动作经 `LauncherPage` 事件边界进入 `Composition/LauncherPageRouter.cs` 的强类型路由。
控制器通过 `workspace.Controls` 使用页面输入，通过 `workspace.Window` 使用原生窗口能力，跨模块协作通过显式的模块属性。
`LauncherWorkspace` 只负责组合，不拥有下载、账号或实例业务状态。不要重新添加 `partial MainWindow` 功能文件。
页面迁移时保留原有动态布局的视图树形状；详细说明见 [MainWindow 重构记录](MAINWINDOW_REFACTOR.md)。

## 功能定位

| 想修改的内容 | 界面入口 | 服务或数据 |
| --- | --- | --- |
| 账号面板、登录方式 | `Components/HomeAccountController.cs` | `Authentication/` |
| 主导航、窗口拖动、大小适配 | `Components/ModernController.cs`、`Components/CompactShellController.cs`、`Components/WindowChromeController.cs` | 窗口自身 |
| 主页版本选择、版本内容管理 | `Components/SketchController.cs`、`Components/VersionSettingsController.cs` | Core `Domain/Instances.cs`、`Application/InstanceManagement.cs` |
| 版本和资源下载 | `Components/DownloadPageController.cs`、`Components/ContentController.cs` | Core `GameInstaller.cs`、`ContentService.cs` |
| 下载队列与任务操作 | `Components/DownloadQueueController.cs`、`Components/DownloadTaskController.cs`、`Components/MiniDownloadController.cs` | Core `Application/OperationCoordinator.cs` |
| 文件拖入、资源导入 | `Components/DropFeedbackController.cs`、`Components/InstanceLibraryController.cs` | Core `Application/LocalImportClassifier.cs`、`WorldArchives.cs` |
| 整合包导入、导出、升级 | `Components/PackController.cs`、`Components/PackUpgradeController.cs` | Core `PackService.cs`、`Application/PackUpgrade.cs` |
| Java 与游戏启动 | `Components/JavaManagerController.cs`、`MainWindow.xaml.cs` | Core `Application/Java*.cs`、`LaunchService.cs` |
| 主题、背景 | `Components/AppearanceController.cs`、`Components/ExperienceThemeController.cs`、`App.xaml` | Core `Domain/Preferences.cs` |
| 崩溃分析与反馈 | `Components/DiagnosticsController.cs` | Core `Application/Diagnostics.cs` |

表中的 Core 路径相对于 `src/MCUGLauncher.Core/`；界面路径相对于 `src/MCUGLauncher/`。

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


## 0.20.1 下载界面扩展

- `Components/DownloadNavigationController.cs`：分类名称与 `DownloadCategoryIcon` 映射。为条目的 `Image` 传入 `ImageSource` 即显示图片；留空时回退到字形，不影响分类键或下载逻辑。图片建议以 WPF Resource 加入项目，并使用 pack URI 加载。
- `Components/ResourceWorkspaceController.cs`：列表与页内详情切换、兼容信息与依赖数量展示、窄窗口布局。复用既有命名控件及事件，资源独立下载和整合包导入行为保持原样。
- `Components/LoaderOverviewController.cs`：横向加载器配置行，从官方目录读取兼容版本；选择后调用既有 EnqueueDownload，不实现第二套安装器。
- 列表模板 `ResourceSearchItem` 位于 `App.xaml`。不展示服务端未提供的评价、点赞或截图。

- `ResponsiveResourcePanel.cs`：分页结果的自适应卡片排列；`Components/ResourceSearchLayoutController.cs`：搜索及带标签筛选布局。
- `Components/ResourcePresentationController.cs`：可选的项目介绍与截图展示。请求失败不影响版本选择和下载；截图限 Modrinth CDN，正文仅显示有长度上限的纯文本预览。


## 0.20.2 菜单与资源交互

- 新增栏目从 `MenuExtensions.cs` 注册，入口定义在 `Navigation/LauncherMenuRegistry.cs`，宿主适配在 `Components/MenuExtensionsController.cs`。具体区域、上下文和图片图标示例见 [菜单扩展说明](MENU_EXTENSIONS.md)。
- `Components/ResourceFavoritesController.cs` 负责收藏筛选、心形状态及视图切换，持久化复用 `ResourceBookmarkStore`，不创建第二份收藏文档。
- 收藏兼容性检查最多并发四个请求，并跟随浏览取消；资源服务保留原始 Search 签名，通过新增重载接收受限的排序值。

## test 分支的组件依赖细化

查看 [窗口拆分与依赖约束](MAINWINDOW_REFACTOR.md) 和 [分支结构比较](TEST_REFACTOR_COMPARISON.md)。账号展示、加载器概览、网络编辑及原生窗口控件通过显式构造参数连接；新的小组件优先接收实际控件、只读状态或操作回调。公共 UI 元素位于 `Views/Shared`，表单位于 `Views/Forms`，数据记录位于 `Models`，纯文件构建位于 `Builders`。

版本设置的页面主体位于 `Views/VersionSettings/`，动作连接在 `Composition/VersionSettingsComposition.cs`。新增或修改某一页时优先定位对应的 `Version*Page`，不要把页面代码重新加进版本设置协调器。
