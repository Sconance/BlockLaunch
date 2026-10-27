# LauncherMenuRegistry

源码级菜单栏目注册表。定义可挂载的菜单区域、上下文、页面工厂，并按区域保存已注册页面。

## 主要成员

- `LauncherMenuArea`：顶部、下载、设置、更多、资源详情、主页、版本设置、实例内容八个区域。
- `LauncherMenuContext(MainWindow Window, string? InstanceId, ContentProject? Resource)`：页面工厂可用的宿主信息。
- `LauncherMenuPage(string Id, string Title, Func<LauncherMenuContext, FrameworkElement> CreateContent)`：栏目定义，另有 `Glyph`、`Icon`、`Order`。
- `LauncherMenuRegistry.Pages(LauncherMenuArea)`：按顺序返回该区域的栏目。
- `LauncherMenuRegistry.Register(...)`：校验区域、ID 唯一性和必需字段后登记栏目并通知订阅者。
- `Changed` 事件：宿主用于在注册变化时重建入口。

## 协作

- `UI/Shell/MenuExtensionsUI.cs` 把注册表挂载到窗口的常驻与临时菜单宿主。
- `VersionSettingsWindowView` 与 `SketchContentManagerDialog` 分别承载版本设置和实例内容的扩展入口。
- 页面内容创建失败由宿主转换为页内提示，不中断窗口。

## 行为边界

- 这是源码级扩展接口，不加载远程插件或执行下载的代码。
- 注册不改变内置菜单索引、下载分类或账号逻辑。
- `Register` 拒绝重复 ID、未定义区域和空标题/工厂；调用方应在 UI 线程操作控件。

## 源码

[`LauncherMenuRegistry.cs`](../../../src/MCUGLauncher/MainWindow/Models/LauncherMenuRegistry.cs)
