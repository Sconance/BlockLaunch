# 当前架构解耦报告

更新时间：2026-10-29  
分支：`refactor-dcg`

## 结论

当前项目处于“双前端过渡阶段”：业务层已经基本从 UI 中移出，旧 UI 和新 Front 通过同一套业务模块工作；UI 层仍未完全收敛，因此当前不能删除 `MainWindow`。

本报告记录的是代码结构状态，不代表 Windows 构建、运行或视觉验收已经通过。本轮 Front 下载页和资源详情的改动仍需在 Windows 上编译验证。

## 当前依赖关系

```text
旧 UI
└── src/MCUGLauncher/MCUGLauncher.csproj
    ├── MainWindow/                 旧 WPF 页面、弹窗和控制器
    ├── MainWindow.xaml
    └── App.xaml
            │
            ├── 调用
            ▼
新 Front
└── src/MCUGLauncher/Front/Front.csproj
            │
            ├── 共同使用
            ▼
    MCUGLauncher.Application
            │
            ├── 共同使用
            ▼
    MCUGLauncher.Core

旧 UI 和 Front 均可使用：
├── Accounts/          账号会话、皮肤和登录
├── Authentication/    认证相关边界
├── Downloads/         下载队列、暂停、恢复和执行
├── Resources/         资源搜索、版本、下载和安装
├── Settings/          偏好、网络和 Java 设置
├── Versions/          版本、加载器和实例维护
├── Launch/             启动运行时和业务协调
├── Contracts/         前端接口和数据模型
└── Adapters/          业务模型到 Front 模型的适配
```

## 已完成的解耦

### 1. Front 独立项目

`src/MCUGLauncher/Front/Front.csproj` 只引用 `MCUGLauncher.Application` 和 `MCUGLauncher.SharedUI`，Front 源码不直接引用 `MainWindow`。

Front 已拥有自己的：

- WPF 窗口和导航：`Front/Views/FrontWindow.xaml`
- 启动页面：`Front/Views/LauncherView.xaml`
- 下载页面：`Front/Views/DownloadBrowserView.xaml`
- 资源中心：`Front/Views/ResourceBrowserView.xaml`
- 品牌资源：`Front/Assets/MCUGLogo.png`、`Front/Assets/MCUGLauncher.ico`

### 2. 业务模块同级化

账号、下载、资源、设置、版本和启动业务已经放在 `MainWindow` 同级目录。Front 通过 `Contracts` 调用 `LauncherRuntime`，不需要把业务逻辑写入 XAML 页面。

### 3. 下载和资源功能复用

Front 下载页现在使用共享版本、加载器、整合包和资源服务；资源详情包含搜索、筛选、收藏、卡片/列表、版本、依赖、外部下载和实例安装入口。旧 UI 的资源介绍读取也改为复用 `ResourceManagementService`。

### 4. 品牌资源独立

Front 不再依赖旧 UI 的 `Assets` 路径。Logo 和程序图标已复制到 Front 的资源目录并在 `Front.csproj` 中声明。

## 尚未完成的解耦

### 1. `LauncherRuntime` 仍是集中式协调入口

`src/MCUGLauncher/Launch/LauncherRuntime.cs` 当前约 899 行，同时实现多个 Front 接口，负责协调账号、下载、资源、版本、设置和启动。它已经脱离 Front 目录，但仍可以进一步拆成按领域组合的运行时服务。

这属于架构优化，不代表账号、下载或资源功能没有实现。

### 2. 旧 UI 仍有独有页面逻辑

`src/MCUGLauncher/MainWindow/` 仍包含旧 UI 的页面布局、弹窗、菜单、资源详情和部分交互。Front 已重新实现对应页面，但两套 UI 尚未共享同一套视觉控件。

### 3. 扩展菜单尚未完全统一

旧 UI 使用 `LauncherMenuRegistry`，Front 使用 `FrontMenuRegistry`。Front 的注册机制已经存在，但旧扩展工厂依赖 `MainWindow`，还不能直接迁移到 Front。

### 4. 运行时验收尚未完成

本轮未在开发环境执行 .NET 构建或 WPF 运行。必须在 Windows 上验证：

- Front 和旧 UI 是否都能构建
- Front Logo、标题栏和“更多”入口是否正常
- 下载分类、版本分组和加载器操作是否正常
- 资源详情、收藏、依赖解析和下载队列是否正常
- 删除旧 UI 的临时隔离构建是否通过

## `MainWindow` 删除条件

只有满足以下条件后才建议删除：

1. Front Windows 构建和发布成功。
2. Front 完成账号、设置、下载、资源、版本、实例、Java、存档和诊断验收。
3. 旧 UI 的配置、下载队列和数据兼容性确认不再需要。
4. `check-frontend-boundaries.py --build` 验证 Front 删除旧 UI 后仍能独立构建。
5. 删除旧入口后同步清理旧 UI 专用引用、资源和文档。

## 当前建议

保留 `src/MCUGLauncher/MainWindow/`，先完成 Windows 构建和验收；验收通过后，再进行旧 UI 清退和 `LauncherRuntime` 的进一步拆分。
