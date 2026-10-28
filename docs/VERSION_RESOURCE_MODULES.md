# 版本与资源管理模块

版本和资源的业务操作分别位于主项目 `Versions/`、`Resources/`，与 `MainWindow/` 同级。
它们使用 Core 的现有实现，不持有 MainWindow、Preferences 或 WPF 控件。

## 版本管理

入口：[`VersionManagementService`](../src/MCUGLauncher/Versions/VersionManagementService.cs)。
数据模型：[`VersionManagementModels.cs`](../src/MCUGLauncher/Versions/VersionManagementModels.cs)。

- 查询在线版本，合并本地独有的配置；在线失败时返回本地列表和单独的错误信息，取消不降级为离线成功。
- 区分已有配置和已完成安装，保留需修复、整合包、自定义名称及收藏标记。
- 解析启动配置、游戏版本、加载器，以及共享/独立实例目录。
- 查询加载器、准备稳定 Fabric 安装请求，读取和保存实例描述，调用现有卸载计划与删除实现。
- `VersionRow` 和 `InstanceTarget` 从窗口 Models 迁到版本模块，保留原类型和命名空间。

版本查询使用根目录快照。窗口收到结果后检查当前目录，避免把另一个目录的版本列表显示到当前目录。

## 资源管理

入口：[`ResourceManagementService`](../src/MCUGLauncher/Resources/ResourceManagementService.cs)。
参数模型：[`ResourceManagementModels.cs`](../src/MCUGLauncher/Resources/ResourceManagementModels.cs)。

- 接收明确的查询参数，调用 Modrinth，筛选兼容版本和发布通道。
- 收藏候选兼容性检查、最多四路并发、下载量排序与分页；收藏存储和装饰仍由原组件负责。
- 校验独立资源下载目录与资源文件，生成原队列可用的 DownloadRequest。
- 准备实例内容安装、验证数据包存档目录、导入本地资源、列举和切换内容、移入备份。
- 检查更新、生成更新预览和包含原内容指纹的队列请求、回退内容事务。

修改实例的服务入口仍检查传入的游戏运行状态；安装和导入检查目标版本是否安装完整。
底层的实例锁、依赖检查、事务、提交边界、备份与内容指纹均复用 Core，不另建一套实现。
服务不会持续跟踪游戏进程，窗口必须传入调用时的运行状态，并沿用原 Run / 队列门禁。

## 调用边界

```text
MainWindow / 界面控制器
  ├── 读取控件 → 版本服务 → 版本列表、实例目录、目标信息
  ├── 读取控件 → 资源服务 → 搜索结果、安装方案、更新请求
  ├── 用户确认 → 原 OperationCoordinator / 下载队列
  │                  └── DownloadTaskExecutor → Core 安装与事务服务
  └── 显示结果、刷新页面
```

窗口保留：选中项、页面和表单、确认弹窗、目录选择器、列表绑定、忙碌状态、游戏会话、任务调度和日志显示。
`ContentUpdateController` 已改为直接注入资源服务和小型交互接口，不再引用具体窗口或 WPF；
`InstanceUninstallationController`、`LoaderInstallationController` 仍是持有窗口的交互适配器。
内容更新的任务执行、弹窗和入队由 UpdatesUI 显式实现接口，详见 [内容更新解耦](CONTENT_UPDATE_DECOUPLING.md)。

整合包生命周期、拖放分类、实例库/收藏 UI、脚本与导出页面仍沿用原组件。
本次不是把 MainWindow 下所有类整体搬走，也不代表所有页面已经组件化。
两个服务复用初始化后的 HttpClient；不要在旧客户端释放前创建服务。

## 检查

[`ManagementServiceChecks`](../test/CompatibilityChecks/ManagementServiceChecks.cs) 链接两个模块源码，不使用 WPF。
通过临时目录与合成 HTTP 响应检查版本合并、离线回退、取消、实例隔离、兼容性与收藏分页、
目标目录限制、游戏运行门禁，以及本地资源导入/禁用/备份。

```powershell
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --management
```

不带参数时 CompatibilityChecks 也会执行这些检查。真实在线查询、加载器安装、更新回退和卸载仍需完整回归；
本地合成检查不能替代 Windows 界面验证。
