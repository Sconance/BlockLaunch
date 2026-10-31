# WebView2 UI 开发说明

## 分层

- MainWindow：WebView2 生命周期、可信来源校验、窗口和原生确认/文件选择。
- LauncherApplication：解析/校验消息、调用按职责划分的 UseCases、生成状态。
- LauncherContext：配置、账号、网络、任务队列与游戏会话的组合与生命周期。
- Services/LauncherTaskQueue：OperationCoordinator 的持久队列调度；DownloadExecutor 调用 Core 安装与校验下载。页面不持有任务。
- Core：启动、依赖解析、事务安装、校验下载、Java 扫描、收藏与存档归档等既有能力。
- Ui：app.js/host.js 保留旧路由；新增 tasks/install/games/bookmarks/saves 模块独立维护。styles.css 是当前样式入口，revision.css 已合并。

新增功能优先使用 Core，协议分发后调用相应 UseCase，不把业务搬回窗口。开发约定见根目录 AGENTS.md。

## 协议与数据

宿主仅接受 `https://app.local` 的 JSON 命令。消息先验证字段类型，错误返回 `{type:"error",message:"..."}`。

任务命令为 requestTasks/taskAction，操作包括 pause/resume/cancel/delete；tasks 响应包含持久记录 id、name、state、percent、message、committing。提交阶段不允许取消。

加载器通过 listLoaders/installLoader；整合包通过 pickFile(target=pack)/importPack；游戏通过 requestGames/stopGame；收藏通过 toggleBookmark；备份通过 listSaveBackups/backupSaves/restoreSave。原有命令继续兼容。

配置在 `%LOCALAPPDATA%/MCUGLauncher/config.json`；Core Preferences 的保留模型在 LegacyPreferences，网络/启动实际复用它。任务记录在 operations；收藏在 resource-bookmarks.json；备份按实例哈希放在 backups；游戏日志在 game-logs。

数据包不能直接安装到游戏根目录：先下载，再导入具体存档。整合包先下载后在安装与导入中选择文件，或直接选择已有本地包。

## 验证

```powershell
python scripts/check-layout.py
python scripts/check-version.py
python scripts/check-web-boundaries.py
dotnet run --project tools/SourceFormatter
./scripts/run-checks.ps1 -Name AuthChecks,MuaChecks,DownloadChecks,QueueChecks,CompatibilityChecks,WebBridgeChecks
npm ci --ignore-scripts --prefix test/WebUiChecks
npm test --prefix test/WebUiChecks
```

WebUiChecks 默认使用系统 Edge；也可通过 BROWSER_CHANNEL 指定支持的浏览器通道。截图输出到 work/verify/report-ui（不入库）。测试使用隔离目录、假 HTTP 和模拟桥接，不涉及真实用户账号。

完整的进展与剩余缺口见 [报告对应修复状态](REPORT_FIX_STATUS.md)。真实 WebView2、账号与游戏必须另行验收。
