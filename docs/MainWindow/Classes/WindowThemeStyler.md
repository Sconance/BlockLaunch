# WindowThemeStyler

为辅助窗口和内嵌页面应用浅/深主题资源，并保留控件初始颜色以支持重复切换主题。

- **主要入口：** `Apply(Window)`、内嵌页面对应的 `Apply(...)`。
- **协作：** 由主窗口主题及 `UtilityWindowInitializer` 生命周期回调调用；更新主题资源后通知主窗口应用现代标题栏和背景。
- **内部类型：** 持有弱引用表中每个对象的原始背景/前景数据，见 [OriginalTheme](OriginalTheme.md)。
- **源码：** [`WindowThemeStyler.cs`](../../../src/MCUGLauncher/MainWindow/Styling/WindowThemeStyler.cs)。
