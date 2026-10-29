using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BlockLaunch;

public sealed class LoaderConfigurationView : StackPanel
{
    readonly ComboBox loaderMinecraft = new() { MinWidth = 180, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly Dictionary<string, (ComboBox Versions, Button Install, TextBlock State)> loaderRows = new();
    public ComboBox Minecraft => loaderMinecraft;
    public IReadOnlyDictionary<string, (ComboBox Versions, Button Install, TextBlock State)> Rows => loaderRows;
    public event Action<string>? InstallRequested;

    public LoaderConfigurationView(Button localInstaller, Style cardStyle, Style primaryStyle)
    {
        loaderMinecraft.ItemTemplate = LoaderValueTemplate("Id");
        Children.Add(ViewHeading("版本安装配置"));
        Children.Add(ViewHint("选择 Minecraft 版本和一种兼容的加载器，版本列表来自对应官方服务。"));
        var game = new StackPanel();
        game.Children.Add(new TextBlock { Text = "Minecraft 版本", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
        game.Children.Add(loaderMinecraft);
        var gameCard = new Border { Style = cardStyle, Child = game, Margin = new Thickness(0, 0, 0, 15) };
        gameCard.Padding = new Thickness(14, 10, 14, 10);
        Children.Add(gameCard);
        foreach (var loader in new[] { "forge", "neoforge", "fabric", "quilt" })
        {
            var title = loader switch
            {
                "forge" => "Forge",
                "neoforge" => "NeoForge",
                "fabric" => "Fabric",
                _ => "Quilt"
            };
            var row = new Grid();
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(52)
            });
            row.ColumnDefinitions.Add(new());
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(170)
            });
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(82)
            });
            var icon = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 10, 0), Child = new TextBlock { Text = title[..1], FontSize = 24, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            icon.SetResourceReference(Border.BackgroundProperty, "ThemeInputBrush");
            row.Children.Add(icon);
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 15 });
            var state = new TextBlock { Text = "请选择 Minecraft 版本", FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 8, 0) };
            state.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
            text.Children.Add(state);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            var versions = new ComboBox { IsEnabled = false, ItemTemplate = LoaderValueTemplate("Label"), Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(versions, 2);
            row.Children.Add(versions);
            var install = new Button { Content = "安装" };
            install.Click += (_, _) => InstallRequested?.Invoke(loader);
            install.Style = primaryStyle;
            install.IsEnabled = false;
            install.Margin = new Thickness(0);
            install.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(install, 3);
            row.Children.Add(install);
            loaderRows[loader] = (versions, install, state);
            var card = new Border { Style = cardStyle, Child = row };
            card.Padding = new Thickness(14, 12, 14, 12);
            card.Margin = new Thickness(0, 0, 0, 8);
            Children.Add(card);
        }
        Children.Add(ViewHint("每次安装一种加载器，生成独立版本。Forge、NeoForge、Fabric 和 Quilt 不能混合安装。"));

        var advanced = new StackPanel();
        advanced.Children.Add(localInstaller);
        advanced.Children.Add(ViewHint("本地官方安装器的目标目录应设为游戏根目录，完成后刷新版本。"));
        Children.Add(new Expander { Header = "高级 · 本地官方安装器", Content = advanced, Margin = new Thickness(0, 10, 0, 0) });
    }

    static TextBlock ViewHeading(string text) => new() { Text = text, FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 12), TextWrapping = TextWrapping.Wrap };
    static TextBlock ViewHint(string text) => new() { Text = text, Foreground = System.Windows.Media.Brushes.SlateGray, Margin = new Thickness(0, 5, 0, 10), TextWrapping = TextWrapping.Wrap };
    static DataTemplate LoaderValueTemplate(string property)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(property));
        text.SetBinding(FrameworkElement.ToolTipProperty, new Binding(property));
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        return new DataTemplate { VisualTree = text };
    }

}
