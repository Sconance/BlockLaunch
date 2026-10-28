# 启动模块拆分

## 当前结构

```text
src/MCUGLauncher/
├── Launch/
│   ├── GameLaunchController.cs     启动、预检、认证组件及进程创建的编排
│   ├── IGameLaunchInteraction.cs   输入、授权、确认和结果的窄交互边界
│   └── GameLaunchModels.cs         身份、启动选择与结果数据
└── MainWindow/
    └── UI/Launch/GameLaunchUI.cs   窗口事件及接口适配，不再实现启动编排
```

`MainWindow.Launch()` 保留为一行转发，供首页、实例详情、服务器连接及现有任务入口复用。
它调用独立的 `GameLaunchController.Launch(CurrentToken)`，不再执行游戏预检和进程创建。

## 依赖方向

```text
启动按钮 / 实例启动入口
  → MainWindow.Run（原有互斥、取消、错误展示）
  → GameLaunchController
      ├── IGameLaunchInteraction：设置、账号授权、输入、确认、展示
      ├── Core：实例元数据、预检、认证组件、启动服务和游戏会话
      └── OfflineSkinPackBuilder：离线皮肤资源准备
```

控制器不引用 `MainWindow`、`System.Windows` 或命名控件。
窗口显式实现接口：认证和表单交互保留在 UI 适配边界，服务只接收会话及启动数据。
MUA metadata 通过回调获取，控制器不直接依赖主项目的 MUA 账号服务类型。

## 独立模块负责

1. 检查已有游戏、取消、设置和账号准备结果。
2. 检查已安装版本、读取实例并准备语言、离线皮肤与启动配置快照。
3. 执行模组预检；需要时请求界面确认。
4. 准备 MUA 认证组件、构建并启动游戏进程，通知界面接收会话。
5. 请求正常停止游戏，并将失败信息交给日志入口。

临时服务器地址写入启动配置副本，不修改全局或实例原配置。
预检被拒绝、认证失败或取消时，不应继续创建游戏进程。

## 尚未拆分的部分

本次只拆启动编排，不重构整个窗口或所有账号流程：

- `Run`、`busy` 和操作协调器仍在窗口。
- 设置表单读取与授权 UI 仍由窗口适配。
- `game`、`gameSession` 和最近诊断上下文仍由窗口保存，供其他功能查询。
- 游戏日志、退出观察和崩溃诊断沿用 StabilityUI。
- 主窗口最小化、隐藏与恢复沿用 GameWindowBehaviorController。

因此这是独立启动交互模块，不是已完成的全局游戏会话服务或前后端分离。

## 检查

`test/CompatibilityChecks/GameLaunchControllerChecks.cs` 使用假的交互对象和禁止网络的 HTTP handler，
检查重复启动、取消、设置/认证失败、未安装版本、预检拒绝、临时配置不污染原设置、外部认证失败及无会话停止。
不读取真人账号，也不启动真实游戏。

```powershell
dotnet run --project test/CompatibilityChecks/CompatibilityChecks.csproj -c Release -- --launch-controller
```

不加该参数时，原 CompatibilityChecks 也会运行这些检查。
完整构建与真实微软/MUA、游戏启动和退出仍需在相应环境验证。
