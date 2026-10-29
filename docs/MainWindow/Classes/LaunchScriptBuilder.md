# LaunchScriptBuilder

将启动器路径、游戏目录、实例和账号模式格式化为 Windows 命令提示符或 PowerShell 启动脚本；脚本只保存启动器参数，不保存账号凭据。

- **主要入口：** `Build(executable, root, instance, account, powershell)`。
- **协作：** 由 MainWindow 的启动脚本导出流程调用；导出对话框和文件编码仍由 UI 负责。
- **检查：** `VersionUiChecks` 验证 PowerShell 单引号转义、命令提示符百分号转义及账号参数。
- **源码：** [`LaunchScriptBuilder.cs`](../../../src/MCUGLauncher/Builders/LaunchScriptBuilder.cs)，已迁入与 MainWindow 同级的共享模块，不依赖任一前端。
- **独立检查：** `test/BuilderChecks` 不引用 WPF 或前端项目；详见 [共享模块](../../dcg/BUILDERS_MODULE.md)。
