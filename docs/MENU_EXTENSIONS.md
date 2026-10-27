# 菜单扩展

在 `src/MCUGLauncher/MainWindow/UI/Shell/MenuExtensions.cs` 的 `ConfigureMenuExtensions` 中注册已实现的页面，不必修改主窗口导航索引、下载分类判断或账号逻辑：

```csharp
partial void ConfigureMenuExtensions(LauncherMenuRegistry menus)
{
    menus.Register(
        LauncherMenuArea.Download,
        new LauncherMenuPage(
            "community.resources",
            "社区资源",
            context => new CommunityResourcesPage(context.Window))
        {
            Glyph = "◇",
            Order = 20
        });
}
```

示例中的 `CommunityResourcesPage` 由调用方实现；启动器不会显示空栏目或无动作按钮。

| 区域 | 显示位置 |
| --- | --- |
| `Top` | 顶部导航 |
| `Download` | 下载页左侧分类，页面显示在右侧并保留分类栏 |
| `Settings` | 设置页附加入口 |
| `More` | 顶部“更多”菜单 |
| `ResourceDetails` | 资源详情的附加标签，选择后才创建内容 |
| `Home` | 主页操作区的附加入口 |
| `VersionSettings` | 版本设置窗口侧边菜单 |
| `InstanceContent` | 版本内容管理窗口分类菜单 |

每个区域中的 `Id` 必须唯一；`Title` 是显示文字，`Order` 决定扩展栏目之间的顺序。重复 ID 和非法区域会明确抛出参数异常。`Glyph` 是默认文字图标，也可通过 `Icon` 提供 WPF `ImageSource`。

页面工厂每次都应返回新的 `FrameworkElement`，不可将同一个 WPF 控件挂载到多个页面。`LauncherMenuContext.Window` 提供主窗口；版本设置与实例内容扩展提供当前 `InstanceId`，资源详情扩展提供当前 `Resource`。扩展内容创建失败时会显示页内错误信息。实例与版本设置菜单在宿主关闭时解除注册变更订阅。

注册表可在窗口初始化时配置，也可在 UI 线程调用 `window.MenuPages.Register(...)`。从后台线程注册会触发对应宿主的控件更新，因此调用方应先切换到 UI 调度器。

该接口是源码级导航扩展，不是第三方插件系统；启动器不会加载远程脚本或执行下载的代码。
