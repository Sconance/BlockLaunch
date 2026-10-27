using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    FrameworkElement BuildLoaderOverview(Border original)
    {
        var old = (StackPanel)original.Child;
        var stable = old.Children.OfType<WrapPanel>().First(p => p != LoaderActions);
        var actions = LoaderActions.Children.OfType<Button>().Concat(stable.Children.OfType<Button>()).ToArray();
        var page = new StackPanel();
        page.Children.Add(Heading("版本安装配置"));
        page.Children.Add(Hint("先在原版游戏中选择 Minecraft 版本，再安装一种兼容的加载器。Forge、NeoForge、Fabric、Quilt 不能混合安装。"));
        foreach (var action in actions)
        {
            ((Panel)action.Parent).Children.Remove(action);
            var loader = action.Tag?.ToString() ?? (action.Content.ToString()!.Contains("Fabric") ? "fabric" : "quilt");
            var title = loader switch
            {
                "forge" => "Forge",
                "neoforge" => "NeoForge",
                "fabric" => "Fabric",
                _ => "Quilt"
            };
            var description = loader switch
            {
                "forge" => "选择可用版本后安装，适用于 Forge 模组。",
                "neoforge" => "选择可用版本后安装；支持范围以版本列表为准。",
                "fabric" => "安装适合当前游戏版本的稳定版加载器。",
                _ => "安装适合当前游戏版本的稳定版加载器。"
            };
            action.Content = loader is "fabric" or "quilt" ? "安装稳定版" : "选择版本";
            action.ToolTip = title + " · " + description;
            var row = new DockPanel();
            DockPanel.SetDock(action, Dock.Right);
            row.Children.Add(action);
            var text = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            text.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold });
            text.Children.Add(Hint(description));
            row.Children.Add(text);
            var card = FeatureCard(row);
            card.Padding = new Thickness(14, 10, 14, 10);
            card.Margin = new Thickness(0, 0, 0, 8);
            page.Children.Add(card);
        }
        var localInstaller = old.Children.OfType<Button>().Single();
        old.Children.Remove(localInstaller);
        var advanced = new StackPanel();
        advanced.Children.Add(localInstaller);
        advanced.Children.Add(Hint("使用官方本地安装器时，请将安装目录设为游戏根目录，安装后刷新版本。"));
        page.Children.Add(new Expander { Header = "高级 · 本地官方安装器", Content = advanced, Margin = new Thickness(0, 10, 0, 0) });
        return page;
    }
}
