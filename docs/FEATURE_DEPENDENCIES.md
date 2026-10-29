# 功能依赖关系说明

本文基于 `refactor` 分支 0.21.0、提交 `db0ab6a` 的源码阅读，描述当前实际依赖，
并为后续将业务控制从 `MainWindow` 移出提供参考。

这里的“依赖”包括直接调用、共享状态、文件数据和操作顺序，不等同于单纯的项目引用。
本文按用户可见功能和主要支撑能力归类，不逐一列出按钮、控件和所有辅助方法。
当前仍是 WPF 应用；版本、资源服务和内容更新控制器已实现明确边界，其他功能的建议边界仍可能未实现。

版本查询/目标解析、资源搜索/本地操作/更新准备现已提取到同级 `Versions/` 和 `Resources/`。
相关窗口方法与控制器保留交互适配，具体调用边界见 [版本与资源管理模块](VERSION_RESOURCE_MODULES.md)。

## 1. 路径与符号约定

- `Window/`：`src/MCUGLauncher/MainWindow/`。
- `Controllers/`、`Presentation/`：分别位于 `src/MCUGLauncher/Controllers/` 和 `src/MCUGLauncher/Presentation/`，不再属于窗口子目录。
- `Auth/`：现为 `src/MCUGLauncher/Accounts/`（由共享 Application 编译）；旧授权窗口与头像渲染位于 `MainWindow/UI/Accounts/`。详见 [账号模块](dcg/ACCOUNTS_MODULE.md)。
- `Core/`：`src/MCUGLauncher.Core/`。
- `A → B`：A 调用或需要 B；不表示 B 反向依赖 A。
- “刷新”：调用界面更新，不表示已建立统一的事件总线。

项目级引用为 `MCUGLauncher → MCUGLauncher.Core → CmlLib.Core`。
微软认证库由主程序引用。Core 不反向引用 WPF 主程序，但启动请求中仍出现 CmlLib 会话类型。

## 2. 总体依赖图

```text
XAML / UI 组件
  ├── 调用 MainWindow 分部方法
  └── 调用 Controller / Dialog / Builder
             ├── 读取窗口共享状态、访问控件
             ├── 调用窗口 Run / 入队 / 日志 / 刷新方法
             └── 调用 Auth 或 Core 服务
                           ├── CmlLib / HTTP / Java / 游戏进程
                           └── 文件、配置、实例、任务及内容记录

服务进度 / 任务变更 / 进程输出
  → 回调或事件
  → MainWindow / Dispatcher
  → 多个页面与状态区域刷新
```

这是实际的混合结构，而不是“所有 UI 都必须经 Controller 才能访问 Core”。
下载任务分派已移到 `Downloads/DownloadTaskExecutor.cs`，队列控制仍在窗口；
游戏启动编排已移到独立的 `Launch/GameLaunchController.cs`，窗口通过 `IGameLaunchInteraction` 适配交互。

## 3. 多数功能共同依赖什么？

| 公共依赖 | 当前所有者或入口 | 用途与约束 |
| --- | --- | --- |
| `prefs`、`settings` | 共享 `Settings/Preferences.cs`、`SettingsStore`；由 MainWindow/Front 持有 | 当前根目录、启动参数、网络、账号偏好、主题等；多个功能共用同一配置对象 |
| `http`、`runtimeNetwork` | 共享 `Settings/NetworkPolicy.cs`；由各前端创建 | 认证、资源查询、下载和组件请求；网络设置有保存值和运行时值之分 |
| `operations`、`CurrentToken` | MainWindow；OperationCoordinator | 当前操作、进度、取消、队列及历史；不能随意为每个组件各建一个协调器 |
| `busy`、`Run`、`RequireStopped` | MainWindow | 界面门禁、操作执行和游戏运行检查；不同入口的限制并不完全相同 |
| 当前实例与资源目标 | `Selected`、`Instance`、`Target`、`ManagedDirectory` 等 | 全局选中版本不一定就是某个已打开管理页的目标；排队任务另外保存自己的参数 |
| 游戏进程与会话 | MainWindow 的 `game`、`gameSession` | 启动、停止、日志、窗口行为、诊断以及修改文件前的安全约束 |
| 页面、控件与提示 | MainWindow、InlinePageHost、InlineDialog | 导航、确认、错误展示、页面栈及关闭行为 |

排队任务需要保留创建时的目录、版本和资源参数，不能执行时无条件读取当前页面的选择。
账号和内容搜索等入口也不是简单地全部复用 `busy`：需保留各自取消、浏览与下载交互规则。

## 4. 账号与游戏运行

### 4.1 微软账号

- **界面入口**：`Window/UI/Accounts/AccountUI.cs`、`HomeAccountUI.cs`。
- **调用关系**：账号 UI → `MicrosoftAccountController` → `MicrosoftAccountService`；服务配合
  `EmbeddedMicrosoftOAuthUI`、`MinecraftOwnershipGuard` 与 `ProtectedAccountStorage` 完成授权、授权检查和存储。
- **共享状态**：账号模式、`online`、保存的账号、账号偏好、配置目录、HTTP 客户端和当前操作。
- **影响其他功能**：登录或退出后更新账号面板与头像；启动流程通过 `EnsureMicrosoftLogin` 取得可用会话。
- **耦合点**：旧控制器仍持有 MainWindow，但认证服务与策略已由共享 Application 编译，不依赖任一前端；它不是独立账号程序集。

### 4.2 MUA 账号与认证组件

- **入口**：`Window/UI/Accounts/MuaAccountUI.cs`。
- **调用关系**：UI → `MuaAccountService` → 固定认证 API、凭据存储与角色资料；
  游戏启动另外调用 `Core/Application/AuthlibLaunch.cs` 准备并校验 authlib-injector。
- **共享状态**：`muaAccount`、验证状态、账号模式、偏好、配置目录、HTTP 客户端和取消令牌。
- **顺序约束**：登录/角色选择与启动时会话校验不同；MUA 启动还需站点 metadata 和组件参数。
- **影响其他功能**：头像、账号摘要、启动会话、日志脱敏；不能把 MUA 会话失效当作自动切换离线账号。

### 4.3 离线身份与头像

- **入口**：`HomeAccountUI.cs`、`Controllers/PlayerIdentityController.cs`。
- **调用关系**：账号模式与玩家名 → `PlayerIdentityState` → PlayerIdentityController → AvatarRenderer；
  在线头像还需要认证服务或 HTTP，离线皮肤生成使用 `Builders/OfflineSkinPackBuilder.cs`。
- **共享状态**：玩家名、离线皮肤路径、在线账号、头像缓存目录。
- **顺序约束**：头像刷新有取消和版本检查，防止旧请求覆盖新账号；启动时还需验证离线名称并准备皮肤资源。

### 4.4 游戏启动、停止与进程生命周期

- **入口**：`Window/UI/Launch/GameLaunchUI.cs` 转发到主项目 `Launch/GameLaunchController.cs`。
- **主调用链**：

```text
LaunchClick → Run → Launch（一行转发）→ GameLaunchController
  → 保存设置并检查根目录变化
  → 准备微软 / MUA / 离线会话
  → 检查已安装版本、实例与启动设置
  → 准备默认语言和离线皮肤
  → ModPreflight 预检
  → MUA 模式准备 AuthlibLaunch
  → LaunchService.Build → GameSession.Start
  → ObserveGame / GameWindowBehaviorController
```

- **直接依赖**：InstanceCatalog、ProfileInspector、JavaDiscovery、MemoryPolicy、LaunchSettings、
  账号服务、AuthlibLaunch、LaunchService、GameSession、诊断上下文及操作提交边界。
- **共享状态**：当前实例、配置、会话、游戏进程、日志目录及临时服务器地址。
- **输出**：游戏进程、实时日志、退出观察、窗口行为与最近启动诊断信息。
- **耦合点**：启动编排不再引用窗口，但配置和账号 UI 仍通过交互接口适配；
  会话状态、退出观察和窗口行为仍保留在窗口边界。停止请求走 GameSession，不能以简单强杀替代现有退出语义。
  详见 [启动模块说明](LAUNCH_MODULE.md)。

### 4.5 Java 管理

- **入口**：`Controllers/JavaManagerController.cs`；版本启动表单也消费 Java 选择结果。
- **依赖**：Core 的 JavaDiscovery、JavaSearch、JavaInventory；用户指定路径、当前版本和取消令牌。
- **输出**：候选 Java、版本检查和选择结果，通过回调回写相应设置。
- **关联**：游戏/加载器安装与启动都会使用 Java，但“管理器已选路径”不代表最终实例必定使用该路径，
  还需考虑实例设置和全局设置的选择。

## 5. 在线资源与下载任务

### 5.1 游戏列表与加载器版本

- **入口**：`UI/Instances/GameVersionUI.cs`、`UI/Downloads/DownloadPageUI.cs`、`LoaderOverviewUI.cs`。
- **依赖**：MinecraftLauncher 的版本能力、窗口 `rows` 与 `downloadVersion`、LoaderCatalog、
  LoaderInstallationController、当前游戏目录和 HTTP 客户端。
- **流向**：查询版本 → 展示/筛选 → 用户选择 → 构造游戏安装 DownloadRequest → 入队。
- **耦合点**：首页选中版本、下载页目标版本和加载器选择相互关联，但不是同一个状态字段。

### 5.2 资源搜索与详情

- **入口**：`UI/Content/ContentUI.cs`、ResourceFiltersUI、ResourceSearchLayoutUI、
  ResourceWorkspaceUI、ResourcePresentationUI 和 ResponsiveResourcePanel。
- **依赖**：ContentSearchFilterBuilder、ContentService、当前资源种类、游戏版本、加载器、
  分页/排序、收藏筛选与独立浏览取消逻辑。
- **流向**：筛选条件 → 搜索 → 详情/版本/依赖展示 → 安装或独立下载请求。
- **关联**：详情页读取收藏状态；安装需要明确目标实例/存档；独立下载需要独立目标目录。
- **耦合点**：多个 UI partial 共享搜索与详情状态；切换实例或类型会影响请求条件，旧响应不得覆盖新页面。

### 5.3 资源收藏与文件夹

- **入口**：`UI/Content/ResourceFavoritesUI.cs`、`UI/Instances/CollectionsUI.cs`。
- **依赖**：ResourceBookmarkStore、收藏模型、BookmarkBatch、ContentService 及当前资源目标。
- **流向**：收藏/取消 → 保存收藏 → 更新搜索卡片、详情按钮和收藏列表；批量操作另需解析资源兼容版本。
- **耦合点**：两个页面使用同一收藏数据，不应各自维护互不通知的副本。

### 5.4 下载队列与实际任务分派

- **入口**：`UI/Downloads/DownloadQueueUI.cs` 调用主项目 `Downloads/DownloadTaskExecutor.cs`；多个页面通过 EnqueueDownload 创建任务。
- **依赖**：OperationCoordinator、DownloadRequest、OperationContext、队列目录锁、定时器和网络策略。
- **实际分派**：

| 请求种类 | 执行依赖 | 结果 |
| --- | --- | --- |
| `game` | GameInstaller | 游戏/加载器安装 |
| `resource` | ResourceDownloads | 下载到独立目录 |
| `pack`、`import` | PackService + GameInstaller + InstanceCatalog | 新整合包实例 |
| `content` | ContentService.Resolve / Install | 为指定目录安装内容及依赖 |

- **共同约束**：安装恢复、哈希校验、取消/暂停、必要的存档保护和提交边界。
- **耦合点**：队列状态管理在 Core、队列调度仍在窗口；任务执行器已独立，通过显式服务/回调依赖获取网络、存档保护和刷新能力。
  详见 [下载执行器说明](DOWNLOAD_EXECUTOR.md)。

### 5.5 任务显示、取消与恢复

- **入口**：DownloadTaskUI、MiniDownloadUI 和 `UI/Support/StabilityUI.cs`。
- **依赖**：同一个 OperationCoordinator 的活动记录与持久化历史；任务栏与 WPF 进度控件。
- **流向**：任务变化 → Dispatcher → 顶部/紧凑/完整任务视图刷新。
- **恢复依赖**：InstallationRecovery、InstanceCatalog.MigrateLegacy 和任务中断记录。
- **约束**：显示组件可以拆开，但不能各自创建队列；暂停、取消和提交中的行为必须保留一致语义。

## 6. 实例、内容与整合包

### 6.1 实例列表、搜索与目录切换

- **入口**：UI/Instances 下的 FeatureUI、SketchUI、GameVersionUI；InstanceSearchController。
- **依赖**：InstanceCatalog、InstanceManagement、ProfileInspector、当前根目录与已安装版本。
- **输出**：版本列表、搜索结果、当前实例、概览和管理入口。
- **跨功能影响**：切换根目录会影响 launcher、版本列表、资源安装目标、启动设置和实例管理。
- **约束**：需保留运行中/忙碌时的目录切换限制；不能只更新列表而不更新服务上下文。

### 6.2 实例设置与管理页面

- **入口**：VersionSettingsUI → VersionSettingsDialog / VersionSettingsWindowView；
  SketchUI → SketchContentManagerDialog；InstanceLibraryUI → InstanceLibraryDialog。
- **依赖**：VersionSettingsPageContext、实例描述和目录、全局启动默认值、各 Version*PageBuilder。
- **子功能**：概览、启动选项、加载器、模组管理、整合包导出、启动脚本导出及文件管理。
- **耦合点**：页面上下文持有 MainWindow 和 WPF 控件；已打开页面的实例 ID 应保持明确，不能被全局选择偷偷替换。

### 6.3 本地内容、存档与拖入导入

- **入口**：LocalContentUI、InstanceLibraryUI、FileImportController、相关管理对话框。
- **依赖**：LocalImportClassifier、ContentService、WorldArchives、WorldMetadata 和明确的导入目标；
  整合包导入另外走队列与确认流程。
- **流向**：识别文件 → 确认目标/包名 → 导入或入队 → 刷新目标内容。
- **耦合点**：FileImportController 主要承担拖入反馈和包名确认，并不独立包办全部导入操作。
- **约束**：资源种类、当前存档和实例隔离设置会改变目标目录，不能把所有文件都写到根目录。

### 6.4 内容更新与回退

- **入口**：UpdatesUI → ContentUpdateController → 更新选择与预览视图。
- **依赖**：ContentUpdates、ContentService、ContentTransaction、当前游戏/加载器与目标目录。
- **流向**：检查更新 → 选择 → 解析依赖与预览 → 入队 PreparedPlan → 安装；回退调用内容事务恢复。
- **约束**：依赖方案、内容指纹和游戏停止检查不能被 UI 重构省略；存档与内容事务是不同的保护范围。

### 6.5 实例克隆与删除

- **入口**：CollectionsUI 的克隆入口；UninstallUI → InstanceUninstallationController。
- **依赖**：InstanceCloner、InstanceUninstaller、InstanceCatalog、启动默认值和文件目录。
- **流向**：克隆/删除计划 → 用户确认 → 执行 → 刷新实例及版本视图。
- **约束**：删除需要区分共享目录与隔离目录；删除确认时的目标和计划不能被之后的页面选择替换。

### 6.6 整合包导入、导出与升级

- **入口**：PackUI、PackUpgradeUI、InstancePackExportDialog、VersionPackExportPageBuilder。
- **依赖**：PackService、GameInstaller、InstanceCatalog、PackUpgrade、SaveProtection、操作队列。
- **导入**：预读包信息与确认 → 记录来源/选项 → 入队 → 安装游戏与内容 → 写入实例。
- **升级**：准备预览 → 用户决定保留文件 → 保护存档 → 提交升级副本 → 刷新并切换实例。
- **约束**：升级副本和原实例的关系、文件基线、可选内容、导入来源校验均属于业务语义，不是纯 UI。

### 6.7 启动设置与脚本导出

- **入口**：LaunchOptionsDialog、LaunchOptionsForm、VersionLaunchPageBuilder、
  VersionLaunchScriptExportPageBuilder 和 FeatureUI.ExportScript。
- **依赖**：实例描述、Preferences.Launch、LaunchSettings、LaunchScriptBuilder 和当前账号模式。
- **关联**：全局默认与实例覆盖共同影响启动；修改表单不等于已经更新运行中的游戏。
- **约束**：导出脚本应通过启动器处理授权，不能把密码或 Token 写入脚本。

## 7. 设置、工具与窗口基础能力

### 7.1 配置与网络

- **入口**：PreferencesUI、NetworkSettingsController、ExperienceUI 的限速入口。
- **依赖**：SettingsStore、Preferences、NetworkSettings、NetworkPolicyHandler 与 runtimeNetwork。
- **约束**：网络对话框保存代理/源设置并提示下次启动生效，不能把保存配置理解为所有现有客户端已更新。
- **跨功能影响**：配置包含账号偏好、实例、启动、主题与网络，不应为每个组件分配一个互相覆盖的写入副本。

### 7.2 存档保护与服务器收藏

- **入口**：ExperienceUI；启动、升级和加载器操作也会消费相关能力。
- **依赖**：SaveProtection、WorldArchives、WorldMetadata；服务器使用 ServerFavorites、ServerEndpoint 和配置中的服务器列表。
- **关联**：服务器连接最终回到游戏启动流程；存档保护可能作为修改实例前的步骤执行。
- **约束**：存档备份不等于内容回退；服务器状态查询不等于已成功加入服务器。

### 7.3 日志、诊断与反馈

- **入口**：DiagnosticsUI、StabilityUI、MainWindow.Log。
- **依赖**：GameSession 输出、DiagnosticContext、Diagnostics、ModPreflight、LogPrivacy 和日志文件。
- **流向**：当前实例或最近启动上下文 → 捕获/分析 → 显示问题与证据 → 生成反馈预览/导出。
- **约束**：日志脱敏必须留在公共输出边界；诊断应使用对应启动/实例上下文，而不只读取当前选中版本。

### 7.4 外观、窗口与导航

- **入口**：AppearanceUI、ModernUI、CompactShellUI、PageNavigationController、InlinePageHost。
- **依赖**：App.xaml 资源、WindowThemeStyler、ModernThemeStyler、WindowChromeBuilder、背景文件与偏好。
- **输出**：主题、背景、标题栏、响应式布局、页面切换和内嵌页面栈。
- **耦合点**：部分布局会重新组合控件，初始化顺序和父子关系仍然重要；对话框也依赖公共主题/宿主。

### 7.5 菜单扩展

- **入口**：MenuExtensions、MenuExtensionsUI、LauncherMenuRegistry。
- **依赖**：注册区域、页面工厂、LauncherMenuContext、MainWindow 与 WPF FrameworkElement。
- **区域**：顶部、下载、设置、更多、资源详情、主页、版本设置、实例内容。
- **约束**：这是源码级扩展，页面仍可能访问窗口；不是外部插件沙箱或与 UI 框架无关的业务接口。

## 8. 最需要保留的跨功能规则

| 场景 | 必须同时考虑的依赖 |
| --- | --- |
| 切换账号 | 会话、偏好、头像、账号摘要与下一次启动；不能只改显示名称 |
| 切换实例/根目录 | 版本、安装目标、启动配置、资源筛选、已打开管理页及排队任务的原目标 |
| 修改实例文件 | 游戏运行状态、当前操作、任务取消、文件事务与必要的存档保护 |
| 关闭启动器 | 当前任务取消/清理、下载队列、页面与异步请求生命周期；游戏进程另有退出语义 |
| 后台请求完成 | UI 线程、取消/过期结果、当前页面目标；不能用旧响应覆盖新状态 |

## 9. 若改为“组件通过接口调用模块”

下列是建议边界，不是当前已经存在的 C# 接口，也不要求引入 HTTP 或进程通信。

| 建议模块 | 模块负责 | UI 组件负责 |
| --- | --- | --- |
| 账号服务 | 会话、验证、账号切换和身份状态；授权交互通过适配器请求 | 输入、角色选择、身份展示 |
| 实例服务 | 根目录/实例操作、目标解析、克隆与删除计划 | 列表、搜索输入、危险操作确认 |
| 下载服务 | 队列、任务分派、快照参数、进度与取消 | 任务展示与控制请求 |
| 资源服务 | 查询、收藏、兼容性方案、安装/更新请求 | 搜索布局、详情、收藏按钮 |
| 启动应用服务 | 协调账号、Java、实例、预检、认证组件和进程 | 发起启动、展示需用户决策的结果 |
| 设置与辅助服务 | 设置保存、诊断、存档和服务器用例 | 表单与结果展示 |

公共约束应放在应用层/服务层，不能让组件各自决定安装、启动和删除能否并发。
模块内部持有自己的状态；多个组件通过状态快照与明确事件订阅共享结果。
MainWindow 保留窗口生命周期、导航与依赖组装，不再成为所有模块获取状态的唯一入口。

建议先提取完整的下载任务执行器或账号用例，保留原检查和数据兼容，再替换对应 UI 调用。
不要只增加一层接口名称，却让接口实现继续回调 MainWindow 完成所有业务。

## 10. 阅读与验证

- [架构总览](ARCHITECTURE.md)：整体结构与启动流程。
- [开发导览](DEVELOPMENT.md)：功能定位。
- [窗口组件索引](MainWindow/README.md)：逐类说明与源码。
- [构建说明](BUILD.md)：构建、静态检查及回归入口。

本文为静态阅读结果，不宣称已运行全部功能。后续改变模块边界时，应按功能运行对应检查；
目录校验通过不能代替账号授权、真实游戏、存档恢复和文件删除边界的验证。
