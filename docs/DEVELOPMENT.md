# 开发说明

## 架构边界

- 旧入口：`src/MCUGLauncher/App.xaml.cs` 与 `MainWindow/MainWindow.xaml.cs`。
- 新入口：`src/MCUGLauncher/Front/Program.cs`，界面全部在 `Front/Views/`、`ViewModels/`、`Styles/`。
- 业务目录位于 `MainWindow` 同级：`Accounts`、`Downloads`、`Settings`、`Versions`、`Resources`、`Launch`、`Builders`。
- `MCUGLauncher.Application.csproj` 通过源码链接编译这些目录、`Contracts` 与 `Adapters`；不要再复制一套“共享业务”。
- `Front/Hosting/FrontLauncherRuntime.cs` 负责契约适配与生命周期；不属于纯视图，也不应承载新的领域算法。
- Core 提供底层实现；SharedUI 只提供可复用 WPF 控件。新旧前端均不能直接依赖另一套前端。

拆分模块时先定义参数、取消令牌、进度与结果，再让两个前端分别适配。不能为 Front 调用重新依赖旧 MainWindow。
删除任一前端后，另一套应仍可独立构建。不要在业务服务中引用 WPF 控件、窗口或 Front 类型。

## 开发约定

- C# 使用 4 空格缩进，公开类型与成员使用 PascalCase，沿用相邻文件的私有成员命名。
- UI 读取输入、确认危险操作、显示状态；业务服务负责校验和文件/网络操作。
- 下载使用共享队列，持久化状态和事务提交沿用现有 Core 实现，不直接从按钮回调修改任务文件。
- 保留现有配置字段、内部 `BlockLaunch` 命名空间与存储目录的兼容性。
- 不记录密码、令牌或私人路径，不提交 `bin/`、`obj/`、游戏目录和存档。
- 本轮已按要求删除测试项目；发现问题记录复现步骤，不引用不存在的历史检查程序。

## 构建与格式

编译命令和项目图见 [BUILD.md](BUILD.md)。Windows 验收以 [交接清单](dcg/FRONT_UI_WINDOWS_TEST.md) 为准。
源码格式工具仍保留，需 .NET SDK；从仓库根目录运行：

```powershell
dotnet run --project tools/SourceFormatter -- --write
dotnet run --project tools/SourceFormatter
```

不要为了格式化扩大业务改动范围。格式检查、编译通过和窗口打开都不能单独证明功能验收通过。
原有 `docs/MainWindow/` 等说明保留为旧 UI 参考；其中历史测试命令不再适用。
