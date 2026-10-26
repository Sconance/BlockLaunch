namespace BlockLaunch;
public partial class MainWindow
{
    void BuildKeyboardNavigation()
    {
        UseLayoutRounding=true; SnapsToDevicePixels=true;
        HomeNav.ToolTip="启动首页"; downloadNav.ToolTip="下载";
        SearchQuery.ToolTip="搜索资源"; vanillaQuery.ToolTip="搜索游戏版本";
    }
}

