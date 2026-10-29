# 共享 Builders 模块

## 目录与边界

```text
src/MCUGLauncher/
├── Builders/                 无 UI 依赖的共享生成逻辑
│   ├── LaunchScriptBuilder.cs
│   └── OfflineSkinPackBuilder.cs
├── MainWindow/Builders/      旧前端专用的九个 WPF 界面构建器
└── Front/                   新前端及其宿主
```

共享 Builders 不引用 MainWindow、Front、System.Windows、窗口资源或具体控件。
两个前端均可调用公开 API；文件选择、确认、导出路径、错误展示由各自前端负责。
保留 BlockLaunch 命名空间和原有方法签名，由 Application 项目链接编译，两个前端引用同一程序集。

## 入口与当前调用

| 入口 | 职责 | 当前调用 |
|---|---|---|
| `LaunchScriptBuilder.Build(...)` | 返回 CMD/PowerShell 启动脚本，不写文件，不保存令牌 | 旧前端 FeatureUI；新前端尚未提供脚本导出页面，可直接调用同一 API |
| `OfflineSkinPackBuilder.Prepare(...)` | 根据皮肤及版本元数据生成离线皮肤 ZIP | 共享 GameLaunchController，新旧前端均经启动控制器调用 |

迁移不改变生成行为。删除任一前端不会删除这两个 Builder 的源码或给它们引入缺失引用。
Front 已有独立工程，不再引用旧入口；项目级边界与双向移除构建检查见 [FRONTEND_PROJECTS.md](FRONTEND_PROJECTS.md)。

## 为什么其他 Builder 留在旧前端

`LaunchOptionsForm`、`ContentSearchFilterBuilder`、`WindowChromeBuilder` 以及六个
`Version*PageBuilder` 创建 WPF 控件；多项直接依赖 MainWindow、VersionSettingsPageContext
或旧前端资源。把它们直接移动到共享目录只是改路径，不是解耦。
后续如需共享其中业务，应提取数据输入/输出和业务服务；控件创建及事件绑定留在各自前端。

## 验证

新增 `test/BuilderChecks` 直接编译两个共享源码，不引用任何前端项目、WPF 或第三方包。
在安装了 global.json 指定 SDK 的设备运行：

```powershell
dotnet run --project test/BuilderChecks/BuilderChecks.csproj -c Release
```

退出码为 0，输出 `ALL_BUILDER_CHECKS_PASSED` 才表示通过。覆盖脚本转义、账号参数、
在线/缺失皮肤门禁、资源包格式数字/对象/缺失版本回退、纹理内容与资源包替换。
仅使用自建临时文件夹并在结束后清理，不操作正式游戏目录。

Windows 继续执行既有回归：

```powershell
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --launch-controller
dotnet run --project test/VersionUiChecks/VersionUiChecks.csproj -c Release
```

2026-10-29 开发端用临时 SDK 8.0.426 运行 BuilderChecks，退出码 0，输出 `ALL_BUILDER_CHECKS_PASSED`。
Windows VersionUiChecks 界面回归尚未执行；双前端交叉编译与移除构建结果见 [项目验证记录](FRONTEND_PROJECTS.md)。
