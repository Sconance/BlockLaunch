# 项目架构说明

本文描述 `refactor` 分支 0.21.0 的实际代码结构，以提交 `db0ab6a` 为阅读基线。
它是现状说明，不表示待办中的前后端分离已经完成。

补充：启动编排已提取到同级 `Launch/` 功能目录，具体边界见 [启动模块说明](LAUNCH_MODULE.md)。
下载任务执行已提取到同级 `Downloads/`，详见 [下载执行器说明](DOWNLOAD_EXECUTOR.md)。
版本查询/目标解析与资源管理已提取到同级 `Versions/`、`Resources/`，详见 [模块说明](VERSION_RESOURCE_MODULES.md)。

## 1. 架构概览

MCUG Launcher 是 Windows 桌面 Minecraft Java 版启动器，采用 C#、.NET 8 和 WPF。
当前不是 Web 前后端架构，也不是完整的 MVVM 或严格的清洁架构，而是：

**WPF 窗口与分域 UI + 交互控制器及呈现组件 + 独立 Core 业务库。**

```text
MCUGLauncher（WPF 应用）
│
├── App.xaml / App.xaml.cs             启动配置与应用级样式
├── MainWindow.xaml                   窗口布局、命名控件和事件绑定
├── MainWindow/MainWindow.xaml.cs     服务组装、生命周期与窗口共享状态
│   ├── UI/                          各业务区域的窗口分部实现
│   ├── Builders/                    页面、表单与脚本构建
│   ├── Models/                      展示模型及菜单注册
│   ├── Styling/                     主题与控件样式处理
│   └── Initialization/              界面初始化辅助
├── Controllers/                     交互流程和操作协调；部分仍持有窗口
├── Presentation/                    对话框、页面宿主与呈现上下文
├── Authentication/                  微软、MUA 认证及凭据和头像处理
├── Launch/                          独立启动编排、交互接口和输入/结果模型
├── Downloads/                       独立下载任务执行器；队列控制暂留窗口
├── Versions/                        版本查询、实例目标、加载器与配置/卸载服务入口
├── Resources/                       资源搜索、安装准备、本地内容与更新回退服务
├── Front/                           独立界面迁移预览；目前为外壳和只读资源页
├── Contracts/                       Front 调用的框架无关接口与数据模型
├── Adapters/                        将界面契约映射到现有业务服务
│
└── 引用 MCUGLauncher.Core
    ├── Application/                 启动、Java、任务和实例等用例
    ├── Domain/                      配置、实例、任务及收藏模型
    ├── Infrastructure/              网络、可靠下载、文件事务与恢复
    ├── ContentService.cs            资源查询与内容管理
    ├── GameInstaller.cs             游戏和加载器安装
    └── PackService.cs               整合包处理
```

## 2. 项目与依赖方向

| 项目或目录 | 职责 | 关键边界 |
| --- | --- | --- |
| `src/MCUGLauncher/` | 窗口、认证、交互和应用组装 | 引用 Core，依赖 WPF |
| `src/MCUGLauncher.Core/` | 游戏、资源、任务和文件操作 | 不反向引用主程序 |
| `test/` | 22 个功能、界面及集成检查项目 | 检查方式包括模拟 HTTP、反射和真实环境集成 |
| `tools/` | 5 个界面预览项目和 SourceFormatter | 开发辅助，不是启动器业务依赖 |
| `scripts/`、`docs/` | 构建、验证和文档 | 不参与运行时业务调用 |

主程序引用 `CmlLib.Core.Auth.Microsoft 3.3.0`；Core 引用 `CmlLib.Core 4.1.0`。
两个主体项目都以 `net8.0-windows` 为目标。Core 没有启用 WPF，但不能仅凭项目独立
就认为它已经具备完整跨平台能力。

项目和产品已更名为 MCUG Launcher，现有代码命名空间仍主要使用 `BlockLaunch`。
名称变化不意味着必须改写既有配置、账号或实例数据格式。

## 3. 程序启动流程

```text
WPF 生成的应用入口
  → App.xaml：StartupUri="MainWindow.xaml"
  → MainWindow 构造函数
      → 加载编译后的 XAML
      → 创建页面宿主、控制器和构建器
      → 加载设置并取得配置目录的队列锁
      → 创建 OperationCoordinator 和网络客户端
      → 初始化 MinecraftLauncher、页面、主题和菜单
  → 窗口 Loaded
      → 按需处理 --login
      → 执行启动恢复
      → 处理脚本启动参数
      → 启动下载队列
```

主要入口是 [`MainWindow.xaml.cs`](../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs)。
该文件使用 `Application.LoadComponent` 加载窗口 XAML，并提供类型化的命名控件访问入口。
窗口仍承担应用级组装职责，页面初始化顺序与控件父子关系存在关联，不能任意重排。

关闭窗口时，如果仍有操作在执行，会请求取消并等待任务清理，不是直接终止业务进程。

## 4. 界面层如何协作

### UI：按业务组织的窗口分部类

`MainWindow/UI/` 按账号、下载、资源、实例、整合包、外观和支持等功能分组。
不少文件仍声明 `partial MainWindow`，因此它们在编译后属于同一个类，能够共享字段和方法。
目录分组改善了定位，但没有自动形成独立的状态边界。

### Controllers：抽离交互流程

控制器处理登录、导航、Java 管理、导入、内容更新等流程。
当前有两类依赖方式：

- 接收整个 `MainWindow`：如 `ContentUpdateController`，通过窗口调用任务入口、读取状态并刷新界面。
- 接收指定控件和回调：如 `PlayerIdentityController`、`PageNavigationController`，依赖范围更明确。

它们仍是桌面交互组件，不能直接当作可被 Web 前端调用的后端 API。

### Builders 与 Presentation：构建和呈现

Builders 负责表单、版本页面、窗口标题栏及启动脚本等构建工作。
Presentation 负责内嵌页面栈、实例管理和更新确认等呈现过程。
两者是职责划分，不代表所有类型都完全无状态或脱离 WPF。

### 菜单扩展

`LauncherMenuRegistry` 提供八个菜单区域的页面注册、排序和变更通知。
页面工厂接收 `LauncherMenuContext`，返回 WPF `FrameworkElement`。
它是源码级扩展点，不是动态加载第三方插件的系统，也不是跨前端的通用菜单协议。

## 5. Core 层与关键流程

### 游戏启动

```text
界面选择实例和启动设置
  → MainWindow.Run / Launch 转发
  → GameLaunchController 通过交互接口准备设置、账号与输入
  → 独立编排实例配置、预检、认证组件和 LaunchService
  → GameSession 启动；通知窗口接收会话
  → 窗口沿用日志、退出诊断和窗口行为组件
```

认证服务目前位于主程序的 `Authentication/`，而不是全部位于 Core。
Core 的启动请求还包含 CmlLib 的会话类型，说明第三方依赖尚未完全封装在适配层内部。

### 下载与安装

```text
UI / Controller 创建 DownloadRequest
  → OperationCoordinator 入队、记录状态并协调执行
  → 窗口下载队列调用 DownloadTaskExecutor 分派具体任务
  → GameInstaller / ContentService / PackService 等执行
  → 可靠下载、哈希校验、事务和安装恢复组件
  → 任务事件通过 Dispatcher 更新界面
```

任务模型在 `Domain/Operations.cs`；协调器、执行上下文与提交边界分别位于
`Application/OperationCoordinator.cs`、`OperationContext.cs` 和 `OperationExecution.cs`。
提交边界通过 `AsyncLocal` 传递，是隐式的执行上下文依赖，重构取消与任务流程时需要保留其语义。

### 配置与持久化

`Preferences`、实例描述、收藏和任务记录构成主要持久化模型；文件写入、事务和恢复由相关组件协作完成。
`Domain/` 中仍包含 `SettingsStore`、`InstanceCatalog` 等带文件操作的类型，
因此目前并非严格的“领域层不依赖基础设施”结构。

账号凭据使用认证相关存储组件管理，不应当作普通界面状态、日志或导出参数传递。

## 6. 当前耦合与改造边界

| 方面 | 现状 | 对后续改动的影响 |
| --- | --- | --- |
| 项目级依赖 | 主程序单向引用 Core | Core 可独立维护，不需把业务全部放回窗口 |
| 窗口状态 | 多个 partial 与控制器共享窗口状态 | 改页面和状态时需核对跨模块影响 |
| 控制器边界 | 部分直接接收 MainWindow | 抽类不等于前后端分离 |
| 认证与第三方类型 | 认证仍在 UI 项目，启动参数暴露 CmlLib 类型 | 更换前端前应先定义应用用例与数据边界 |
| 检查程序 | 部分依赖窗口私有字段及反射 | 改名、拆类后需同步调整检查入口 |

若继续使用 WPF，可在现有分层上逐步改进；若改为 Tauri 等 Web 界面，应先提取
不接触 WPF 控件的账号、下载和启动用例，再定义请求、结果、进度与取消协议。
只迁移 `UI/` 文件或将窗口方法包装为接口，并不能消除对窗口状态的依赖。

## 7. 构建、发布与验证

- `scripts/build.ps1`：Release 构建；加 `-Publish` 时生成完整版与 Lite 版。
- 完整版：自包含、单文件、Windows x64，携带 .NET 运行时。
- Lite 版：框架依赖发布，需要系统安装 .NET 8 Desktop Runtime x64。
- `scripts/check-layout.py`：静态检查项目、引用及资源路径，不代表编译或功能通过。
- `scripts/run-checks.ps1`：运行指定检查；真实游戏与账号流程需额外环境和明确验证。

GitHub Actions 当前配置执行布局检查、源码格式检查、主程序构建和一组隔离回归。
这不等于每次都完成真实微软/MUA 授权、游戏启动矩阵或服务器联机验收。

## 8. 推荐阅读顺序

1. [`开发导览`](DEVELOPMENT.md)：按功能定位入口。
2. [`主窗口入口`](../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs)：理解组装与生命周期。
3. [`窗口组件索引`](MainWindow/README.md)：查找控制器、页面和构建器。
4. [`Core 启动服务`](../src/MCUGLauncher.Core/Application/LaunchService.cs)：阅读业务调用边界。
5. [`构建与验证说明`](BUILD.md) 和对应 `test/` 项目：理解环境要求及检查范围。
