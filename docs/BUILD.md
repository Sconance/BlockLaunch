# 构建与目录

## 目录职责

| 目录 | 内容 |
| --- | --- |
| `src/MCUGLauncher/` | 旧 WPF 入口、共享业务源码及独立 Front 子项目 |
| `src/MCUGLauncher/Front/` | 独立新前端项目、入口、自有主题与服务组装 |
| `src/MCUGLauncher.Application/` | 无窗口依赖的共享业务项目，链接既有业务源码 |
| `src/MCUGLauncher.SharedUI/` | 两个前端共用的 WPF 下载组件，不属于任一前端 |
| `src/MCUGLauncher.Core/` | 下载、Java、实例、整合包等服务 |
| `test/` | 功能、界面及共享模块的独立检查程序 |
| `tools/` | 辅助界面预览程序与源码格式工具 |
| `scripts/` | 构建、发布、指定检查与静态布局校验 |
| `docs/` | 使用指南、构建说明、依赖清单、历史测试记录与截图 |

命名空间与兼容存储路径保留内部旧名 BlockLaunch，不影响产品名称。
根目录保留 README、CHANGELOG 和 CONTRIBUTING，便于仓库平台展示。

## 构建与发布

要求 Windows x64、.NET 8 SDK（global.json 固定 8.0.426，可使用同 feature band 的更新补丁），首次还原 NuGet 包需要网络。
以下命令从仓库根目录运行：

```powershell
.\scripts\build.ps1
.\scripts\build.ps1 -Publish
```

也可以直接使用 SDK：

```powershell
dotnet restore src/MCUGLauncher/MCUGLauncher.csproj
dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
dotnet publish src/MCUGLauncher/MCUGLauncher.csproj -c Release -o artifacts/publish
```

输出 `artifacts/publish/MCUGLauncher.exe`。项目已配置自包含、Windows x64 单文件。
游戏仍需 Java，微软内嵌授权需要 WebView2。Linux 不能直接运行 WPF 界面。

## Windows 双前端编译

在 Windows x64 安装 `global.json` 指定的 SDK，打开 PowerShell，在仓库根目录运行：

```powershell
# 旧前端：正式 MainWindow 启动器
.\scripts\build-windows.ps1 -Frontend Legacy
# 新前端：FrontWindow 真实模块宿主
.\scripts\build-windows.ps1 -Frontend Front
# 两个入口依次编译（默认行为）
.\scripts\build-windows.ps1
```

脚本也可通过完整路径从其他目录调用，自动切换到仓库根目录并在结束后恢复原工作目录。
若本机执行策略阻止脚本，仅对本次进程运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -Frontend Both
```

| 选择 | Release 默认可执行文件 |
| --- | --- |
| Legacy | `artifacts/windows/legacy/Release/build/MCUGLauncher.exe` |
| Front | `artifacts/windows/front/Release/build/MCUGLauncher.Front.exe` |

两个输出目录互不覆盖。编译失败立即停止，脚本不会启动程序、修改正式入口、运行测试、提交或推送。
Front 项目为 `src/MCUGLauncher/Front/Front.csproj`，不引用或编译旧入口项目；新旧入口分别引用 Application 和 SharedUI。
Front 自有 App.xaml、Program.cs 和 Styles/Theme.xaml，不再复制旧入口资源。项目独立不等于所有旧功能均已迁移。
Front 默认使用 `%LOCALAPPDATA%\BlockLaunch` 正式配置，实际调用账号、版本、资源、设置、启动和下载模块；不再是隔离预览。
同配置目录的新旧前端互斥，必须先退出其中一个。启动 Front 后，未暂停的持久化下载会在启动恢复完成后自动执行。
显式 `--settings-dir <目录>` 仍可指定配置位置，但同样执行真实业务，不提供模拟资源、模拟游戏或禁用下载模式。

需要自包含发布目录时运行（首次还原运行时和依赖需要网络）：

```powershell
.\scripts\build-windows.ps1 -Frontend Both -Publish
```

发布输出位于对应入口的 `artifacts/windows/<legacy或front>/Release/publish/`；`-Configuration Debug` 可改为 Debug 配置。
复制或分发时保留完整输出目录及依赖，不能只复制 `MCUGLauncher.Front.exe`；Front 主题已编译进自身程序集，无需 Fixtures 文件。
旧的 `build.ps1 -Publish` 完整版/Lite 发布方式不变。
本脚本目前仅完成静态检查，尚未在 Windows 执行构建或发布；它不代替 `docs/dcg/DOWNLOAD_MODULE.md` 的功能验收。
真实 Front 的功能边界见 [Front 模块接入](dcg/FRONT_RUNTIME.md)；项目拆分、独立构建验收见 [双前端独立项目](dcg/FRONTEND_PROJECTS.md)。旧 tools/FrontPreview 工程已移除。

## 运行检查

检查项目使用自定义控制台断言，以 `dotnet run` 执行，不是 `dotnet test`。
PowerShell 脚本支持从任意工作目录调用，执行时会切换到仓库根目录：

```powershell
.\scripts\run-checks.ps1 -Name CompatibilityChecks
.\scripts\run-checks.ps1 -Name DownloadChecks,DependencyChecks
.\scripts\run-checks.ps1 -Name AuthChecks -CheckArguments '--avatar'
```

直接运行的等价示例：

```powershell
dotnet run --project test/InlineUiChecks/InlineUiChecks.csproj -c Release
```

脚本不会默认批量运行全部检查。部分检查会启动窗口、访问网络、读取账号缓存或扫描
本机环境，运行前阅读对应 `Program.cs`。集成检查可能下载和启动真实游戏，需单独准备
Java 与游戏环境。自动化检查不代替真实授权、游戏联机或多显示器 DPI 验收。

## 界面预览与资源

```powershell
dotnet run --project tools/CompactPreview/CompactPreview.csproj -c Release
```

从仓库根目录运行预览，生成的隔离数据存入已忽略的 `work/`。
主题 XAML 由项目配置复制到输出目录的 `Fixtures/LauncherApp.xaml`，不依赖旧开发机
或 `work/v092/`。MUA 检查中的 metadata JSON 为合成 fixture，不是真实认证站响应，
也不包含真人凭据。真实组件与认证集成验证仍需独立执行。

## 静态布局校验

```powershell
python scripts/check-layout.py
```

只需 Python 3 标准库，在 Windows 或 Linux 上检查项目 XML、项目引用、链接源码、
图标与复制资源是否存在，并检查旧路径残留；不会编译或运行游戏。CI 在构建前执行此校验。

`docs/TESTING.md` 顶部记录当前版本验证范围，下方保留历史记录。

## 双版本发布

scripts/build.ps1 -Publish 同时生成完整版和 Lite 版。artifacts/release 中包含 MCUGLauncher-版本号-Windows-x64.exe 与 MCUGLauncher-版本号-Windows-x64-Lite.exe。Lite 不内置 .NET，要求 .NET 8 Desktop Runtime x64；两版均需要 WebView2 才能使用内嵌微软授权。后续 Release 同时附带两个程序，完整版名称保持不变。
