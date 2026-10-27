using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed record DownloadCategoryIcon(string Glyph, ImageSource? Image = null);

public partial class MainWindow
{
    readonly Dictionary<string, DownloadCategoryIcon> downloadCategoryIcons = new()
    {
        ["原版游戏"] = new("▣"),
        ["Mod"] = new("✚"),
        ["整合包"] = new("▦"),
        ["数据包"] = new("≡"),
        ["资源包"] = new("▧"),
        ["光影包"] = new("☼"),
        ["本地整合包"] = new("⇩"),
        ["模组加载器"] = new("⚙")
    };

    FrameworkElement DownloadCategoryVisual(string name)
    {
        var icon = downloadCategoryIcons[name];
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        FrameworkElement artwork = icon.Image is { } source
            ? new Image { Source = source, Width = 20, Height = 20, Stretch = Stretch.Uniform }
            : new TextBlock { Text = icon.Glyph, FontSize = 19, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(artwork);
        var label = new TextBlock
        {
            Text = name == "Mod" ? "模组" : name,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(label, 1);
        row.Children.Add(label);
        return row;
    }
}
