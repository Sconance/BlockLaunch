# 重构路线图

当前主干是 `test`；`refactor`（`db0ab6a`）已在 `145bf37` 合入，结构以 `test` 为准，详见 [refactor 分支整合记录](docs/REFACTOR_INTEGRATION.md)。目标仍是继续拆分 `MainWindow`：它已不是一个大类，但页面之外的模块协作仍在收窄。

## 已完成

- [x] 原生窗口 shell 化：`MainWindow.xaml.cs` 只保留 35 行，九个页面与标题区拆成独立 XAML。
- [x] `Composition/LauncherWorkspace` 组合根、`LauncherControls` 强类型绑定、`LauncherPageRouter` 编译期页面路由。
- [x] 41 个功能模块从窗口迁出；12 个组件改为接收控件、状态与回调。
- [x] 版本设置拆分为概览、启动、加载器、Mod、导出五个独立页面。
- [x] 0.20.1 / 0.20.2 功能整合：资源工作区、心形收藏与筛选、八个菜单扩展区、加载器概览、品牌资源。
- [x] `refactor` 分支全量并入，文档规则与版本号控制统一。

## 待办

- [ ] 收窄仍通过 `LauncherWorkspace` 协作的模块：实例概览、资源工作区、任务协调、启动协调。
- [ ] 拆分 `Components/` 中超过 400 行的模块：`LaunchCoordinator`（约 540 行）、`CollectionsController`（约 510 行）、`ContentController`（约 480 行）、`ExperienceController`（约 407 行）。
- [ ] 前后端分离：Core 只暴露明确操作与数据，UI 不直接读取窗口或工作区状态。
- [ ] 补齐 `IntegrationChecks`、真实微软 / MUA 授权与完整游戏启动矩阵的验收环境。
- [ ] 为新增的版本校验补充发版演练记录（打标签、从标签构建）。

## 整合与拆分检查要点

沿用 `refactor` 的整合规则，并适用于后续任何分支合并：

- 保留用户可见功能、错误处理和数据兼容行为；不改存储目录、加密熵、JSON 字段名与实例格式。
- 按功能与责任组件迁移改动，避免用旧版整文件替换当前实现，不重新引入 `partial MainWindow`。
- 冲突或行为不明确时，先查对应 Core 服务、所属模块与 `test/*Checks`，再决定迁移方式。
- 新增菜单或页面走 `LauncherMenuRegistry` 与 `LauncherPage` 边界，不复制下载分类或导航索引逻辑。
- 每批整合后运行 Release 构建、相关检查、源码格式、布局与差异检查；文档、CHANGELOG 与版本号同步更新。
- 版本号只改 `Directory.Build.props` 的 `McugVersion`，规则见 [版本号控制](docs/VERSIONING.md)。
