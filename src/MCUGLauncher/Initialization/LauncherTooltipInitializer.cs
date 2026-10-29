using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class LauncherTooltipInitializer(
    RadioButton homeNav,
    RadioButton downloadNav,
    TextBox searchQuery,
    TextBox vanillaQuery)
{
    public void Build()
    {
        homeNav.ToolTip = "启动首页";
        downloadNav.ToolTip = "下载";
        searchQuery.ToolTip = "搜索资源";
        vanillaQuery.ToolTip = "搜索游戏版本";
    }
}
