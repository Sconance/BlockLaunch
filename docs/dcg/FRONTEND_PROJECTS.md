# 双前端独立项目

可视化关系见 [当前项目架构图（Mermaid）](ARCHITECTURE.md)。

## 当前项目图

```text
MCUGLauncher.csproj（旧窗口） ─┬─→ MCUGLauncher.Application ─→ MCUGLauncher.Core
                             └─→ MCUGLauncher.SharedUI ───→ Application
Front/Front.csproj（新窗口） ─┬─→ MCUGLauncher.Application
                            └─→ MCUGLauncher.SharedUI
```

两个可执行项目互不引用。Application 和 SharedUI 不引用任一前端项目。
保留 BlockLaunch 命名空间和已有配置/凭据路径，不复制账号、任务或业务实现。

## 目录与编译所有权

| 路径 | 编译所有者 | 职责 |
|---|---|---|
| `src/MCUGLauncher/Front/` | Front.csproj | Program、App、Styles、Views、ViewModels、Hosting，新前端的完整入口 |
| `src/MCUGLauncher/MainWindow/`、Controllers、Presentation、旧 App/InlineDialog | MCUGLauncher.csproj | 旧前端及尚未迁出的旧交互流程 |
| `src/MCUGLauncher/Accounts/`、Adapters、Builders、Contracts、Downloads、Launch、Resources、Settings、Versions | Application.csproj | 链接编译无窗口业务源码，继续与 MainWindow 同级放置 |
| `src/MCUGLauncher/Accounts/` 下的账号服务、认证策略、受保护存储与所有权校验；Brand.cs | Application.csproj | 共享账号认证和品牌信息；不包含旧授权窗口及 WPF 头像渲染，详见 [账号与认证模块](ACCOUNTS_MODULE.md) |
| `src/MCUGLauncher.SharedUI/Downloads/` | SharedUI.csproj | 中立的下载列表、卡片及 ViewModel，两个前端均可引用 |

旧项目显式排除 Front 与共享业务源码，避免重复类型和重复编译。
Application 不启用 WPF；但仍面向 Windows，认证依赖包含 WebView2 运行库探测，这不是跨平台改造。
旧交互控制器可以暂时留在旧项目，不能让 Application 为它们重新依赖旧窗口。

## Front 自有资源与调用

入口已从 `tools/FrontPreview` 迁到 `Front/Program.cs`，历史工程删除。
`Front/App.xaml` 合并自身 `Styles/Theme.xaml`，主题编译进 MCUGLauncher.Front，不再读取旧入口 App.xaml 或复制 Fixtures。
`Hosting/FrontLauncherRuntime` 通过 Application 的公开接口/类型组装业务；窗口交互由 Front 自己实现。
共享启动控制器、交互契约与启动数据已公开，未使用友元程序集绕过项目边界。
两套授权 UI 都调用 Application 中的 `MicrosoftAuthorizationPolicy`，不再跨前端引用校验逻辑。
两套设置 UI 都调用同级 `Settings/` 的模型、存储和校验服务，详见 [设置模块](SETTINGS_MODULE.md)。
下载共享控件不读取任一前端的命名资源，删掉 Front 目录不会使旧窗口失去下载组件。

## 什么可以删除

- 移除 `src/MCUGLauncher/Front/`：旧前端及共享模块可以继续构建。
- 移除旧项目、MainWindow、Controllers、Presentation、旧 App/InlineDialog/InlineFilePicker、旧授权窗口和头像渲染：Front 可以继续构建。
- **不能删除整个 `src/MCUGLauncher/`**：这个容器目录仍放置共享业务源码和新 Front，不能把目录名当作旧前端边界。
- Front 独立构建不代表旧前端全部功能已经迁移；升级/回退、卸载及个性化等完整页面仍需逐项接入。
- 本轮不删除实际旧前端，也不自动提交或推送；删除测试仅在临时副本进行。

## 构建与边界验收

Windows x64 安装 global.json 指定的 .NET SDK 8.0.426（允许同 feature band 补丁更新）。从仓库根目录执行：

```powershell
.\scripts\build-windows.ps1 -Frontend Both
dotnet build src/MCUGLauncher/Front/Front.csproj -c Release
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
```

两个输出分别是 MCUGLauncher.Front.exe 和 MCUGLauncher.exe。分发时保留相应完整输出目录。
Front 默认使用正式配置并恢复下载；界面验收前阅读 [FRONT_RUNTIME.md](FRONT_RUNTIME.md)，不要直接操作重要游戏目录。

安装 Python 3 后运行可重复的独立性验证：

```powershell
python scripts/check-layout.py
python scripts/check-frontend-boundaries.py
python scripts/check-frontend-boundaries.py --build
```

最后一条仅在自行创建的临时副本中操作：删除旧前端后构建 Front，再删除 Front 后构建旧项目。
它排除 bin/obj，避免沿用之前的输出，复制 SDK 固定文件，还原失败或构建失败均返回非零。
原仓库源码不删除；临时副本退出后自动清理。可用 `--dotnet <SDK可执行路径>` 指定 SDK。

## 2026-10-29 开发端验证边界

开发端 Linux 使用临时 SDK 8.0.426，所有 WPF 编译命令附加 `-p:EnableWindowsTargeting=true`。
这是实际交叉编译，不是 Windows 原生启动验收。分支 `refactor-dcg`，基线提交 `bd861ff`，验证包含未提交工作区修改。
Windows 的窗口显示、主题、导航、授权、关闭及游戏运行仍按 FRONT_RUNTIME.md 验收，不能把编译成功记为界面通过。

### 已执行结果

| 检查 | 结果 |
|---|---|
| 新 Front Release 交叉编译 | 通过；最终由 FrontRuntimeChecks 项目引用构建，0 警告、0 错误 |
| 旧项目 Release 交叉编译 | 通过；0 警告、0 错误 |
| `check-frontend-boundaries.py --build` | 退出码 0；临时副本删除旧前端后 Front 成功，删除 Front 后旧入口成功，两次最终构建均 0 警告、0 错误 |
| `dotnet build test/FrontRuntimeChecks/FrontRuntimeChecks.csproj -c Release -p:EnableWindowsTargeting=true` | 退出码 0；仅编译，未在 Linux 运行 WPF 宿主 |
| `dotnet run --project test/BuilderChecks/BuilderChecks.csproj -c Release` | 退出码 0；`ALL_BUILDER_CHECKS_PASSED` |
| `dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --download-front` | 退出码 0；Runner 和 Front 下载界面模型专项通过 |
| `dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release` | 退出码 0；`ALL_QUEUE_CHECKS_PASSED`，含执行器、暂停持久化、提交保护和重启恢复 |
| `dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front` | 退出码 0；`ALL_FRONT_RESOURCE_BROWSER_CHECKS_PASSED` |
| `python3 scripts/check-layout.py` / `python3 scripts/check-frontend-boundaries.py` / `git diff --check` | 退出码 0；34 个项目引用与资源检查、项目边界及差异格式检查通过 |

初次 Front 构建提示两处用户名可能为空，已增加显式拒绝缺失名称的错误处理；最终构建无警告。
未执行：Windows PowerShell 构建脚本、发布打包、原生窗口/授权/游戏操作、FrontRuntimeChecks 的 Windows 运行。
`AGENT.md` 保留，待 Windows 验收完成后删除；未提交或推送。

## 后续修改约束

新解耦业务必须供两个前端调用，且不能引用任一前端的控件、资源或项目。
复用界面应归属 SharedUI，不放在其中一个前端后再让另一端引用。
新增共享源码必须由 Application 编译，并从旧项目默认源码项中排除；新旧前端各自负责交互适配。

## 本轮 Front 页面与共享模块

Front 页面现在通过 `IFrontLauncher`、`IFrontPreferences`、`IFrontVersions` 和 `IFrontResourceBrowser` 调用同级 `Accounts/`、`Downloads/`、`Settings/`、`Versions/`、`Resources/`、`Launch/` 业务目录。

版本管理窗口位于 `Front/Views/VersionManagerWindow.cs`，只负责表单和确认；实例元数据、启动设置、加载器队列、资源导入与导出由 `Versions/`、`Resources/` 和 `Downloads/` 执行。旧 UI 的版本设置保存也已改走 `VersionManagementService.SaveLaunch`。
