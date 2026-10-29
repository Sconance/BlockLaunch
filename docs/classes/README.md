# 类文档目录

`src/MCUGLauncher/` 下每个顶层类型一页，按代码目录分组。文档由当前源码整理，
`python scripts/check-class-docs.py` 校验覆盖与索引一致性。

## 根目录

- [App](App.md) — 应用启动入口与全局资源、未处理异常处理。
- [Brand](Brand.md) — 品牌名与兼容存储目录常量。
- [InlineDialog](InlineDialog.md) — 主窗口内的对话框表面，不创建原生窗口。
- [InlineFilePicker](InlineFilePicker.md) — 页内文件选择器的基类与目录浏览。
- [InlineMessageBox](InlineMessageBox.md) — 页内消息框的便捷入口。
- [MainWindow](MainWindow.md) — 原生窗口 shell：装配九个页面、组合根与页内对话框入口。
- [MenuExtensionsController](MenuExtensionsController.md) — 把源码注册的菜单扩展接入各宿主页面。
- [OpenFileDialog](OpenFileDialog.md) — 页内打开文件对话框。
- [OpenFolderDialog](OpenFolderDialog.md) — 页内选择文件夹对话框。
- [ResponsiveResourcePanel](ResponsiveResourcePanel.md) — 把分页资源结果排列为等宽自适应卡片。
- [SaveFileDialog](SaveFileDialog.md) — 页内保存文件对话框。

## `Authentication/`

- [AccountLoginException](Authentication/AccountLoginException.md) — 登录失败异常，可标记是否需要交互式重试。
- [AccountStorageException](Authentication/AccountStorageException.md) — 账号存储读写失败时抛出的异常。
- [AvatarRenderer](Authentication/AvatarRenderer.md) — 按皮肤地址渲染头像位图，并把结果缓存到本地文件。
- [EmbeddedMicrosoftOAuthUI](Authentication/EmbeddedMicrosoftOAuthUI.md) — 在应用内 WebView2 中承载微软 OAuth 授权页，只替换浏览器表面。
- [MemoryAccountStorage](Authentication/MemoryAccountStorage.md) — 仅保存在内存中的账号存储，用于检查与临时场景。
- [MicrosoftAccountService](Authentication/MicrosoftAccountService.md) — 微软账号的登录、会话刷新、保存与退出。
- [MinecraftOwnershipGuard](Authentication/MinecraftOwnershipGuard.md) — 在微软认证流程中校验账号是否拥有 Minecraft。
- [MuaAccountService](Authentication/MuaAccountService.md) — MUA 皮肤站账号的登录、资料读取与皮肤获取。
- [MuaCredential](Authentication/MuaCredential.md) — MUA 登录凭据，保存令牌与角色信息。
- [MuaMetadata](Authentication/MuaMetadata.md) — MUA 元数据：签名公钥与允许的皮肤域名。
- [MuaProfile](Authentication/MuaProfile.md) — MUA 账号的 ID 与名称。
- [ProtectedAccountStorage](Authentication/ProtectedAccountStorage.md) — 用 Windows 用户级加密读写账号 JSON。
- [SavedMicrosoftAccount](Authentication/SavedMicrosoftAccount.md) — 已保存微软账号的用户名、UUID 与皮肤地址快照。

## `Builders/`

- [ContentSearchFilterBuilder](Builders/ContentSearchFilterBuilder.md) — 把当前筛选状态转换为资源搜索参数。
- [LaunchScriptBuilder](Builders/LaunchScriptBuilder.md) — 生成并转义启动参数，产出可安全引用的启动命令。
- [OfflineSkinPackBuilder](Builders/OfflineSkinPackBuilder.md) — 为离线实例准备皮肤包文件。

## `Components/Accounts/`

- [AccountController](Components/Accounts/AccountController.md) — 账号面板交互：登录、切换与管理入口协调。
- [AvatarController](Components/Accounts/AvatarController.md) — 头像与名称刷新，显式接收控件与状态回调。
- [HomeAccountController](Components/Accounts/HomeAccountController.md) — 首页账号面板的构建与状态更新。
- [MuaAccountController](Components/Accounts/MuaAccountController.md) — MUA 账号面板交互与登录管理。

## `Components/Bookmarks/`

- [BookmarkLibraryController](Components/Bookmarks/BookmarkLibraryController.md) — 收藏夹与本地内容集合的展示和操作。

## `Components/Downloads/`

- [DownloadMiniBarController](Components/Downloads/DownloadMiniBarController.md) — 顶部迷你下载条、速度与暂停状态。
- [DownloadNavigationController](Components/Downloads/DownloadNavigationController.md) — 下载分类的图标与名称映射。
- [DownloadPageController](Components/Downloads/DownloadPageController.md) — 下载页布局、分类切换与加载器入口。
- [DownloadQueueController](Components/Downloads/DownloadQueueController.md) — 下载队列的入队、暂停、恢复与执行。
- [DownloadTaskController](Components/Downloads/DownloadTaskController.md) — 下载任务页、任务状态操作与任务栏进度。
- [LoaderOverviewController](Components/Downloads/LoaderOverviewController.md) — 加载器版本概览与入队，显式接收控件与回调。
- [LoaderSelectionController](Components/Downloads/LoaderSelectionController.md) — 加载器类型切换与自动选择。

## `Components/Experience/`

- [AppearanceController](Components/Experience/AppearanceController.md) — 应用背景、主题色与个性化设置。
- [ExperienceController](Components/Experience/ExperienceController.md) — 个性化、存档保护与服务端等体验功能。
- [ExperienceThemeController](Components/Experience/ExperienceThemeController.md) — 把主题配置应用到工具窗口。

## `Components/Instances/`

- [DropFeedbackController](Components/Instances/DropFeedbackController.md) — 拖入文件的提示与目标目录判断。
- [InstanceContentController](Components/Instances/InstanceContentController.md) — 首页版本选择与实例内容管理入口。
- [InstanceLibraryController](Components/Instances/InstanceLibraryController.md) — 实例库对话框与拖入文件导入。
- [InstanceSearchController](Components/Instances/InstanceSearchController.md) — 按输入过滤实例列表并更新匹配摘要。
- [InstanceWorkspaceController](Components/Instances/InstanceWorkspaceController.md) — 实例页面、内容管理与启动设置入口。
- [JavaManagerController](Components/Instances/JavaManagerController.md) — Java 运行时的扫描、选择与管理。
- [UninstallController](Components/Instances/UninstallController.md) — 实例卸载流程。
- [VersionIdentityEditor](Components/Instances/VersionIdentityEditor.md) — 实例名称、描述等身份信息的编辑与保存。
- [VersionSettingsController](Components/Instances/VersionSettingsController.md) — 版本设置页内窗口的生命周期与栏目导航。

## `Components/Launch/`

- [GameWindowBehaviorController](Components/Launch/GameWindowBehaviorController.md) — 启动后调整游戏窗口的行为与焦点。
- [LaunchCoordinator](Components/Launch/LaunchCoordinator.md) — 游戏启动流程、运行状态与设置/网络初始化。
- [ModpackController](Components/Launch/ModpackController.md) — 整合包导入、安装、导出与启动版本标签。
- [PackUpgradeController](Components/Launch/PackUpgradeController.md) — 整合包升级流程与原版回退。
- [TaskRecoveryController](Components/Launch/TaskRecoveryController.md) — 任务状态、运行日志与恢复操作。

## `Components/Resources/`

- [ContentUpdateActions](Components/Resources/ContentUpdateActions.md) — 内容更新检查所需的应用操作集合。
- [ContentUpdatesController](Components/Resources/ContentUpdatesController.md) — 内容更新的检查、选择、预览与入队。
- [ResourceBrowserController](Components/Resources/ResourceBrowserController.md) — 资源搜索、详情、安装与内容更新入口。
- [ResourceFavoriteActions](Components/Resources/ResourceFavoriteActions.md) — 收藏操作与存储访问的显式回调。
- [ResourceFavoriteControls](Components/Resources/ResourceFavoriteControls.md) — 收藏查询所需的控件集合。
- [ResourceFavoritesController](Components/Resources/ResourceFavoritesController.md) — 收藏切换、仅收藏筛选与列表/卡片视图切换。
- [ResourceFiltersController](Components/Resources/ResourceFiltersController.md) — 资源筛选条件与搜索目标的维护。
- [ResourcePresentationController](Components/Resources/ResourcePresentationController.md) — 资源正文、截图与版本事实的展示。
- [ResourceSearchLayoutContext](Components/Resources/ResourceSearchLayoutContext.md) — 搜索布局装配所需的控件与动作上下文。
- [ResourceSearchLayoutController](Components/Resources/ResourceSearchLayoutController.md) — 资源搜索区布局的装配。
- [ResourceWorkspaceController](Components/Resources/ResourceWorkspaceController.md) — 资源页内详情工作区与窄/宽布局切换。

## `Components/Shell/`

- [CompactShellController](Components/Shell/CompactShellController.md) — 紧凑窗口外壳、首页版本入口与导航布局。
- [InlineHostController](Components/Shell/InlineHostController.md) — 页内对话框栈、标题与显示切换。
- [KeyboardNavigationController](Components/Shell/KeyboardNavigationController.md) — 键盘导航与工具提示初始化。
- [ModernShellController](Components/Shell/ModernShellController.md) — 现代窗口外壳、响应式布局与窗口拖动。
- [NetworkController](Components/Shell/NetworkController.md) — 网络设置的编辑与页内保存，使用显式回调。
- [WindowChromeController](Components/Shell/WindowChromeController.md) — 自定义标题栏与窗口边框。

## `Components/Support/`

- [DiagnosticsController](Components/Support/DiagnosticsController.md) — 崩溃诊断快照、证据展示与反馈。

## `Composition/`

- [LauncherControls](Composition/LauncherControls.md) — 九个页面视图的强类型控件引用。
- [LauncherWorkspace](Composition/LauncherWorkspace.md) — 组合根：构造各功能模块、连接页面动作与 Core 服务，本身不保存业务状态。

## `Initialization/`

- [LauncherTooltipInitializer](Initialization/LauncherTooltipInitializer.md) — 初始化导航与按钮的工具提示。
- [UtilityWindowInitializer](Initialization/UtilityWindowInitializer.md) — 配置工具窗口的样式与行为。

## `Models/`

- [BookmarkView](Models/BookmarkView.md) — 收藏项的展示模型。
- [DownloadCategoryIcon](Models/DownloadCategoryIcon.md) — 下载分类的图片或回退字形。
- [InstanceTarget](Models/InstanceTarget.md) — 实例目标的版本、游戏版本与加载器。
- [InstanceView](Models/InstanceView.md) — 实例列表的展示模型。
- [PlayerIdentityState](Models/PlayerIdentityState.md) — 账号身份的展示快照。
- [VersionModView](Models/VersionModView.md) — 版本 Mod 列表的展示模型。

## `Navigation/`

- [LauncherMenuArea](Navigation/LauncherMenuArea.md) — 菜单扩展可注册的区域。
- [LauncherMenuContext](Navigation/LauncherMenuContext.md) — 页面工厂收到的宿主上下文。
- [LauncherMenuPage](Navigation/LauncherMenuPage.md) — 菜单页面定义：ID、标题与内容工厂。
- [LauncherMenuRegistry](Navigation/LauncherMenuRegistry.md) — 源码级菜单注册表，不加载外部插件。

## `Presentation/`

- [FeatureDialogBuilder](Presentation/FeatureDialogBuilder.md) — 构造通用功能对话框。
- [HomeWelcomePresenter](Presentation/HomeWelcomePresenter.md) — 配置首页默认欢迎区，不读写偏好。
- [InstanceManagerBindings](Presentation/InstanceManagerBindings.md) — 内容管理与宿主的刷新绑定。
- [InstanceManagerCommands](Presentation/InstanceManagerCommands.md) — 内容管理可用的操作集合。
- [InstanceManagerData](Presentation/InstanceManagerData.md) — 内容管理对话框的数据快照。
- [InstancePackExportActions](Presentation/InstancePackExportActions.md) — 整合包导出所需的数据与操作。
- [InstancePackExportDialog](Presentation/InstancePackExportDialog.md) — 整合包导出对话框。
- [LaunchOptionsActions](Presentation/LaunchOptionsActions.md) — 启动选项读写所需的应用回调。
- [LaunchOptionsDialog](Presentation/LaunchOptionsDialog.md) — 全局或实例级启动选项对话框。
- [LoaderConfigurationPresenter](Presentation/LoaderConfigurationPresenter.md) — 加载器目录浏览、取消与选择；安装交给应用回调。
- [LoaderSelection](Presentation/LoaderSelection.md) — 加载器版本选择结果。
- [ResourceFavoriteQuery](Presentation/ResourceFavoriteQuery.md) — 收藏查询的不可变输入，避免异步过程读取控件。
- [ResourceFavoriteSearch](Presentation/ResourceFavoriteSearch.md) — 收藏候选的并发查询、排序与分页。
- [SketchContentManagerDialog](Presentation/SketchContentManagerDialog.md) — 实例内容管理对话框，使用受限依赖与既有修复。
- [VersionRow](Presentation/VersionRow.md) — 版本行展示模型。
- [VersionSettingsPageContext](Presentation/VersionSettingsPageContext.md) — 版本设置页的实例数据与重绘入口。
- [VersionTaskActions](Presentation/VersionTaskActions.md) — 版本编辑共享的任务执行能力。

## `Styling/`

- [InstanceListScrollStyle](Styling/InstanceListScrollStyle.md) — 实例列表滚动条样式。
- [NavigationMenuStyle](Styling/NavigationMenuStyle.md) — 导航菜单项样式。

## `Views/`

- [AccountPanelActions](Views/AccountPanelActions.md) — 账号面板的登录与刷新回调。
- [AccountPanelControls](Views/AccountPanelControls.md) — 账号面板使用的控件集合。
- [AccountPanelView](Views/AccountPanelView.md) — 账号面板视图，只负责呈现。
- [HomeWelcomeView](Views/HomeWelcomeView.md) — 首页欢迎区视图。
- [LoaderConfigurationView](Views/LoaderConfigurationView.md) — 加载器配置行与版本选择视图。
- [ResourceFilterField](Views/ResourceFilterField.md) — 资源搜索的单个筛选字段。
- [ResourceSearchHeader](Views/ResourceSearchHeader.md) — 资源搜索与筛选布局视图。
- [ResourceSearchHeaderControls](Views/ResourceSearchHeaderControls.md) — 资源搜索头部的控件集合。

## `Views/Forms/`

- [LaunchOptionsForm](Views/Forms/LaunchOptionsForm.md) — 全局启动选项表单。
- [NetworkSettingsForm](Views/Forms/NetworkSettingsForm.md) — 网络设置表单，不访问持久化。

## `Views/Pages/`

- [ContentManagementPageView](Views/Pages/ContentManagementPageView.md) — 内容管理页面视图。
- [HeadingPageView](Views/Pages/HeadingPageView.md) — 标题区页面视图。
- [HomePageView](Views/Pages/HomePageView.md) — 开始游戏页面视图。
- [InstallPageView](Views/Pages/InstallPageView.md) — 游戏安装页面视图。
- [LauncherPage](Views/Pages/LauncherPage.md) — 独立页面基类，提供动作边界与内容物化。
- [LauncherPageAction](Views/Pages/LauncherPageAction.md) — 页面动作参数。
- [LogsPageView](Views/Pages/LogsPageView.md) — 运行日志页面视图。
- [NavigationPageView](Views/Pages/NavigationPageView.md) — 主导航页面视图。
- [ResourcePageView](Views/Pages/ResourcePageView.md) — 内容中心页面视图。
- [SettingsPageView](Views/Pages/SettingsPageView.md) — 设置页面视图。
- [TasksPageView](Views/Pages/TasksPageView.md) — 任务与恢复页面视图。

## `Views/Shared/`

- [FeatureUiElements](Views/Shared/FeatureUiElements.md) — 通用标题、说明与动作按钮工厂。
- [InlineEscapeHandler](Views/Shared/InlineEscapeHandler.md) — Esc 时先关闭展开的选择器。

## `Views/Updates/`

- [ContentUpdatePreviewView](Views/Updates/ContentUpdatePreviewView.md) — 内容更新预览视图。
- [ContentUpdateSelectionView](Views/Updates/ContentUpdateSelectionView.md) — 内容更新选择视图。

## `Views/VersionSettings/`

- [VersionExportPage](Views/VersionSettings/VersionExportPage.md) — 版本整合包导出页面。
- [VersionLaunchActions](Views/VersionSettings/VersionLaunchActions.md) — 版本启动页面所需的操作。
- [VersionLaunchPage](Views/VersionSettings/VersionLaunchPage.md) — 版本启动设置页面。
- [VersionLoaderActions](Views/VersionSettings/VersionLoaderActions.md) — 版本加载器页面所需的操作。
- [VersionLoaderPage](Views/VersionSettings/VersionLoaderPage.md) — 版本加载器修改页面。
- [VersionModActions](Views/VersionSettings/VersionModActions.md) — 版本 Mod 页面所需的操作。
- [VersionModPage](Views/VersionSettings/VersionModPage.md) — 版本 Mod 管理页面。
- [VersionOverviewActions](Views/VersionSettings/VersionOverviewActions.md) — 版本概览页面所需的操作。
- [VersionOverviewPage](Views/VersionSettings/VersionOverviewPage.md) — 版本概览与身份编辑页面。
- [VersionSettingsView](Views/VersionSettings/VersionSettingsView.md) — 版本设置窗口布局与卡片、字段构造。

## 0.22.1 运行与启动加载组件

- [RunningGamesController](Components/Launch/RunningGamesController.md) — 多实例运行会话的展示和观察。
- [VersionCatalogController](Components/Downloads/VersionCatalogController.md) — 在线版本按需读取与超时隔离。

- [IRunningGamesView](Presentation/IRunningGamesView.md) — 框架无关的展示端口。
- [RunningGameRow](Presentation/RunningGameRow.md) — 纯数据会话行。
- [RunningGameActions](Components/Launch/RunningGameActions.md) — 注入的日志与诊断能力。
- [RunningGamesView](Views/RunningGamesView.md) — WPF 视图适配器。
