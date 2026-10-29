# LaunchScriptBuilder

将启动器路径、游戏目录、实例和账号模式格式化为 Windows 命令提示符或 PowerShell 启动脚本；脚本只保存启动器参数，不保存账号凭据。

- **主要入口：** `Build(executable, root, instance, account, powershell)`。
- **协作：** 由 MainWindow 的启动脚本导出流程调用；导出对话框和文件编码仍由 UI 负责。
- **源码：** [`LaunchScriptBuilder.cs`](../../../src/MCUGLauncher/Builders/LaunchScriptBuilder.cs)，已迁入与 MainWindow 同级的共享模块，不依赖任一前端。
- **验证：** 本仓库已按维护者要求删除测试源码；Windows 行为验收见 [交接清单](../../dcg/FRONT_UI_WINDOWS_TEST.md)。
