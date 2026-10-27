# MainWindow 组件目录

本文档列出 `src/MCUGLauncher/MainWindow/` 中的所有类和记录类型。每个类型均有独立说明页，介绍其职责、主要入口/协作关系及源码位置。

## 分类结构

```text
MainWindow/
├── MainWindow.xaml.cs
├── UI/
│   ├── Accounts/     账号面板及认证入口
│   ├── Content/      内容搜索、导入和更新页面
│   ├── Downloads/    下载页面、队列和任务展示
│   ├── Experience/   个性化及体验功能
│   ├── Instances/    版本/实例管理界面
│   ├── Launch/       启动交互
│   ├── Packs/        整合包操作
│   ├── Settings/     设置页面
│   ├── Shell/        主窗口壳层及导航显示
│   └── Support/      诊断和稳定性界面
├── Controllers/      交互流程协调
├── Builders/         控件与页面构造
├── Models/           窗口侧展示状态
├── Presentation/     窗口、对话框及页面视图
├── Styling/          主题和导航样式
└── Initialization/   窗口生命周期与提示初始化
```

`MainWindow.xaml` 与 `MainWindow.xaml.cs` 保持原位置。`UI/` 下的文件仍是同一个 `partial MainWindow`，不是独立窗口；其他组件通过显式依赖或现有窗口入口协作。

## 窗口根类

| 类型 | 说明 | 源码 |
|---|---|---|
| [MainWindow](Classes/MainWindow.md) | 组合窗口服务、处理生命周期并保留 XAML 事件边界；页面功能分布于 UI partial。 | [`MainWindow.xaml.cs`](../../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs) |

## UI partial

以下文件均扩展 `MainWindow` 同一个类型，职责说明集中在 [MainWindow](Classes/MainWindow.md)。

| 目录 | 源文件 |
|---|---|
| `UI/Accounts/` | [AccountUI](../../src/MCUGLauncher/MainWindow/UI/Accounts/AccountUI.cs)、[HomeAccountUI](../../src/MCUGLauncher/MainWindow/UI/Accounts/HomeAccountUI.cs)、[MuaAccountUI](../../src/MCUGLauncher/MainWindow/UI/Accounts/MuaAccountUI.cs) |
| `UI/Content/` | [ContentUI](../../src/MCUGLauncher/MainWindow/UI/Content/ContentUI.cs)、[LocalContentUI](../../src/MCUGLauncher/MainWindow/UI/Content/LocalContentUI.cs)、[ResourceFavoritesUI](../../src/MCUGLauncher/MainWindow/UI/Content/ResourceFavoritesUI.cs)、[ResourceFiltersUI](../../src/MCUGLauncher/MainWindow/UI/Content/ResourceFiltersUI.cs)、[ResourcePresentationUI](../../src/MCUGLauncher/MainWindow/UI/Content/ResourcePresentationUI.cs)、[ResourceSearchLayoutUI](../../src/MCUGLauncher/MainWindow/UI/Content/ResourceSearchLayoutUI.cs)、[ResourceWorkspaceUI](../../src/MCUGLauncher/MainWindow/UI/Content/ResourceWorkspaceUI.cs)、[UpdatesUI](../../src/MCUGLauncher/MainWindow/UI/Content/UpdatesUI.cs)、[ResponsiveResourcePanel](../../src/MCUGLauncher/MainWindow/UI/Content/ResponsiveResourcePanel.cs) |
| `UI/Downloads/` | [DownloadPageUI](../../src/MCUGLauncher/MainWindow/UI/Downloads/DownloadPageUI.cs)、[LoaderOverviewUI](../../src/MCUGLauncher/MainWindow/UI/Downloads/LoaderOverviewUI.cs)、[DownloadQueueUI](../../src/MCUGLauncher/MainWindow/UI/Downloads/DownloadQueueUI.cs)、[DownloadTaskUI](../../src/MCUGLauncher/MainWindow/UI/Downloads/DownloadTaskUI.cs)、[MiniDownloadUI](../../src/MCUGLauncher/MainWindow/UI/Downloads/MiniDownloadUI.cs) |
| `UI/Experience/` | [AppearanceUI](../../src/MCUGLauncher/MainWindow/UI/Experience/AppearanceUI.cs)、[ExperienceUI](../../src/MCUGLauncher/MainWindow/UI/Experience/ExperienceUI.cs) |
| `UI/Instances/` | [CollectionsUI](../../src/MCUGLauncher/MainWindow/UI/Instances/CollectionsUI.cs)、[FeatureUI](../../src/MCUGLauncher/MainWindow/UI/Instances/FeatureUI.cs)、[GameVersionUI](../../src/MCUGLauncher/MainWindow/UI/Instances/GameVersionUI.cs)、[InstanceLibraryUI](../../src/MCUGLauncher/MainWindow/UI/Instances/InstanceLibraryUI.cs)、[SketchUI](../../src/MCUGLauncher/MainWindow/UI/Instances/SketchUI.cs)、[UninstallUI](../../src/MCUGLauncher/MainWindow/UI/Instances/UninstallUI.cs)、[VersionSettingsUI](../../src/MCUGLauncher/MainWindow/UI/Instances/VersionSettingsUI.cs) |
| `UI/Launch/` | [GameLaunchUI](../../src/MCUGLauncher/MainWindow/UI/Launch/GameLaunchUI.cs) |
| `UI/Packs/` | [PackUI](../../src/MCUGLauncher/MainWindow/UI/Packs/PackUI.cs)、[PackUpgradeUI](../../src/MCUGLauncher/MainWindow/UI/Packs/PackUpgradeUI.cs) |
| `UI/Settings/` | [PreferencesUI](../../src/MCUGLauncher/MainWindow/UI/Settings/PreferencesUI.cs) |
| `UI/Shell/` | [CompactShellUI](../../src/MCUGLauncher/MainWindow/UI/Shell/CompactShellUI.cs)、[InlineHostUI](../../src/MCUGLauncher/MainWindow/UI/Shell/InlineHostUI.cs)、[MenuExtensions](../../src/MCUGLauncher/MainWindow/UI/Shell/MenuExtensions.cs)、[MenuExtensionsUI](../../src/MCUGLauncher/MainWindow/UI/Shell/MenuExtensionsUI.cs)、[ModernUI](../../src/MCUGLauncher/MainWindow/UI/Shell/ModernUI.cs) |
| `UI/Support/` | [DiagnosticsUI](../../src/MCUGLauncher/MainWindow/UI/Support/DiagnosticsUI.cs)、[StabilityUI](../../src/MCUGLauncher/MainWindow/UI/Support/StabilityUI.cs) |

## 独立组件

### Controllers

| 类型 | 说明页 | 源码 |
|---|---|---|
| ContentUpdateController | [职责](Classes/ContentUpdateController.md) | [`ContentUpdateController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/ContentUpdateController.cs) |
| FileImportController | [职责](Classes/FileImportController.md) | [`FileImportController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/FileImportController.cs) |
| GameWindowBehaviorController | [职责](Classes/GameWindowBehaviorController.md) | [`GameWindowBehaviorController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/GameWindowBehaviorController.cs) |
| InstanceSearchController | [职责](Classes/InstanceSearchController.md) | [`InstanceSearchController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/InstanceSearchController.cs) |
| InstanceUninstallationController | [职责](Classes/InstanceUninstallationController.md) | [`InstanceUninstallationController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/InstanceUninstallationController.cs) |
| JavaManagerController | [职责](Classes/JavaManagerController.md) | [`JavaManagerController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/JavaManagerController.cs) |
| LoaderInstallationController | [职责](Classes/LoaderInstallationController.md) | [`LoaderInstallationController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/LoaderInstallationController.cs) |
| MicrosoftAccountController | [职责](Classes/MicrosoftAccountController.md) | [`MicrosoftAccountController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/MicrosoftAccountController.cs) |
| NetworkSettingsController | [职责](Classes/NetworkSettingsController.md) | [`NetworkSettingsController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/NetworkSettingsController.cs) |
| PackUpgradeController | [职责](Classes/PackUpgradeController.md) | [`PackUpgradeController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/PackUpgradeController.cs) |
| PageNavigationController | [职责](Classes/PageNavigationController.md) | [`PageNavigationController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/PageNavigationController.cs) |
| PlayerIdentityController | [职责](Classes/PlayerIdentityController.md) | [`PlayerIdentityController.cs`](../../src/MCUGLauncher/MainWindow/Controllers/PlayerIdentityController.cs) |

### Presentation

| 类型 | 说明页 | 源码 |
|---|---|---|
| ContentUpdatePreviewView | [职责](Classes/ContentUpdatePreviewView.md) | [`ContentUpdatePreviewView.cs`](../../src/MCUGLauncher/MainWindow/Presentation/ContentUpdatePreviewView.cs) |
| ContentUpdateSelectionView | [职责](Classes/ContentUpdateSelectionView.md) | [`ContentUpdateSelectionView.cs`](../../src/MCUGLauncher/MainWindow/Presentation/ContentUpdateSelectionView.cs) |
| FeatureDialogBuilder | [职责](Classes/FeatureDialogBuilder.md) | [`FeatureDialogBuilder.cs`](../../src/MCUGLauncher/MainWindow/Presentation/FeatureDialogBuilder.cs) |
| InlinePageHost | [职责](Classes/InlinePageHost.md) | [`InlinePageHost.cs`](../../src/MCUGLauncher/MainWindow/Presentation/InlinePageHost.cs) |
| InstanceLibraryDialog | [职责](Classes/InstanceLibraryDialog.md) | [`InstanceLibraryDialog.cs`](../../src/MCUGLauncher/MainWindow/Presentation/InstanceLibraryDialog.cs) |
| InstancePackExportDialog | [职责](Classes/InstancePackExportDialog.md) | [`InstancePackExportDialog.cs`](../../src/MCUGLauncher/MainWindow/Presentation/InstancePackExportDialog.cs) |
| LaunchOptionsDialog | [职责](Classes/LaunchOptionsDialog.md) | [`LaunchOptionsDialog.cs`](../../src/MCUGLauncher/MainWindow/Presentation/LaunchOptionsDialog.cs) |
| SketchContentManagerDialog | [职责](Classes/SketchContentManagerDialog.md) | [`SketchContentManagerDialog.cs`](../../src/MCUGLauncher/MainWindow/Presentation/SketchContentManagerDialog.cs) |
| VersionSettingsDialog | [职责](Classes/VersionSettingsDialog.md) | [`VersionSettingsDialog.cs`](../../src/MCUGLauncher/MainWindow/Presentation/VersionSettingsDialog.cs) |
| VersionSettingsPageContext | [职责](Classes/VersionSettingsPageContext.md) | [`VersionSettingsPageContext.cs`](../../src/MCUGLauncher/MainWindow/Presentation/VersionSettingsPageContext.cs) |
| VersionSettingsWindowView | [职责](Classes/VersionSettingsWindowView.md) | [`VersionSettingsWindowView.cs`](../../src/MCUGLauncher/MainWindow/Presentation/VersionSettingsWindowView.cs) |

### Builders

| 类型 | 说明页 | 源码 |
|---|---|---|
| ContentSearchFilterBuilder | [职责](Classes/ContentSearchFilterBuilder.md) | [`ContentSearchFilterBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/ContentSearchFilterBuilder.cs) |
| LaunchScriptBuilder | [职责](Classes/LaunchScriptBuilder.md) | [`LaunchScriptBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/LaunchScriptBuilder.cs) |
| LaunchOptionsForm | [职责](Classes/LaunchOptionsForm.md) | [`LaunchOptionsForm.cs`](../../src/MCUGLauncher/MainWindow/Builders/LaunchOptionsForm.cs) |
| OfflineSkinPackBuilder | [职责](Classes/OfflineSkinPackBuilder.md) | [`OfflineSkinPackBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/OfflineSkinPackBuilder.cs) |
| VersionLaunchPageBuilder | [职责](Classes/VersionLaunchPageBuilder.md) | [`VersionLaunchPageBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/VersionLaunchPageBuilder.cs) |
| VersionLaunchScriptExportPageBuilder | [职责](Classes/VersionLaunchScriptExportPageBuilder.md) | [`VersionLaunchScriptExportPageBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/VersionLaunchScriptExportPageBuilder.cs) |
| VersionLoaderPageBuilder | [职责](Classes/VersionLoaderPageBuilder.md) | [`VersionLoaderPageBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/VersionLoaderPageBuilder.cs) |
| VersionModManagementPageBuilder | [职责](Classes/VersionModManagementPageBuilder.md) | [`VersionModManagementPageBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/VersionModManagementPageBuilder.cs) |
| VersionOverviewPageBuilder | [职责](Classes/VersionOverviewPageBuilder.md) | [`VersionOverviewPageBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/VersionOverviewPageBuilder.cs) |
| VersionPackExportPageBuilder | [职责](Classes/VersionPackExportPageBuilder.md) | [`VersionPackExportPageBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/VersionPackExportPageBuilder.cs) |
| WindowChromeBuilder | [职责](Classes/WindowChromeBuilder.md) | [`WindowChromeBuilder.cs`](../../src/MCUGLauncher/MainWindow/Builders/WindowChromeBuilder.cs) |

### Models

| 类型 | 说明页 | 源码 |
|---|---|---|
| BookmarkView | [职责](Classes/BookmarkView.md) | [`BookmarkView.cs`](../../src/MCUGLauncher/MainWindow/Models/BookmarkView.cs) |
| DownloadCategoryIcon | [职责](Classes/DownloadNavigation.md) | [`DownloadNavigation.cs`](../../src/MCUGLauncher/MainWindow/Models/DownloadNavigation.cs) |
| InstanceTarget | [职责](Classes/InstanceTarget.md) | [`InstanceTarget.cs`](../../src/MCUGLauncher/MainWindow/Models/InstanceTarget.cs) |
| InstanceView | [职责](Classes/InstanceView.md) | [`InstanceView.cs`](../../src/MCUGLauncher/MainWindow/Models/InstanceView.cs) |
| LauncherMenuArea、LauncherMenuContext、LauncherMenuPage、LauncherMenuRegistry | [职责](Classes/LauncherMenuRegistry.md) | [`LauncherMenuRegistry.cs`](../../src/MCUGLauncher/MainWindow/Models/LauncherMenuRegistry.cs) |
| PlayerIdentityState | [职责](Classes/PlayerIdentityState.md) | [`PlayerIdentityState.cs`](../../src/MCUGLauncher/MainWindow/Models/PlayerIdentityState.cs) |
| SaveChoice | [职责](Classes/SaveChoice.md) | [`SaveChoice.cs`](../../src/MCUGLauncher/MainWindow/Models/SaveChoice.cs) |
| VersionModView | [职责](Classes/VersionModView.md) | [`VersionModView.cs`](../../src/MCUGLauncher/MainWindow/Models/VersionModView.cs) |
| VersionRow | [职责](Classes/VersionRow.md) | [`VersionRow.cs`](../../src/MCUGLauncher/MainWindow/Models/VersionRow.cs) |

### Styling 与 Initialization

| 类型 | 说明页 | 源码 |
|---|---|---|
| ModernThemeStyler | [职责](Classes/ModernThemeStyler.md) | [`ModernThemeStyler.cs`](../../src/MCUGLauncher/MainWindow/Styling/ModernThemeStyler.cs) |
| NavigationMenuStyle | [职责](Classes/NavigationMenuStyle.md) | [`NavigationMenuStyle.cs`](../../src/MCUGLauncher/MainWindow/Styling/NavigationMenuStyle.cs) |
| WindowThemeStyler | [职责](Classes/WindowThemeStyler.md) | [`WindowThemeStyler.cs`](../../src/MCUGLauncher/MainWindow/Styling/WindowThemeStyler.cs) |
| OriginalTheme（嵌套类型） | [职责](Classes/OriginalTheme.md) | [`WindowThemeStyler.cs`](../../src/MCUGLauncher/MainWindow/Styling/WindowThemeStyler.cs) |
| LauncherTooltipInitializer | [职责](Classes/LauncherTooltipInitializer.md) | [`LauncherTooltipInitializer.cs`](../../src/MCUGLauncher/MainWindow/Initialization/LauncherTooltipInitializer.cs) |
| UtilityWindowInitializer | [职责](Classes/UtilityWindowInitializer.md) | [`UtilityWindowInitializer.cs`](../../src/MCUGLauncher/MainWindow/Initialization/UtilityWindowInitializer.cs) |
