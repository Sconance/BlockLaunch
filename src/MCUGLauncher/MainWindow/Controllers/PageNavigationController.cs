using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class PageNavigationController(
    TabControl tabs,
    TextBlock title,
    TextBlock subtitle,
    Func<bool> isCompactShellReady,
    Func<string> getContentKind,
    Func<string> getDownloadCategory,
    Action<string> navigateDownloads,
    Action<string> openVersionSettings,
    Func<string> selectedVersion,
    Action restoreResourcePage,
    Action<string> selectDownloadCategory,
    Action refreshLocalContent,
    Action refreshOperations)
{
    static readonly string[] Titles = [
        "启动",
        "下载",
        "内容中心",
        "内容管理",
        "启动设置",
        "运行日志",
        "任务与恢复"
    ];

    static readonly string[] Subtitles = [
        "欢迎回来，今天也去创造一点不同。",
        "准备好游戏，让每次出发都更轻松。",
        "为你的世界，加入一点新鲜感。",
        "当前实例的模组与资源包，一目了然。",
        "找到适合你的游戏节奏。",
        "每次启动，都有迹可循。",
        "查看进行中的操作和上次中断的任务。"
    ];

    public void Navigate(object sender, RoutedEventArgs e)
    {
        if (tabs == null || sender is not RadioButton button)
            return;
        var index = int.Parse(button.Tag.ToString()!);
        if (isCompactShellReady() && index == 2)
        {
            navigateDownloads(getContentKind() switch
            {
                "resourcepack" => "资源包",
                "modpack" => "整合包",
                "shader" => "光影包",
                "datapack" => "数据包",
                _ => "Mod"
            });
            return;
        }

        if (isCompactShellReady() && index == 3)
        {
            try
            {
                openVersionSettings(selectedVersion());
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }

            return;
        }

        var category = getDownloadCategory();
        if (index != 1)
            restoreResourcePage();
        else if (category is not ("原版游戏" or "本地整合包" or "模组加载器"))
            selectDownloadCategory(category);
        tabs.SelectedIndex = index;
        title.Text = Titles[index];
        subtitle.Text = Subtitles[index];
        if (index == 3)
            refreshLocalContent();
        if (index == 6)
            refreshOperations();
    }
}
