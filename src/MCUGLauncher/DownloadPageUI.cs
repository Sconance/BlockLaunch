using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly ContentControl downloadHost = new();
    readonly StackPanel vanillaGroups = new();
    readonly TextBox vanillaQuery = new()
    {
        ToolTip = "搜索游戏版本",
        Margin = new Thickness(0, 0, 0, 14)
    };
    readonly Dictionary<string, RadioButton> downloadCategories = new();
    FrameworkElement? resourcePage;
    FrameworkElement? vanillaPage, packImportPage, loaderPage;
    string downloadCategory = "原版游戏";
    ControlTemplate? downloadRowTemplate;
    static readonly HashSet<string> AprilVersions = new(StringComparer.OrdinalIgnoreCase)
    {
        "15w14a",
        "1.RV-Pre1",
        "3D Shareware v1.34",
        "20w14infinite",
        "22w13oneblockatatime",
        "23w13a_or_b",
        "24w14potato",
        "25w14craftmine",
        "2.0"
    };

    void BuildDownloadPage(StackPanel previous)
    {
        var cards = previous.Children.Cast<UIElement>().ToArray();
        previous.Children.Clear();
        var selection = (Border)cards[0];
        var selectionBody = (StackPanel)selection.Child;
        var actions = (WrapPanel)selectionBody.Children[4];
        selectionBody.Children.Remove(actions);
        selectionBody.Children.Remove(InstallTarget);
        var target = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        target.Children.Add(InstallTarget);
        target.Children.Add(Hint("选择下方版本后下载；已安装版本可修复。"));
        var selectionRow = new DockPanel();
        DockPanel.SetDock(actions, Dock.Right);
        selectionRow.Children.Add(actions);
        selectionRow.Children.Add(target);
        actions.Margin = new Thickness(15, 0, 0, 0);
        selection.Child = selectionRow;
        selection.Padding = new Thickness(18, 12, 18, 12);
        var vanilla = new DockPanel();
        var vanillaHeading = Heading("原版游戏");
        DockPanel.SetDock(vanillaHeading, Dock.Top);
        vanilla.Children.Add(vanillaHeading);
        DockPanel.SetDock(selection, Dock.Top);
        vanilla.Children.Add(selection);
        var list = new StackPanel
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        var searchRow = new DockPanel();
        searchRow.Children.Add(new TextBlock { Text = "搜索版本", Margin = new Thickness(0, 9, 14, 0) });
        searchRow.Children.Add(vanillaQuery);
        list.Children.Add(searchRow);
        list.Children.Add(vanillaGroups);
        vanilla.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        vanillaPage = vanilla;
        packImportPage = new ScrollViewer
        {
            Content = cards[1],
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        loaderPage = new ScrollViewer
        {
            Content = BuildLoaderOverview((Border)cards[2]),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        resourcePage = (FrameworkElement)((TabItem)MainTabs.Items[2]).Content;
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(148)
        });
        layout.ColumnDefinitions.Add(new());
        var navigation = new StackPanel
        {
            Margin = new Thickness(0, 0, 14, 0)
        };
        foreach (var name in new[] {
            "原版游戏",
            "Mod",
            "整合包",
            "数据包",
            "资源包",
            "光影包",
            "本地整合包",
            "模组加载器"
        }

        )
        {
            var option = new RadioButton
            {
                Content = DownloadCategoryVisual(name),
                ContentTemplate = null,
                GroupName = "DownloadCategory",
                Style = (Style)FindResource("Nav"),
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(8),
                ToolTip = name
            };
            option.Checked += (_, _) => SelectDownloadCategory(name);
            downloadCategories[name] = option;
            navigation.Children.Add(option);
        }

        layout.Children.Add(new ScrollViewer { Content = navigation, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Grid.SetColumn(downloadHost, 1);
        layout.Children.Add(downloadHost);
        ((TabItem)MainTabs.Items[1]).Content = layout;
        vanillaQuery.TextChanged += (_, _) => RenderVanillaDownloads();
        SelectDownloadCategory("原版游戏");
    }

    void RestoreResourcePage()
    {
        if (resourcePage == null || downloadHost.Content != resourcePage)
            return;
        downloadHost.Content = null;
        ((TabItem)MainTabs.Items[2]).Content = resourcePage;
        ContentType.Visibility = Visibility.Visible;
        ((Grid)ContentType.Parent).ColumnDefinitions[1].Width = new GridLength(132);
    }

    void SelectDownloadCategory(string category)
    {
        if (resourcePage == null)
            return;
        RestoreResourcePage();
        ShowResourceResults();
        downloadCategory = category;
        ShowResourceResults();
        downloadHost.Content = category switch
        {
            "原版游戏" => vanillaPage,
            "本地整合包" => packImportPage,
            "模组加载器" => loaderPage,
            _ => null
        };
        if (downloadHost.Content == null)
        {
            ((TabItem)MainTabs.Items[2]).Content = null;
            downloadHost.Content = resourcePage;
            bookmarkPage.Visibility = Visibility.Collapsed;
            resourceSearchPage.Visibility = Visibility.Visible;
            ContentType.Visibility = Visibility.Collapsed;
            ((Grid)ContentType.Parent).ColumnDefinitions[1].Width = new GridLength(0);
            var type = category switch
            {
                "Mod" => 0,
                "资源包" => 1,
                "整合包" => 2,
                "光影包" => 3,
                "数据包" => 4,
                _ => 0
            };
            if (ContentType.SelectedIndex != type)
                ContentType.SelectedIndex = type;
            if (LocalVersions().Count == 0 && resourceGame.Text == "跟随实例")
                resourceGame.Text = "全部";
        }

        if (!downloadCategories[category].IsChecked.GetValueOrDefault())
            downloadCategories[category].IsChecked = true;
        if (category == "原版游戏")
            RenderVanillaDownloads();
        if (category == "模组加载器")
            PrepareLoaderConfiguration();
    }

    void RenderVanillaDownloads()
    {
        if (vanillaPage == null)
            return;
        var expanded = vanillaGroups.Children.OfType<Border>().Select(b => b.Child).OfType<Expander>().Where(e => e.IsExpanded).Select(e => e.Header.ToString()!.Split('（')[0]).ToHashSet();
        vanillaGroups.Children.Clear();
        bool Type(VersionRow r, string type) => r.Type.Equals(type, StringComparison.OrdinalIgnoreCase);
        var remote = rows.Where(r => Type(r, "release") || Type(r, "snapshot") || r.Type.Contains("old", StringComparison.OrdinalIgnoreCase)).ToList();
        var matching = remote.Where(r => r.Id.Contains(vanillaQuery.Text.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (matching.Count == 0)
        {
            vanillaGroups.Children.Add(Hint(remote.Count == 0 ? "版本列表尚未加载。可刷新列表，或检查下载与网络设置。" : "没有匹配的游戏版本。"));
        }

        var refresh = ActionButton("刷新版本列表", async (_, _) => await Run(Refresh, "刷新游戏版本"));
        if (remote.Count == 0)
            vanillaGroups.Children.Add(refresh);
        var local = LocalVersions();
        Button VersionButton(VersionRow version)
        {
            var row = new StackPanel();
            row.Children.Add(new TextBlock { Text = version.Id, FontWeight = FontWeights.SemiBold, FontSize = 14 });
            row.Children.Add(new TextBlock { Text = (AprilVersions.Contains(version.Id) ? "愚人节版" : Type(version, "release") ? "正式版" : Type(version, "snapshot") ? "预览版" : "远古版") + " · " + (version.Published is { } published ? "发布于 " + published.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : "发布时间未知") + (local.Contains(version.Id) ? " · 已安装" : ""), Foreground = Brushes.SlateGray, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
            var button = ActionButton("", (_, _) =>
            {
                if (busy)
                    return;
                downloadVersion = version.Id;
                UpdateTargets();
                RenderVanillaDownloads();
            });
            var visual = new DockPanel();
            visual.Children.Add(new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(7), Background = new SolidColorBrush(Type(version, "release") ? Color.FromRgb(221, 238, 224) : Color.FromRgb(255, 235, 211)), Margin = new Thickness(0, 0, 13, 0), Child = new TextBlock { Text = "◇", FontSize = 25, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
            visual.Children.Add(row);
            button.Template = downloadRowTemplate ??= (ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' CornerRadius='8' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Left' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='0.8'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.4'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
            button.Padding = new Thickness(9, 6, 9, 6);
            button.Margin = new Thickness(0, 2, 0, 2);
            button.ContentTemplate = null;
            button.Content = visual;
            button.ToolTip = "选择 " + version.Id;
            if (downloadVersion == version.Id)
                button.Background = new SolidColorBrush(Color.FromRgb(221, 238, 248));
            return button;
        }

        void Group(string title, IEnumerable<VersionRow> source, bool latest = false)
        {
            var versions = source.ToList();
            var items = new StackPanel();
            var populated = false;
            void Populate()
            {
                if (populated)
                    return;
                populated = true;
                foreach (var row in versions)
                    items.Children.Add(VersionButton(row));
                if (versions.Count == 0)
                    items.Children.Add(Hint("没有匹配版本"));
            }

            var expander = new Expander
            {
                Header = title + (latest ? "" : "（" + versions.Count + "）"),
                Content = items,
                IsExpanded = latest || expanded.Contains(title) || vanillaQuery.Text.Trim().Length > 0,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            expander.Expanded += (_, _) => Populate();
            if (expander.IsExpanded)
                Populate();
            var card = FeatureCard(expander);
            card.Padding = new Thickness(12);
            card.CornerRadius = new CornerRadius(7);
            card.Margin = new Thickness(0, 0, 0, 9);
            vanillaGroups.Children.Add(card);
        }

        var latestRows = new[] {
            matching.FirstOrDefault(r => Type(r, "release") && !AprilVersions.Contains(r.Id)),
            matching.FirstOrDefault(r => Type(r, "snapshot") && !AprilVersions.Contains(r.Id))
        }.OfType<VersionRow>().DistinctBy(r => r.Id);
        Group("最新版本", latestRows, true);
        Group("正式版", matching.Where(r => Type(r, "release") && !AprilVersions.Contains(r.Id)));
        Group("预览版", matching.Where(r => Type(r, "snapshot") && !AprilVersions.Contains(r.Id)));
        Group("远古版", matching.Where(r => r.Type.Contains("old", StringComparison.OrdinalIgnoreCase) && !AprilVersions.Contains(r.Id)));
        Group("愚人节版", matching.Where(r => AprilVersions.Contains(r.Id)));
    }
}
