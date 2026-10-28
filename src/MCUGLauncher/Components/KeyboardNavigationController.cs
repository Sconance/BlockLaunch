namespace BlockLaunch;
internal sealed class KeyboardNavigationController
{
    readonly LauncherWorkspace workspace;
    internal KeyboardNavigationController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal void BuildKeyboardNavigation()
    {
        workspace.Window.UseLayoutRounding = true;
        workspace.Window.SnapsToDevicePixels = true;
        workspace.Controls.HomeNav.ToolTip = "启动首页";
        workspace.CompactShellController.downloadNav.ToolTip = "下载";
        workspace.Controls.SearchQuery.ToolTip = "搜索资源";
        workspace.DownloadPageController.vanillaQuery.ToolTip = "搜索游戏版本";
    }
}
