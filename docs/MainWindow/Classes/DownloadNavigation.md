# DownloadCategoryIcon

下载分类栏的图标定义，允许后续替换为图片而不改动导航与下载行为。

## 主要成员

- `DownloadCategoryIcon(string Glyph, ImageSource? Image)`：`Glyph` 是缺图时的文字图标，`Image` 为可选的 WPF 图片源。
- `MainWindow.downloadCategoryIcons`：八种下载分类的默认图标表。
- `DownloadCategoryVisual(string name)`：按分类生成图标加文字的导航内容。

## 协作

- `UI/Downloads/DownloadPageUI.cs` 在构建左侧分类导航时调用，`ContentTemplate` 置空以避免公共文本模板覆盖自定义内容。
- 图标只影响呈现，分类选择仍交由 `SelectDownloadCategory` 处理。

## 源码

[`DownloadNavigation.cs`](../../../src/MCUGLauncher/MainWindow/Models/DownloadNavigation.cs)
