# FeatureDialogBuilder

构造带滚动容器、统一尺寸与主题初始化的内嵌功能对话框，并在展示前收起重复标题、整理操作按钮。

- **主要入口：** `Build(title, content)`。
- **协作：** 由 `MainWindow.FeatureDialog` 提供共享入口；内容准备与窗口主题配置委托给现有内嵌页面及窗口初始化流程。
- **检查：** `InlineUiChecks` 验证标题处理、按钮分组、按钮尺寸及内嵌页面交互。
- **源码：** [`FeatureDialogBuilder.cs`](../../../src/MCUGLauncher/Presentation/FeatureDialogBuilder.cs)。
