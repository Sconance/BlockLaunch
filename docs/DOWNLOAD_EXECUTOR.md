# 下载任务执行器拆分（第一步）

本次只提取原 `DownloadQueueUI.ExecuteDownload`，不改变入队、定时调度、任务状态、暂停/取消按钮或队列退出清理。

## 当前调用关系

```text
MainWindow.PumpDownloads
  → OperationCoordinator.Run
  → DownloadTaskExecutor.Execute(record, context)
      ├── game：GameInstaller
      ├── resource：ResourceDownloads
      ├── pack / import：PackService + GameInstaller
      └── content：ContentService
```

执行器位于 [`src/MCUGLauncher/Downloads/DownloadTaskExecutor.cs`](../src/MCUGLauncher/Downloads/DownloadTaskExecutor.cs)，
与主项目的 `MainWindow/` 目录同级。不引用 MainWindow、WPF 控件或 Dispatcher。

## 明确依赖

`DownloadExecutionServices` 接收运行时 HttpClient、网络策略和五类回调：

| 依赖 | 职责 |
| --- | --- |
| ProtectWorlds | 使用任务根目录检查并保护存档，失败则停止执行 |
| DefaultLaunchSettings | 获取当前默认隔离配置，实例配置存在时优先使用实例值 |
| IsCurrentRoot | 判断完成任务后是否应该刷新当前游戏目录的界面 |
| RefreshInstalledContent | 重建版本列表、选择安装结果并刷新实例/内容视图 |
| Log | 输出恢复、下载完成或非致命刷新失败信息 |

运行时 HttpClient 和网络策略初始化完成后才组装执行器，不能传入随后被替换/释放的初始客户端。
这里是第一步拆分，存档保护和刷新仍由窗口适配，不表示队列与所有下载依赖已独立。

## 保持的行为边界

1. 使用 `record.Root` 与请求中保存的版本、Java、目标和可选内容，不改为执行时读取界面选择。
2. 继续使用同一个 OperationContext 的令牌、进度及隐式提交上下文，不新建任务协调器。
3. 保留安装恢复、导入源哈希校验、资源目录检查、内容方案和内容指纹。
4. 完成安装后的默认语言写入/界面刷新失败仍按原策略处理，不能因此把已提交的安装变成可重试失败。
5. 非当前目录的任务可以完成，但不刷新当前目录的列表；需要备份的任务仍按原规则要求匹配当前目录。

## 未改变的部分

- `EnqueueDownload` 和整合包导入确认仍在窗口。
- `PumpDownloads`、500 ms 定时器、队列锁和 `busy` 仍在窗口。
- 暂停、继续、取消、任务显示和退出时清理保留原逻辑。
- Core 的 OperationCoordinator、GameInstaller 和下载/事务服务未重写。

第二步才考虑独立队列控制器；本次没有提前实施。

## 隔离检查

新增 `test/QueueChecks/DownloadTaskExecutorChecks.cs`，通过本地合成 HTTP 响应检查资源下载、
目标目录限制、未知任务、导入文件变化、内容提交后刷新失败、跨目录执行、备份失败和暂停。
不会访问真实 CDN 或下载游戏。

```powershell
dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --executor
```

不带参数时，原 QueueChecks 也会执行这些检查。真实游戏和加载器安装、整合包安装仍需另行回归。
