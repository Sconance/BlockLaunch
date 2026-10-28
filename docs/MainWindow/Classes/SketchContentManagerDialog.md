# SketchContentManagerDialog

构建并管理首页“版本内容管理”对话框，支持选择游戏目录及实例，浏览存档、模组、资源包、光影、服务器收藏、截图和蓝图，并提供对应的版本操作。

- **主要入口：** `Open(string initialKey)`。
- **协作：** 通过 `MainWindow` 调用实例刷新、目录切换、版本设置、导入、导出及内容管理入口；支持传递当前实例标识的 `InstanceContent` 菜单扩展；通过显式传入的实例列表和首页刷新回调保持与主窗口界面同步。
- **行为边界：** `MainWindow` 保留窄入口 `OpenSketchManager`；对话框关闭时清理当前实例标记、刷新实例列表并更新首页版本区。
- **源码：** [`SketchContentManagerDialog.cs`](../../../src/MCUGLauncher/Presentation/SketchContentManagerDialog.cs)。
