# Front 界面迁移预览

完整待测步骤与旧 UI 删除门槛见 [Front 验收清单](FRONT_ACCEPTANCE.md)。目前不满足删除条件，保留旧 UI。

## 第一阶段范围

新界面位于 [`Front/`](../src/MCUGLauncher/Front/Views/FrontWindow.xaml)，与 MainWindow 同级。
原 App.xaml 的 StartupUri、MainWindow、账号配置和启动流程均不改动。
这不是所有旧页面迁移完成，也不是 Web 前端或独立进程。

| 页面 | 本阶段实现 |
| --- | --- |
| 主窗口与开始游戏 | 复用旧 XAML 的导航分区、尺寸、配色和卡片风格；展示模拟实例/玩家，启动按钮禁用 |
| 内容中心 | 可搜索、按类型/游戏版本/加载器筛选、排序、翻页、选择项目并读取兼容版本 |
| 游戏安装、内容管理、设置、日志、任务 | 导航和明确的未迁移提示；不提供假装已接通的业务按钮 |

整体风格参考现有界面，未逐像素复刻所有运行时 Builders/Styling 的效果。
本轮不接入登录、启动、下载、安装、删除、配置保存或事务回退，防止预览修改真实数据。

## 分层

```text
Front/Views                         XAML 布局和界面事件
    ↓
Front/ViewModels                    输入、列表、加载/空/错误/取消状态
    ↓
Contracts/IFrontResourceBrowser     纯 C# 查询接口和显示数据
    ↑
Adapters/FrontResourceBrowserAdapter
    ↓
Resources/ResourceManagementService → Core ContentService
```

Front 只依赖契约，不直接引用 MainWindow、Controller、HttpClient、文件操作或资源业务实现。
适配器调用现有服务而不是另写搜索/兼容性规则。
Contract 与 Front 仍在原主项目内，当前隔离由代码边界维护，不是独立程序集强制隔离。

模拟实现位于工具目录，不混入界面；独立工具负责创建 Application、注入服务和复用样式。
预览从 App.xaml 的 Application.Resources 提取同一套样式，不复制整份样式，不触发旧 StartupUri。
原资源文件中的样式仍是共享依赖，尚未迁到 Front/Styles。

## 在 Windows 上打开预览

需要仓库 global.json 指定的 .NET 8 SDK 和 Windows Desktop 环境。

```powershell
dotnet run --project tools/FrontPreview/FrontPreview.csproj -c Release
```

默认模拟模式不会发起网络请求。点击“内容中心”，搜索 `empty` 检查空列表，搜索 `error` 检查失败状态；
清空关键词可查看 32 个示例项目、分页和模拟版本，查看加载状态时可点击取消。
模拟内容均标明为演示数据，不代表真实项目。模拟筛选用于传参/布局预览，不能验证真实兼容性。

真实只读模式：

```powershell
dotnet run --project tools/FrontPreview/FrontPreview.csproj -c Release -- --live
```

此模式通过现有 ResourceManagementService 调用 Modrinth；HttpClient 由预览工具创建，
不会读取旧账号或配置，也不加载旧网络偏好。查询可能受网络限制而失败，错误展示在页面。
不显示图标远程加载、项目长描述、收藏等尚未适配的完整功能。

## 生命周期和状态

每次搜索保存查询快照，后续分页/详情使用同一组条件，不读取用户尚未提交的新筛选值。
新搜索和新选择会取消上一请求，并检查响应所属请求，避免忽略取消的旧请求覆盖新界面。
搜索/详情超时为 60 秒；关闭窗口释放 ViewModel，离开资源页取消当前查询。
未完成加载或错误状态不允许继续翻页；错误后可以重新搜索。

## 检查

[`FrontResourceBrowserChecks`](../test/CompatibilityChecks/FrontResourceBrowserChecks.cs) 在无 WPF 的检查项目中链接
契约、真实适配器和 ViewModel，覆盖服务映射、查询快照、分页、乱序响应、取消、错误、空结果和关闭。

```powershell
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --front
```

本次环境无 .NET SDK 且不是 Windows，未编译、未运行检查、未打开界面；静态验证不能代替上述验收。

下一阶段可沿用同一模式为版本列表添加契约/适配器，再单独接入启动和下载命令，
必须保留原操作协调器、运行门禁和事务保护，不能让 ViewModel 自行执行安装或文件修改。
