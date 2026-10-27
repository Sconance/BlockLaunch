using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;

internal sealed class VersionSettingsWindowView
{
    static readonly (string Icon, string Name)[] Pages =
    [
        ("◇", "概览"),
        ("⚙", "设置"),
        ("✎", "修改"),
        ("▦", "Mod 管理"),
        ("□", "导出")
    ];

    readonly InstanceDescriptor descriptor;
    readonly PackRequirements requirements;
    readonly ScrollViewer scroll;
    readonly RadioButton[] navigation;

    public InlineDialog Window
    {
        get;
    }
    public TextBlock Title
    {
        get;
    }
    public StackPanel Content
    {
        get;
    }

    public VersionSettingsWindowView(
        Window owner,
        string displayName,
        InstanceDescriptor descriptor,
        PackRequirements requirements,
        Action<string> pageSelected,
        Action close)
    {
        this.descriptor = descriptor;
        this.requirements = requirements;
        var title = "版本设置 · " + displayName;
        Window = new InlineDialog
        {
            Owner = owner,
            Title = title,
            Width = 920,
            Height = 710,
            MinWidth = 820,
            MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(226, 237, 250)),
            FontFamily = owner.FontFamily
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition());
        Window.Content = layout;

        var bar = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
            LastChildFill = true,
            Visibility = Visibility.Collapsed
        };
        var back = new Button
        {
            Content = "← 返回",
            Foreground = Brushes.White,
            Background = Brushes.Transparent
        };
        back.Click += (_, _) => close();
        DockPanel.SetDock(back, Dock.Left);
        bar.Children.Add(back);
        Title = new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontSize = 17,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 12, 10, 12)
        };
        bar.Children.Add(Title);
        layout.Children.Add(bar);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(body, 1);
        layout.Children.Add(body);

        var menu = new StackPanel { Background = Brushes.White };
        body.Children.Add(menu);
        Content = new StackPanel { Margin = new Thickness(16) };
        scroll = new ScrollViewer
        {
            Content = Content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(scroll, 1);
        body.Children.Add(scroll);

        navigation = new RadioButton[Pages.Length];
        for (var i = 0; i < Pages.Length; i++)
        {
            var page = Pages[i];
            var nav = new RadioButton
            {
                Style = NavigationMenuStyle.Create(),
                Content = page.Icon + "   " + page.Name,
                GroupName = "VersionPages",
                Margin = new Thickness(0, 7, 0, 0),
                FontSize = 14
            };
            nav.Checked += (_, _) =>
            {
                try
                {
                    pageSelected(page.Name);
                }
                catch (Exception ex)
                {
                    Content.Children.Clear();
                    Content.Children.Add(new TextBlock
                    {
                        Text = ex.Message,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.SlateGray,
                        Margin = new Thickness(0, 5, 0, 10)
                    });
                }
            };
            navigation[i] = nav;
            menu.Children.Add(nav);
        }
    }

    public void SelectPage(string page)
    {
        var index = page switch
        {
            "设置" => 1,
            "修改" => 2,
            "Mod 管理" => 3,
            "导出" => 4,
            _ => 0
        };
        navigation[index].IsChecked = true;
    }

    public void BeginPage()
    {
        Content.Children.Clear();
        scroll.ScrollToTop();
        Content.Children.Add(BuildHeader());
    }

    static Border VersionCard(UIElement child) => new()
    {
        Background = Brushes.White,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 12),
        Child = child
    };

    Border BuildHeader()
    {
        var row = new DockPanel();
        if (File.Exists(descriptor.IconPath))
        {
            var icon = new Image
            {
                Source = new BitmapImage(new Uri(descriptor.IconPath)),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 14, 0)
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);
        }
        else
        {
            var icon = new TextBlock
            {
                Text = requirements.Loader == "vanilla" ? "▣" : "◇",
                FontSize = 33,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
                Margin = new Thickness(0, 0, 14, 0)
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);
        }

        var info = new StackPanel();
        info.Children.Add(new TextBlock { Text = descriptor.DisplayName, FontSize = 16, FontWeight = FontWeights.SemiBold });
        info.Children.Add(new TextBlock
        {
            Text = "Minecraft " + requirements.Minecraft + " · " + requirements.Loader + (requirements.LoaderVersion.Length > 0 ? " " + requirements.LoaderVersion : ""),
            FontSize = 11,
            Foreground = Brushes.SlateGray,
            Margin = new Thickness(0, 3, 0, 0)
        });
        row.Children.Add(info);
        return VersionCard(row);
    }

}
