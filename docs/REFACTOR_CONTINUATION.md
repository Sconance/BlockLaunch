# refactor 组件在 test 中的延续

参考基线固定为 `db0ab6af7cde32ca685de7ff3371cfefdf308f18`。本轮从本地 `test` 的 `156208e` 继续，只向 `test` 提交，不移动、合并或改写 `refactor` 的分支指针，也不修改正式版本号。

## 完整复用的组件

以下 11 个完整文件与参考提交逐字一致，比较仅统一 UTF-8 BOM 与 CRLF/LF。六个已在此前迁移，五个在本轮接入。不是只比较类名或文件名。

| refactor 的 MainWindow/ 下路径 | test 的 src/MCUGLauncher/ 下路径 |
| --- | --- |
| Builders/LaunchScriptBuilder.cs | Builders/LaunchScriptBuilder.cs |
| Builders/OfflineSkinPackBuilder.cs | Builders/OfflineSkinPackBuilder.cs |
| Controllers/GameWindowBehaviorController.cs | Components/GameWindowBehaviorController.cs |
| Controllers/InstanceSearchController.cs | Components/InstanceSearchController.cs |
| Models/PlayerIdentityState.cs | Models/PlayerIdentityState.cs |
| Builders/LaunchOptionsForm.cs | Views/Forms/LaunchOptionsForm.cs |
| Presentation/ContentUpdateSelectionView.cs | Views/Updates/ContentUpdateSelectionView.cs |
| Presentation/ContentUpdatePreviewView.cs | Views/Updates/ContentUpdatePreviewView.cs |
| Initialization/UtilityWindowInitializer.cs | Initialization/UtilityWindowInitializer.cs |
| Initialization/LauncherTooltipInitializer.cs | Initialization/LauncherTooltipInitializer.cs |
| Styling/NavigationMenuStyle.cs | Styling/NavigationMenuStyle.cs |

`scripts/refactor-reuse.json` 记录参考来源与内容哈希。`scripts/check-refactor-reuse.py` 检查工作区，也可用 `--revision origin/test` 检查已推送的目标文件。CI 无需下载另一个分支即可检查固定基线。后续若有正确性修复，应明确改为“适配组件”并更新来源说明，不应为保持一致而保留缺陷。

## 延续原实现、适配依赖的组件

- `Presentation/LaunchOptionsDialog.cs`：沿用同名参考组件，保留启动选项克隆、校验、全局字段联动、个性化与离线皮肤设置；用 `LaunchOptionsActions` 接收配置与保存操作。
- `Presentation/InstancePackExportDialog.cs`：沿用同名参考组件，保留导出选项、保守的存档默认值、保存选择和任务执行顺序；接收与导出有关的少量回调。
- `Presentation/FeatureDialogBuilder.cs`：沿用参考布局、尺寸、按钮分组和准备逻辑；接收普通 Window 所有者与主题配置回调。
- `Presentation/SketchContentManagerDialog.cs`：按参考的同名类和 Open 入口提取内容管理；数据读取、管理命令、生命周期绑定分别传入，不接收整个 MainWindow 或 LauncherWorkspace。继续使用 test 的菜单扩展宿主和已验收的实例名称同步，而不是覆盖回旧实现。
- `Components/UpdatesController.cs`：延续参考 `ContentUpdateController` 的检查、选择、预览、确认和入队流程，直接使用上表中的两个原始视图；保留 test 已有的事件入口名称，改用 `ContentUpdateActions`。
- `Builders/ContentSearchFilterBuilder.cs`：沿用参考实现，仅将 MainWindow 的通用按钮工厂改为现有 `FeatureUiElements`。重置回调继续清除 test 的收藏展示条件。

`Composition/InstanceDialogComposition.cs` 负责把既有服务连接到上述组件。适配代码属于组合层，不把新的业务字段重新塞回主窗口。上述文件有必要的依赖差异，不能宣称完整文件逐字相同。

## 保留的 test 差异

- 精简的原生窗口和九个独立 XAML 页面继续保留。
- 版本设置的五个独立页面继续保留。
- Esc 优先关闭展开选择器的修复继续保留。
- 嵌套编辑后实例列表、选中项和首页名称同步继续保留；内容管理关闭时解除订阅。
- 资源收藏、兼容筛选、现有下载/认证协议及实例数据格式保持原有实现；Core 无改动。

## 检查要求

运行全部无需真实游戏环境的检查；额外断言参考启动设置对话框可以保存并同步全局字段，参考整合包导出对话框保留五个选项且默认不包含存档。已有更新确认、目录切换、名称同步、Esc 路由、拖放与菜单扩展断言继续执行。

`test` 的推送现已触发 Windows CI，并加入固定复用基线、UI 边界及适合 CI 的组件检查。真实授权、完整游戏启动矩阵、多显示器 DPI 与本轮原生桌面点击不能由这些检查代替。
