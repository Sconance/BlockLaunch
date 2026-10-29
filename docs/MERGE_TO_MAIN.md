# 合并到主分支前的说明（0.22.0）

## 目的

把 `test` 分支上的 0.22.0 合并到 `main`。本文说明版本、相对 `main` 的变化、兼容性、验证结果、已知限制与合并步骤，供合并前复核与合并后发版使用。

## 版本与分支

| 项 | 值 |
| --- | --- |
| 版本 | **0.22.0**，唯一来源 `Directory.Build.props` 的 `<McugVersion>` |
| 来源分支 | `test`（合并时以其最新提交为准） |
| 目标分支 | `main`（当前为 `a056733`，即 v0.20.2） |
| `refactor` | 已废弃，不再合并或发版；历史记录见 [历史归档](archive/README.md)，源码保留在 `db0ab6a` |

## 相对 main 的变化

1. **0.21.0**：`refactor` 全量并入（结构以 `test` 为准，86 个 `partial MainWindow` 与 51 篇逐类文档退役）；版本号改为单一来源，新增 `scripts/check-version.py` 与 CI 校验；修复诊断信息与界面中的旧版本号。
2. **0.22.0**：`Components/` 按九个功能域重组，10 个控制器按职责改名；142 篇类文档归档到 `docs/classes/`，新增 `scripts/check-class-docs.py`；refactor 时代的四篇文档移入 `docs/archive/`；补本说明与路线图。
3. **基础设施**：`scripts/build.ps1` 从 `McugVersion` 读取发版文件名；CI 在布局检查后依次执行版本元数据、类文档、UI 边界、复用基线、源码格式、构建与回归检查。

## 兼容性

- 用户数据不变：存储目录 `BlockLaunch`、账号加密熵、JSON 字段名、实例与整合包格式均未修改。
- 菜单扩展接口不变：`LauncherMenuRegistry`、`LauncherMenuPage`、`LauncherMenuArea`、`LauncherMenuContext` 未改名，源码级扩展无需调整。
- 类与目录属于内部实现：重命名不产生对外 API 变化；`docs/classes` 已同步。
- 版本元数据自动跟随：程序集版本、User-Agent、诊断信息与关于页均读取 `ProductInfo.Version`。

## 验证

已通过的 CI（`Windows build`，含版本、类文档、UI 边界、复用、格式、构建与回归检查）：

- [run #40840759051](https://github.com/Sconance/MCUGLauncher/actions/runs/40840759051)（`bf9cda4`，发版脚本修复后）
- [run #40842587357](https://github.com/Sconance/MCUGLauncher/actions/runs/40842587357)（`13163c1`，类文档）
- [run #40844971279](https://github.com/Sconance/MCUGLauncher/actions/runs/40844971279)（`697a3b8`，组件重组）

本地验证：Release 构建 0 警告 0 错误；`check-version`、`check-class-docs`、`check-ui-boundaries`、`check-layout`、`check-refactor-reuse` 全部通过；20 项无需真实游戏环境的检查通过（`DiagnosticChecks` 因本机缺少符号链接特权停在 `Program.cs:111`，与代码无关）。

尚未覆盖：`IntegrationChecks`、真实微软 / MUA 人工授权、完整游戏启动矩阵、原生多 DPI 与鼠标验收、正式 EXE 的实机点击。

## 已知限制

- 前后端边界仍偏软：认证逻辑在 UI 程序集，Core 仍输出用户文案，UI 仍有 HTTP/文件操作；改进项已写入 [重构路线图](../TODO.md) 第 1 组。
- `Components/` 仍有 400 行以上模块，`LauncherWorkspace` 仍被多个模块直接持有。
- 0.21.0 标签已发布，0.22.0 必须使用新标签，不移动旧标签。

## 合并步骤

```powershell
git switch main
git pull origin main
git merge --no-ff test -m "merge: 0.22.0 组件功能域重组与文档归档"
python scripts/check-version.py
./scripts/build.ps1 -Publish
```

合并后推送 `main`，等待 CI 通过，再按 [版本号控制](VERSIONING.md) 打标签并发版：

```powershell
git tag -a v0.22.0 -m "MCUG Launcher 0.22.0"
python scripts/check-version.py --tag
git push origin main v0.22.0
```

## 合并后

- 上传 Release 附件（完整版与 Lite 版 EXE）并记录 SHA-256，发布说明可参考 `artifacts/release/RELEASE-0.21.0.md` 的格式；
- 在 `main` 上优先推进 TODO 第 1 组，先落地“前后端边界检查”再迁移认证与文案；
- `test` 分支可保留为集成分支，`refactor` 不再恢复。

## 合并前检查清单

- [ ] 复核 `git diff main...test`，确认没有回退已删除的 `partial MainWindow` 与旧文档；
- [ ] `python scripts/check-version.py`：版本、CHANGELOG、README 一致；
- [ ] `python scripts/check-class-docs.py`：类文档覆盖与索引完整；
- [ ] `python scripts/check-ui-boundaries.py`、`check-layout.py`、`check-refactor-reuse.py` 通过；
- [ ] CI 在 `test` 最新提交上为绿；
- [ ] 确认数据兼容（存储目录、加密熵、JSON 字段、实例格式未变）；
- [ ] 合并后打 `v0.22.0` 标签并从标签构建，发布附件与哈希。
