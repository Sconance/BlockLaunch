# OriginalTheme

`WindowThemeStyler` 的私有嵌套状态类型，保存控件应用主题前的原始背景和前景画刷。

- **用途：** 通过 `ConditionalWeakTable` 与 WPF 对象关联，使样式器能够在深浅主题切换时恢复原始外观且不延长控件生命周期。
- **访问范围：** 仅由 `WindowThemeStyler` 使用。
- **源码：** [`WindowThemeStyler.cs`](../../../src/MCUGLauncher/MainWindow/Styling/WindowThemeStyler.cs)。
