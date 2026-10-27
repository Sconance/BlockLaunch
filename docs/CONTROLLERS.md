# 窗口组件索引

`src/MCUGLauncher/MainWindow/` 按职责组织窗口代码：`UI/` 保存 `MainWindow` 的功能 partial，`Controllers/` 负责交互与业务流程协调，`Presentation/` 负责视图及页面宿主，`Builders/` 构造控件或页面，`Models/` 保存窗口侧展示状态，`Styling/` 处理视觉主题，`Initialization/` 接线生命周期与提示文本。

逐类说明（职责、主要入口、协作关系和源码路径）见[MainWindow 组件文档](MainWindow/README.md)。每个类/记录类型均有独立页面。

## 组合约定

- [`MainWindow.xaml.cs`](../src/MCUGLauncher/MainWindow/MainWindow.xaml.cs) 保留窗口代码后置、初始化顺序及控制器组合入口；`MainWindow.xaml` 与其代码后置保持原位置。
- `UI/` 内的 partial 仍共享 `MainWindow` 状态。XAML 事件及已有窗口调用入口留在 partial 中，再委托给专职组件。
- 控制器和构建器通过窗口服务或明确参数完成操作；版本设置页面共享的上下文由 `VersionSettingsPageContext` 提供。
- 长时间任务沿用窗口 `Run`、取消令牌和下载队列边界，不绕开现有互斥及状态更新。
- 本次归档仅改变文件组织和文档路径，不改变命名空间、XAML 事件名、存储路径或用户可见行为。
