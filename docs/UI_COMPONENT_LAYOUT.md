# 界面组件布局与解耦

## 本次调整：只移动目录

```text
src/MCUGLauncher/
├── MainWindow/       主窗口、UI partial、构建器、展示模型、样式和初始化
├── Builders/         共享启动脚本与离线皮肤包生成（无窗口依赖）
├── Controllers/      12 个交互控制器
├── Presentation/     11 个对话框、视图和页面上下文类
├── Versions/         版本业务服务
├── Resources/        资源业务服务
├── Downloads/        统一下载控制、暂停存储与任务执行器
├── Front/Hosting/    新 Front 的真实模块组装、配置锁与生命周期（不创建 MainWindow）
├── Front/Front.csproj 独立新前端，只引用 Application 和 SharedUI
├── Front/Views/      真实账号/实例/内容页面及 Front 窗口
├── Launch/           启动编排与游戏会话监控
├── Settings/         设置模型、持久化、网络策略与设置校验服务
└── Accounts/         账号会话及提供方抽象
```

原 MainWindow 子目录中的 Controllers 和 Presentation 共 23 个源文件原样迁移。
后续项目级拆分已实施：共享业务由 Application 链接编译，下载 UI 位于 `src/MCUGLauncher.SharedUI/Downloads/`。
主项目排除 Front 及共享业务源码的重复编译；最新项目图见 [FRONTEND_PROJECTS.md](dcg/FRONTEND_PROJECTS.md)。下述原样迁移说明是历史步骤。
保留现有工作区内的代码修改，不重置为 Git 基线；命名空间、访问级别、构造函数和方法均不改变。
MainWindow 继续按原方式创建、持有和调用组件，原有组件间调用也不变。
不移动 XAML，不调整事件绑定、资源 URI 或程序集名称。

主项目使用 SDK 默认的源码包含规则，迁移后的 `.cs` 文件仍在同一个项目和命名空间中，
不需要新增项目引用或改变 `using`。显式文档源码链接及目录说明同步修改。

## 验证边界

共享 Builder 的迁移范围、保留在旧前端的 UI 构建器及独立测试入口见 [BUILDERS_MODULE.md](dcg/BUILDERS_MODULE.md)。

- 对迁移前工作区的 23 个文件记录 SHA-256，与新路径逐一对比，确认没有修改源码内容。
- 检查旧路径残留、文档源码链接、项目中的显式资源/源码引用，以及旧目录无重复源文件。
- 运行 `python3 scripts/check-layout.py` 和 `git diff --check`。
- 上述都是静态检查，不能替代编译和界面运行。本次环境缺少 .NET SDK，且不是 Windows，不能完成 WPF 界面验证。

设置校验、下载队列协调、账号会话统一和游戏退出监控已迁入不访问控件的服务；账号 OAuth/MUA 用户交互、`DispatcherTimer` 和窗口响应仍由 UI 承担。

在配置 .NET SDK 8.0.426 的 Windows 环境继续运行：

```powershell
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --management
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --executor
```

之后验证：版本设置和启动设置、内容更新预览、卸载确认与取消、账号管理、内嵌页面返回/焦点恢复。
迁移不改变这些逻辑，但未运行之前不能宣称这些界面验证通过。

## 第四步：逐个减少窗口依赖（内容更新首轮已实施）

目标是移除对具体 MainWindow 的依赖，不是让所有呈现代码都不依赖 WPF。
MainWindow 负责组装，Controller 负责交互流程，业务服务负责业务，Presentation 负责输入和结果展示。

### 1. 先处理内容更新控制器

`ContentUpdateController` 首轮已完成：直接注入 ResourceManagementService，通过 IContentUpdateInteraction 和
IContentUpdateSelection 对接任务和呈现，不再引用 MainWindow、System.Windows 或具体视图。
具体边界、验证入口见 [内容更新解耦](CONTENT_UPDATE_DECOUPLING.md)。下述其余组件仍是后续方案。
构造函数直接注入该服务；执行一次操作时传入不可变目标快照（根目录、实例、内容目录、游戏版本、加载器）。
为选择更新、确认预览、错误与结果展示建立小型交互接口，不暴露控件或 Preferences。
任务执行和下载入队通过明确的适配接口或委托接入现有 Run / OperationCoordinator。
保留原来的忙碌门禁、取消令牌、内容指纹、提交边界和刷新策略。

不要建立含数十个窗口属性的通用 IMainWindow；这只是把原有耦合改成接口形式。

### 2. 将对话框改为数据输入、结果输出

优先保留已接收数据/回调的 ContentUpdateSelectionView、ContentUpdatePreviewView 设计，
逐步把 LaunchOptionsDialog、VersionSettingsDialog 改为接收数据快照和保存/确认回调。
窗口宿主、主题配置和对话框工厂可以是 WPF 依赖，但不能借此任意读取 MainWindow 业务状态。
通用按钮/标题工厂可移入独立 UI 辅助类，避免仅为调用 MainWindow 的静态构建方法而依赖窗口类型。

### 3. 再改造加载器、卸载和其他交互

加载器选择只需版本服务、目标快照、选择结果和入队能力，改造范围较小。
卸载需要保留实例身份复核、共享目录确认、关联任务取消及最终刷新；应在测试覆盖后单独迁移。
账号、Java、网络和整合包升级分别评估自己的状态和持久化边界，不一次性改完。

### 4. 最后处理页面宿主

InlinePageHost 直接组织主窗口布局、写回窗口字段并管理导航和焦点，是风险较高的改造点。
可改为接收明确的布局容器、导航状态与窗口生命周期适配，并返回所创建的视图引用，
而不是直接读写 MainWindow 字段；必须验证返回栈、焦点、关闭和忙碌时的行为。

## 可行性

内容更新、加载器选择和数据驱动视图的可行性高：现有业务服务与部分回调接口已经可复用。
版本设置、启动设置和卸载的可行性高，但需拆清配置快照、保存及安全校验职责。
页面宿主、账号和整合包流程也能改造，但涉及生命周期或更多共享状态，应后置逐个验证。
PageNavigationController、PlayerIdentityController 已主要使用显式控件和回调，
GameWindowBehaviorController 接收通用 Window；不应为了统一形式强制重写已经合理的依赖。
