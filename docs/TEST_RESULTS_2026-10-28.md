# Windows 测试结果（2026-10-28）

## 结论

Windows 11 x64、.NET SDK 8.0.426。三个 Release 项目构建成功，设置专项检查和相关控制台回归均通过。FrontPreview 尚未进行 GUI 手工验收，因此本轮总体结论为**部分完成**，不能据此认定设置页的完整 Windows 验收通过。

测试分支：`refactor-dcg`

测试基线提交：`399a36b6f054486ca2a02d8285fc192c1e43bd2c`

## 构建结果

| 项目 | 命令 | 结果 |
| --- | --- | --- |
| 主程序 | `dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release` | 通过，退出码 0，0 警告、0 错误 |
| FrontPreview | `dotnet build tools/FrontPreview/FrontPreview.csproj -c Release` | 修复后通过，退出码 0，0 警告、0 错误；首次构建因缺少 `System.IO` 失败 |
| CompatibilityChecks | `dotnet build test/CompatibilityChecks/CompatibilityChecks.csproj -c Release` | 通过，退出码 0，0 警告、0 错误 |

生成文件：

- `src/MCUGLauncher/bin/Release/net8.0-windows/win-x64/MCUGLauncher.exe`
- `tools/FrontPreview/bin/Release/net8.0-windows/win-x64/FrontPreview.exe` 及其完整依赖和 `Fixtures/LauncherApp.xaml`
- `test/CompatibilityChecks/bin/Release/net8.0-windows/CompatibilityChecks.exe`

## 自动化检查

以下命令均退出码 0：

| 检查 | 命令参数 / 命令 | 结果 |
| --- | --- | --- |
| Front 设置 | `--front-settings` | 通过，输出 `ALL_FRONT_SETTINGS_CHECKS_PASSED` |
| 业务服务 | `--business-services` | 通过 |
| 启动控制器 | `--launch-controller` | 通过 |
| Front 资源浏览 | `--front` | 通过 |
| 版本及资源管理 | `--management` | 通过 |
| 内容更新控制器 | `--content-update-controller` | 通过 |
| 下载队列服务 | `dotnet run --project test/QueueChecks/QueueChecks.csproj -c Release -- --queue-service` | 通过 |

这些是控制台断言程序，不是 `dotnet test`。设置专项覆盖快照读取/保存、偏好保留、边界与非法输入、游戏运行门禁、取消及 ViewModel 错误恢复等逻辑。

## 源码修改

- `src/MCUGLauncher/MainWindow/UI/Instances/CollectionsUI.cs`：将收藏批量版本解析中未定义的 `Modrinth` 参数替换为 `new ContentService(http, CurrentToken)`。该服务实现收藏批量解析所需的资源 provider 接口，并复用当前 HTTP 客户端与操作取消令牌。此处也消除了编译器报告的目标类型推断错误。
- `tools/FrontPreview/DemoResourceBrowser.cs`：添加 `using System.IO;`，使模拟错误分支中的 `IOException` 类型可解析。

## 未测项目

- FrontPreview 尚未启动并完成原生 GUI 操作。桌面自动化 native pipe 不可用；没有截图或 GUI 证据。
- 尚未手工验收真实设置页读写、重启恢复、非法输入时文件哈希不变、模拟游戏运行门禁、同目录进程锁、拒绝接管非空目录、高版本配置保护、键盘导航、缩放和资源页导航回归。
- 未创建 transcript 或截图证据；自动化测试通过不能替代以上人工流程。
- 根目录 `AGENT.md` 保留，因为必测 GUI 项仍未完成。

专项命令和逐项交接记录见 [FRONT_SETTINGS_WINDOWS_TEST.md](FRONT_SETTINGS_WINDOWS_TEST.md)；Front 总体待验收清单见 [FRONT_ACCEPTANCE.md](FRONT_ACCEPTANCE.md)。
