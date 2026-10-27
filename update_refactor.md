# 重构分支改动记录

本文记录 `refactor` 分支中 MainWindow 拆分与上游功能整合的进展。已提交部分以分支历史为准，工作区部分在提交后转为本节记录。

## 分支定位

- 远程：`origin` 指向 `https://github.com/Sconance/MCUGLauncher`，`refactor` 分支从 `rebuild` 的组件拆分结果继续。
- 上游对照：`origin/develop`（0.20.1 / 0.20.2 功能线）。差异与迁移映射见 [develop 与 refactor 功能对照](compare_develop_refactor.md)。

## 已提交的组件拆分

相对早期基线已有以下提交（摘录）：

| 提交 | 改动 |
| --- | --- |
| `34582d6` | 将离线皮肤包准备逻辑提取为 `OfflineSkinPackBuilder`。 |
| `fbecfd0` | 将启动脚本生成逻辑提取为 `LaunchScriptBuilder`。 |
| `41ba613` | 将游戏窗口行为管理提取为 `GameWindowBehaviorController`。 |
| `5d9c21b` | 将通用功能对话框构建逻辑提取为 `FeatureDialogBuilder`。 |
| `8e1b89e` | 将启动选项页面与设置交互提取为 `LaunchOptionsDialog`。 |
| `6c4ef41` | 将实例整合包导出流程提取为 `InstancePackExportDialog`。 |
| `c1b2601` | 将草图式实例内容管理页面提取为 `SketchContentManagerDialog`。 |
| `c7dbf71` | 修正 UI partial 的 XAML 生成成员误报，并补充分支文档。 |

对应类型的职责、入口及协作说明见 [MainWindow 组件文档目录](docs/MainWindow/README.md)。

## 本次改动（develop 功能按新架构整合）

- **资源工作区**：新增 `UI/Content/ResourceWorkspaceUI.cs`（详情改为资源页内视图）、`ResourceSearchLayoutUI.cs`（搜索与筛选布局）、`ResourcePresentationUI.cs`（项目介绍、图片、版本与依赖摘要）、`ResponsiveResourcePanel.cs`（自适应卡片面板）。
- **收藏与排序**：新增 `UI/Content/ResourceFavoritesUI.cs`，复用既有 `ResourceBookmarkStore` 数据；`ContentService.Search` 增加可选排序重载，旧签名保持 relevance 行为。
- **加载器概览**：新增 `UI/Downloads/LoaderOverviewUI.cs`，把 Forge / NeoForge / Fabric / Quilt 版本读取与安装队列接入下载页；`DownloadPageUI` 改为调用该概览并接入分类图标。
- **菜单扩展**：新增 `Models/LauncherMenuRegistry.cs`、`UI/Shell/MenuExtensions.cs`、`UI/Shell/MenuExtensionsUI.cs`，覆盖八个区域；版本设置窗口与实例内容对话框接入各自的临时菜单。
- **公共样式与品牌**：`App.xaml` 增加搜索框模板、资源卡片/列表模板和详情标签样式；应用图标与 Logo 更新为 0.20.2 资源；版本号、User-Agent 与诊断信息同步到 0.20.2。
- **文档**：新增 [compare_develop_refactor.md](compare_develop_refactor.md)、[docs/MENU_EXTENSIONS.md](docs/MENU_EXTENSIONS.md)，并更新 README、CHANGELOG、`docs/DEVELOPMENT.md`、组件索引与逐类说明。

迁移保持 `MainWindow` 的 XAML 类型化控件入口与 `UI/` 分域结构，没有把 develop 的旧根目录 partial 覆盖到当前实现。

## 验证

- Release 构建：0 警告、0 错误。
- 常规检查 21 项通过：Auth、Mua、Collection、ContentSafety、Dependency、Diagnostic、Download、MiniDownload、Queue、ResourceDownload、Library、Stability、Uninstall、Upgrade、Experience、VersionUi、WorkflowUi、Compatibility、InlineUi、DownloadUi、CompactUi。
- `DownloadUiChecks` 覆盖资源详情布局切换、收藏筛选、菜单注册与下载扩展；`CompactUiChecks` 使用真实清单与 Modrinth 数据验证卡片列数、收藏筛选与加载器队列。
- SourceFormatter、项目布局检查、`git diff --check` 通过。
- 未执行 `IntegrationChecks`（需要已安装游戏、Java 与真实客户端矩阵）及真实微软/MUA 授权。
