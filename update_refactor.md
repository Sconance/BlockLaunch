# 重构分支改动记录

本文记录 `rebuild` 分支中 MainWindow 拆分相关的已提交工作，以及当前工作区内尚未提交的配套修复。分支提交记录以 `origin/rebuild` 为比较基准。

## 已提交的组件拆分

截至当前分支头 `c1b2601`，相对 `origin/rebuild` 已有以下 7 个提交：

| 提交 | 改动 |
| --- | --- |
| `34582d6` | 将离线皮肤包准备逻辑提取为 `OfflineSkinPackBuilder`。 |
| `fbecfd0` | 将启动脚本生成逻辑提取为 `LaunchScriptBuilder`。 |
| `41ba613` | 将游戏窗口行为管理提取为 `GameWindowBehaviorController`。 |
| `5d9c21b` | 将通用功能对话框构建逻辑提取为 `FeatureDialogBuilder`。 |
| `8e1b89e` | 将启动选项页面与设置交互提取为 `LaunchOptionsDialog`。 |
| `6c4ef41` | 将实例整合包导出流程提取为 `InstancePackExportDialog`。 |
| `c1b2601` | 将草图式实例内容管理页面提取为 `SketchContentManagerDialog`。 |

对应类型的职责、入口及协作说明见 [MainWindow 组件文档目录](docs/MainWindow/README.md)。

## 当前工作区改动（尚未提交）

- 处理本次涉及的 `MainWindow` partial 中语言服务无法识别 WPF XAML 生成成员的问题：通过类型化的 `FindXamlControl<T>` 入口按名称获取并校验控件类型，避免这些 partial 直接引用 XAML 生成字段。
- 通过 `Application.LoadComponent` 加载编译后的窗口 BAML，避免代码直接依赖生成的 `InitializeComponent` 方法；使用初始化就绪标记防止 XAML 加载期间事件提前访问控件。
- 将账号、内容、下载、体验、实例、启动、整合包、设置、Shell 和 Support 等 UI partial 的相关控件访问统一到窗口入口。
- 修复构建器、控制器和展示组件中对已迁移属性或成员的引用，维持拆分后的类型边界。

涉及 `MainWindow.xaml.cs`、`Builders/VersionModManagementPageBuilder.cs`、`Controllers/MicrosoftAccountController.cs`、`Presentation/InstanceLibraryDialog.cs`、`Presentation/LaunchOptionsDialog.cs`，以及 `UI/Accounts`、`Content`、`Downloads`、`Experience`、`Instances`、`Launch`、`Packs`、`Settings`、`Shell`、`Support` 中相应的 partial 文件。

## 当前验证

- Release 构建成功，0 个警告、0 个错误。
- `InlineUiChecks` 全部通过；该检查覆盖模拟与合成 UI 流程，不代表真实微软/MUA 授权验证。
- SourceFormatter、项目布局检查及 `git diff --check` 通过。
- VS Code Problems 对本次检查范围和全工作区均未报告错误。

以上工作区改动在写本文时尚未提交；提交前应再次确认工作区差异和验证结果。
