# 构建与目录

要求 Windows x64、.NET 8 SDK，首次还原 NuGet 包需要网络。

```powershell
dotnet restore BlockLaunch/BlockLaunch.csproj
dotnet build BlockLaunch/BlockLaunch.csproj -c Release
dotnet publish BlockLaunch/BlockLaunch.csproj -c Release -o artifacts/publish
```

输出 artifacts/publish/MCUGLaunch.exe。项目已配置自包含、Windows x64 单文件。游戏仍需 Java，微软内嵌授权需要 WebView2。

- BlockLaunch：WPF 界面、账号与功能交互。
- BlockLaunch.Core：下载、Java、实例、整合包等服务。
- *Checks：可独立运行的检查项目。
- *Preview：隔离场景界面预览工具。

保留现有兄弟目录关系以兼容 ProjectReference，内部旧名不影响产品名称。

```powershell
dotnet run --project CompatibilityChecks/CompatibilityChecks.csproj -c Release
dotnet run --project InlineUiChecks/InlineUiChecks.csproj -c Release
```

部分检查会启动窗口、访问网络或扫描本机环境，运行前阅读对应 Program.cs。自动化检查不代替真实授权、游戏联机或多显示器 DPI 验收。
