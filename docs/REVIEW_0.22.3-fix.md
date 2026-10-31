# 0.22.3-fix 审查指南

## 背景

`0.22.3`（`bb81ecf`）把 `MainWindow` 改成了 WebView2 容器，但旧的 WPF 界面代码（`Components/`、`Views/`、`Presentation/` 等 137 个文件）仍在引用已删除的 `MainTabs`、`TasksPageView` 与各页面字段，分支因此无法编译；同时 CI 的 push 分支列表只到 `0.22.2`，问题一直没有被检查到。

`0.22.3-fix` 以 `0.22.3` 为基础，完成迁移并让分支可构建、可审查。

## 提交顺序（按此顺序阅读）

| 提交 | 内容 | 审查重点 |
| --- | --- | --- |
| `chore: 补齐 0.22.2 的构建配置并让 CI 覆盖 0.22.3 分支` | VS Code 设计时修复（`Directory.Build.props`）、根解决方案、`.vscode`、`.gitignore`、CI 分支列表 | 只对 `DesignTimeBuild=true` 生效，不影响正常构建 |
| `refactor: 移除旧 WPF 界面，完成 WebView2 外壳迁移` | 删除旧 WPF 目录/文件与 `EmbeddedMicrosoftOAuthUI` | 删除清单是否符合“旧界面全部移除、认证与服务保留” |
| `fix: 恢复 0.22.2 的 .NET SDK 固定版本` | `global.json` 回到 `8.0.426 + latestPatch` | 修复 `SourceFormatter` 的 CS1705 |
| `test: 退役依赖旧界面的检查与预览项目` + 文档提交 | 删除 11 个 UI 检查、4 个预览工具、过期脚本与类文档；更新 CI/README/TODO | 保留的 14 个检查是否与 Core/Auth 相关 |

## 如何验证

```powershell
dotnet build MCUGLauncher.sln -c Debug
dotnet build MCUGLauncher.sln -c Release
python scripts/check-layout.py
python scripts/check-version.py
dotnet run --project tools/SourceFormatter
.\scripts\run-checks.ps1 -Name AuthChecks,MuaChecks,DownloadChecks,QueueChecks,CompatibilityChecks
dotnet run --project src/MCUGLauncher/MCUGLauncher.csproj
```

最后一条会启动 WebView2 窗口，用于人工确认页面加载与基础交互。

## 不需要重审的部分

- `bb81ecf` 中约 6000 行的前端重写（`Ui/app.js`、`styles.css`、`host.js`）属于 0.22.3 的既有内容，本次没有修改；
- 删除的旧 WPF 文件与本迁移目标一致，无需逐文件对比；
- `docs/archive/legacy-wpf/` 为历史文档，不参与构建。

## 已知缺口

- 新架构目前没有 WebView2 协议级自动化测试，保留的检查只覆盖 Core 与认证；
- 旧 WPF 时期的 UI 检查与预览工具已删除，后续需要按新界面重写（见 `TODO.md`）；
- 打包与发版仍使用 `scripts/build.ps1 -Publish`，产物未做人工点击验收。
