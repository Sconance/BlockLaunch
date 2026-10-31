# 路线图（0.22.3 及之后）

0.22.3 把界面迁移到 **WebView2 + HTML/CSS/JS**，C# 只保留服务与消息桥接。`0.22.3-fix` 分支修复了原分支的编译失败、SDK 固定、CI 覆盖与 VS Code 配置问题，审查说明见 [0.22.3-fix 审查指南](docs/REVIEW_0.22.3-fix.md)。

## 已完成（0.22.3-fix）

- 移除旧 WPF 界面（`Components/`、`Views/`、`Presentation/`、`Composition/`、`Navigation/`、`Builders/`、`Styling/`、`Initialization/` 与旧 Models），`MainWindow` 只保留 WebView2 宿主。
- 保留并继续使用 `Authentication/`、`Services/`、`Models/AppConfig`、`Ui/` 资源；消息协议位于 `Ui/host.js` 与 `MainWindow.xaml.cs`。
- 恢复 0.22.2 的 .NET SDK 固定版本，修复 `SourceFormatter` 的 CS1705。
- CI 覆盖 `0.22.3` 与 `0.22.3-fix`；退役依赖旧界面的 11 个检查项目与 4 个预览工具。
- 修复 VS Code 的 CS1061/CS0103：设计时构建补 `BuildingInsideVisualStudio`，新增根目录解决方案与工作区设置。

## 待办

### 1. 新架构的测试
- [ ] 为 WebView2 消息协议（`host.js` ↔ `MainWindow.xaml.cs`）补协议级测试，覆盖版本/Mod/服务器/社区/配置读写。
- [ ] 真实 WebView2 环境回归：页面加载、导航、主题切换、错误提示与更新流程。
- [ ] 保留的 Core/Auth 检查继续在 CI 运行；新增检查项目时同步 `run-checks` 与 CI 列表。

### 2. 文档
- [ ] 补 `docs/WEBVIEW_UI.md`：目录、消息协议、数据文件与调试方式。
- [ ] 清理 `docs/` 下旧 WPF 文档引用；历史材料已归档到 `docs/archive/legacy-wpf/`。
- [ ] README 的截图与功能说明随新界面更新。

### 3. 发布
- [ ] 版本号 0.22.3 的 CHANGELOG/README 一致性（`python scripts/check-version.py`）。
- [ ] 合并到 `main` 后打 `v0.22.3` 标签，从标签构建完整版与 Lite 版并记录哈希。
- [ ] 在 GitHub Releases 上传附件与发布说明（参考 [docs/releases](docs/releases/README.md)）。

## 审查约定

- 小步提交：一个提交只做一类改动（配置、删除、测试、文档），便于逐条审查。
- 删除旧文件属于迁移的一部分，不需要逐个文件评审；重点审查 `MainWindow.xaml.cs` 的消息分发与 `Services/` 的数据边界。
- 版本号只改 `Directory.Build.props` 的 `McugVersion`，规则见 [版本号控制](docs/VERSIONING.md)。
