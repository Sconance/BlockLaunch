using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public partial class MainWindow
{
    public LauncherMenuRegistry MenuPages { get; } = new();
    StackPanel? downloadNavigationPanel;
    TabItem? extensionPage;
    partial void ConfigureMenuExtensions(LauncherMenuRegistry menus);

    void BuildMenuExtensionHosts()
    {
        ConfigureMenuExtensions(MenuPages);
        AttachExtensionButtons(LauncherMenuArea.Top, (StackPanel)HomeNavigation.Parent, true);
        if (downloadNavigationPanel != null)
            AttachExtensionButtons(LauncherMenuArea.Download, downloadNavigationPanel);
        if (MainTabControl.Items[4] is TabItem { Content: ScrollViewer { Content: StackPanel settingsPanel } })
            AttachExtensionButtons(LauncherMenuArea.Settings, settingsPanel);
        if (sketchHome != null)
        {
            var homeExtensions = new StackPanel();
            ((DockPanel)sketchHome.Children[0]).Children.Add(homeExtensions);
            AttachExtensionButtons(LauncherMenuArea.Home, homeExtensions);
        }
        if (resourceDetailTabs != null)
            AttachExtensionTabs(LauncherMenuArea.ResourceDetails, resourceDetailTabs);

        void RefreshMore(LauncherMenuArea area)
        {
            if (area != LauncherMenuArea.More)
                return;
            foreach (var item in moreMenu.Items.OfType<MenuItem>().Where(item => item.Tag is LauncherMenuPage).ToArray())
                moreMenu.Items.Remove(item);
            foreach (var page in MenuPages.Pages(area))
            {
                var item = new MenuItem { Header = page.Title, Tag = page };
                item.Click += (_, _) => OpenMenuExtension(page);
                moreMenu.Items.Add(item);
            }
        }
        MenuPages.Changed += RefreshMore;
        RefreshMore(LauncherMenuArea.More);
    }

    FrameworkElement MenuPageLabel(LauncherMenuPage page)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(page.Icon == null
            ? new TextBlock { Text = page.Glyph, Width = 24, VerticalAlignment = VerticalAlignment.Center }
            : new Image { Source = page.Icon, Width = 20, Height = 20, Margin = new Thickness(0, 0, 4, 0), Stretch = Stretch.Uniform });
        row.Children.Add(new TextBlock { Text = page.Title, VerticalAlignment = VerticalAlignment.Center });
        return row;
    }

    void AttachExtensionButtons(
        LauncherMenuArea area,
        Panel host,
        bool top = false,
        string? instanceId = null,
        InlineDialog? lifetime = null,
        Func<string?>? instanceIdProvider = null)
    {
        void Refresh(LauncherMenuArea changed)
        {
            if (changed != area)
                return;
            foreach (var old in host.Children.OfType<FrameworkElement>().Where(element => element.Tag is LauncherMenuPage).ToArray())
                host.Children.Remove(old);
            foreach (var page in MenuPages.Pages(area))
            {
                if (top)
                {
                    var item = new RadioButton
                    {
                        Content = MenuPageLabel(page),
                        ContentTemplate = null,
                        Style = HomeNavigation.Style,
                        GroupName = "Navigation",
                        Tag = page
                    };
                    item.Checked += (_, _) => OpenMenuExtension(page, instanceId);
                    host.Children.Insert(Math.Max(0, host.Children.Count - 1), item);
                    continue;
                }

                var button = ActionButton("", (_, _) =>
                {
                    lifetime?.Close();
                    if (area == LauncherMenuArea.Download)
                    {
                        downloadNav.IsChecked = true;
                        RestoreResourcePage();
                        foreach (var category in downloadCategories.Values)
                            category.IsChecked = false;
                        downloadHost.Content = new ScrollViewer
                        {
                            Content = CreateMenuContent(page, null),
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                        };
                        MainTabControl.SelectedIndex = 1;
                    }
                    else
                        OpenMenuExtension(page, instanceIdProvider?.Invoke() ?? instanceId);
                });
                button.Content = MenuPageLabel(page);
                button.ContentTemplate = null;
                button.Tag = page;
                host.Children.Add(button);
            }
        }

        Refresh(area);
        MenuPages.Changed += Refresh;
        if (lifetime != null)
            lifetime.Closed += (_, _) => MenuPages.Changed -= Refresh;
    }

    void AttachExtensionTabs(LauncherMenuArea area, TabControl host)
    {
        void Refresh(LauncherMenuArea changed)
        {
            if (changed != area)
                return;
            foreach (var old in host.Items.OfType<TabItem>().Where(item => item.Tag is LauncherMenuPage).ToArray())
                host.Items.Remove(old);
            foreach (var page in MenuPages.Pages(area))
            {
                var tab = new TabItem { Header = page.Title, Tag = page, Style = (Style)FindResource("ResourceDetailTab") };
                tab.AddHandler(System.Windows.Controls.Primitives.Selector.SelectedEvent, new RoutedEventHandler((_, e) =>
                {
                    if (ReferenceEquals(e.Source, tab) && tab.Content == null)
                        tab.Content = CreateMenuContent(page, null);
                }));
                host.Items.Add(tab);
            }
        }

        Refresh(area);
        MenuPages.Changed += Refresh;
    }

    FrameworkElement CreateMenuContent(LauncherMenuPage page, string? instanceId)
    {
        try
        {
            return page.CreateContent(new(this, instanceId, resourceDetailProject))
                ?? Hint("栏目未提供页面内容。");
        }
        catch (Exception ex)
        {
            return Hint("无法加载“" + page.Title + "”：" + ex.Message);
        }
    }

    void OpenMenuExtension(LauncherMenuPage page, string? instanceId = null)
    {
        var previous = MainTabControl.SelectedItem;
        foreach (var nav in ((StackPanel)HomeNavigation.Parent).Children.OfType<RadioButton>().Where(button => button.Tag is string))
            nav.IsChecked = false;
        var body = new DockPanel();
        var back = ActionButton("← 返回", (_, _) =>
        {
            MainTabControl.SelectedItem = ReferenceEquals(previous, extensionPage) ? MainTabControl.Items[0] : previous;
        });
        back.HorizontalAlignment = HorizontalAlignment.Left;
        DockPanel.SetDock(back, Dock.Top);
        body.Children.Add(back);
        body.Children.Add(new ScrollViewer
        {
            Content = CreateMenuContent(page, instanceId),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });
        if (extensionPage == null)
        {
            extensionPage = new TabItem();
            MainTabControl.Items.Add(extensionPage);
        }
        extensionPage.Header = page.Title;
        extensionPage.Content = body;
        MainTabControl.SelectedItem = extensionPage;
    }

}
