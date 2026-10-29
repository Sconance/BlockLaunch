# 重构路线图

开发版本 **0.22.1** 位于同名分支；0.22.0 已合并到 `main`，合并说明见 [合并到主分支前的说明](docs/MERGE_TO_MAIN.md)。`refactor` 分支已废弃，仅保留 git 历史与 [历史归档](docs/archive/branch_refactor/README.md) 中的记录。

## 已完成

- [x] 原生窗口 shell 化：`MainWindow.xaml.cs` 只保留 35 行，九个页面与标题区拆成独立 XAML。
- [x] `Composition/LauncherWorkspace` 组合根、`LauncherControls` 强类型绑定、`LauncherPageRouter` 编译期页面路由。
- [x] 41 个功能模块从窗口迁出；12 个组件改为接收控件、状态与回调。
- [x] 版本设置拆分为概览、启动、加载器、Mod、导出五个独立页面。
- [x] 0.20.1 / 0.20.2 功能整合：资源工作区、心形收藏与筛选、八个菜单扩展区、加载器概览、品牌资源。
- [x] `refactor` 全量并入；`Components/` 按九个功能域重组、10 个控制器按职责改名（0.22.0）。
- [x] 142 篇类文档归档到 `docs/classes/`，版本号改为单一来源并接入 CI 检查。
- [x] 完成前后端分界评估，结论转化为下方第 1 组待办。

## 0.22.1 本地开发

- [x] 不同独立实例可并行运行，按会话管理日志、关闭与退出诊断；同目录多开保护保留。
- [x] 启动恢复改为本地优先，在线版本列表在下载页按需读取，超时和错误不占用全局任务。
- [ ] 在真实 Minecraft、账号和不同显卡环境补验多开；不以组件/测试进程检查代替完整游戏验收。
- 本分支只更新本地完整版与 Lite，不发布 GitHub Release。

## 待办

### 1. 前后端边界

- [ ] **让边界可执行**：在 `scripts/check-ui-boundaries.py` 增加规则——`Components/`、`Presentation/` 禁止 `new HttpClient`、`Process.Start`、`ProtectedData`、`JsonSerializer`（按文件白名单放行 `InlineFilePicker` 等）；Core 禁止新增用户可见中文（允许异常消息、诊断规则与日志）。先止住继续越界。
- [ ] **迁移认证与账号安全**：把 `Authentication/MicrosoftAccountService`、`MuaAccountService`、`MinecraftOwnershipGuard`、`ProtectedAccountStorage`、`AvatarRenderer` 的下载部分移入 Core（或独立 `MCUGLauncher.Infrastructure`），UI 只保留 `EmbeddedMicrosoftOAuthUI`（WebView2）。
- [ ] **用户文案回到 UI**：Core 只返回状态、枚举和计数；`OperationRecord.StateLabel`/`Summary`/`Detail`、`ContentService.DisplayTitle`、`ModPreflight.Summary`、`PackUpgrade` 的显示名由展示层生成，相关测试改为断言状态值。
- [ ] **补用例层**：为启动、安装、更新、卸载、收藏各引一个用例对象，控制器只调用例，减少对 `ContentService`（8 个 UI 文件）、`JavaDiscovery`（6 个文件）等服务的分散调用。
- [ ] **拆 Core 项目**：`Domain` 与 `Infrastructure`（CmlLib、HTTP、文件）分项目，域层可完全离线测试。

### 2. MainWindow 剩余拆分

- [ ] 收窄仍通过 `LauncherWorkspace` 协作的模块：实例概览、资源工作区、任务协调、启动协调。
- [ ] 拆分超过 400 行的模块：`LaunchCoordinator`（约 540 行）、`BookmarkLibraryController`（约 510 行）、`ResourceBrowserController`（约 480 行）、`ExperienceController`（约 407 行）。

### 3. 验证与发布

- [ ] 补齐 `IntegrationChecks`、真实微软 / MUA 授权与完整游戏启动矩阵的验收环境。
- [ ] 合并到 `main` 后按 [版本号控制](docs/VERSIONING.md) 打 `v0.22.0` 标签，从标签构建完整版与 Lite 版并记录哈希。
- [ ] 在 GitHub Releases 上传 0.22.0 附件与发布说明（可参考 [docs/releases/0.22.0.md](docs/releases/0.22.0.md)）。

## 整合与拆分检查要点

适用于后续任何分支合并与结构调整：

- 保留用户可见功能、错误处理和数据兼容行为；不改存储目录、加密熵、JSON 字段名与实例格式。
- 按功能与责任组件迁移改动，避免用旧版整文件替换当前实现，不重新引入 `partial MainWindow`。
- 冲突或行为不明确时，先查对应 Core 服务、所属模块与 `test/*Checks`，再决定迁移方式。
- 新增菜单或页面走 `LauncherMenuRegistry` 与 `LauncherPage` 边界，不复制下载分类或导航索引逻辑。
- 每批整合后运行 Release 构建、相关检查、源码格式、布局与差异检查；文档、CHANGELOG 与版本号同步更新。
- 版本号只改 `Directory.Build.props` 的 `McugVersion`，规则见 [版本号控制](docs/VERSIONING.md)；类型或目录调整后同步 `docs/classes`，`python scripts/check-class-docs.py` 必须通过。
