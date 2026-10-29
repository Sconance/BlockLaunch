# 参与改进

欢迎提交问题与改进建议。Issue 请包含软件和系统版本、复现步骤、预期与实际结果，截图和日志先脱敏。

代码修改保持范围清晰，说明目的与验证。不要提交 bin、obj、账号文件、Token、私人路径、游戏文件或存档。涉及启动、认证、下载或实例删除时补充针对性检查。

源码放在 `src/`，检查项目放在 `test/`，界面预览放在 `tools/`。
构建和检查命令见 [构建与目录说明](docs/BUILD.md)。迁移或新增项目后运行
`python scripts/check-layout.py`，核对引用和资源路径。

项目整体许可证尚未指定，复用或再分发前请与维护者确认。第三方组件遵循各自许可证。

开发入口、功能定位与编码约定见 [开发导览](docs/DEVELOPMENT.md)。提交前执行源码格式检查，并按改动范围运行行为检查。

## 分支与版本

- `test` 是当前重构主干；`refactor` 已全量并入（结构以 `test` 为准），不再单独递增版本号。
- 合并或迁移其他分支时按功能与责任组件迁移，不使用旧版整文件替换实现，不重新引入 `partial MainWindow`；检查要点见 [重构路线图](TODO.md)。
- 版本号只改 `Directory.Build.props` 的 `McugVersion`，源码和界面读取 `ProductInfo.Version`；发版前运行 `python scripts/check-version.py`（需要标签时加 `--tag`），规则见 [版本号控制](docs/VERSIONING.md)。
- 新增页面与栏目沿用 [菜单扩展说明](docs/MENU_EXTENSIONS.md) 与 `LauncherPage` 动作边界；源码格式、布局检查、UI 边界检查与相关 `test/*Checks` 必须通过。
