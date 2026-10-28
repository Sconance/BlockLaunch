using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;
internal sealed partial class MenuExtensionsController
{
    readonly LauncherWorkspace workspace;
    internal MenuExtensionsController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal LauncherMenuRegistry MenuPages { get; } = new();

    internal StackPanel? downloadNavigationPanel;
    internal TabControl? resourceDetailTabs;
    partial void ConfigureMenuExtensions(LauncherMenuRegistry menus);
    internal void BuildMenuExtensionHosts()
    {
        ConfigureMenuExtensions(MenuPages);
        AttachExtensionButtons(LauncherMenuArea.Top, (StackPanel)workspace.Controls.HomeNav.Parent, true);
        AttachExtensionButtons(LauncherMenuArea.Download, downloadNavigationPanel!);
        var settingsPanel = (StackPanel)((ScrollViewer)((TabItem)workspace.Window.MainTabs.Items[4]).Content).Content;
        AttachExtensionButtons(LauncherMenuArea.Settings, settingsPanel);
        var homeExtensions = new StackPanel();
        ((DockPanel)workspace.SketchController.sketchHome!.Children[0]).Children.Add(homeExtensions);
        AttachExtensionButtons(LauncherMenuArea.Home, homeExtensions);
        AttachExtensionTabs(LauncherMenuArea.ResourceDetails, resourceDetailTabs!);
        void RefreshMore(LauncherMenuArea area)
        {
            if (area != LauncherMenuArea.More)
                return;
            foreach (var item in workspace.CompactShellController.moreMenu.Items.OfType<MenuItem>().Where(i => i.Tag is LauncherMenuPage).ToArray())
                workspace.CompactShellController.moreMenu.Items.Remove(item);
            foreach (var page in MenuPages.Pages(area))
            {
                var item = new MenuItem
                {
                    Header = page.Title,
                    Tag = page
                };
                item.Click += (_, _) => OpenMenuExtension(page);
                workspace.CompactShellController.moreMenu.Items.Add(item);
            }
        }

        MenuPages.Changed += RefreshMore;
        RefreshMore(LauncherMenuArea.More);
    }

    internal FrameworkElement MenuPageLabel(LauncherMenuPage page)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        row.Children.Add(page.Icon == null ? new TextBlock { Text = page.Glyph, Width = 24, VerticalAlignment = VerticalAlignment.Center } : new Image { Source = page.Icon, Width = 20, Height = 20, Margin = new Thickness(0, 0, 4, 0), Stretch = Stretch.Uniform });
        row.Children.Add(new TextBlock { Text = page.Title, VerticalAlignment = VerticalAlignment.Center });
        return row;
    }

    internal void AttachExtensionButtons(LauncherMenuArea area, Panel host, bool top = false, string? instanceId = null, InlineDialog? lifetime = null, Func<string?>? instanceIdProvider = null)
    {
        void Refresh(LauncherMenuArea changed)
        {
            if (changed != area)
                return;
            foreach (var old in host.Children.OfType<FrameworkElement>().Where(e => e.Tag is LauncherMenuPage).ToArray())
                host.Children.Remove(old);
            foreach (var page in MenuPages.Pages(area))
            {
                if (top)
                {
                    var item = new RadioButton
                    {
                        Content = MenuPageLabel(page),
                        ContentTemplate = null,
                        Style = workspace.Controls.HomeNav.Style,
                        GroupName = "Navigation",
                        Tag = page
                    };
                    item.Checked += (_, _) => OpenMenuExtension(page, instanceId);
                    host.Children.Insert(Math.Max(0, host.Children.Count - 1), item);
                }
                else
                {
                    var item = FeatureUiElements.ActionButton("", (_, _) =>
                    {
                        lifetime?.Close();
                        if (area == LauncherMenuArea.Download)
                        {
                            workspace.CompactShellController.downloadNav.IsChecked = true;
                            workspace.DownloadPageController.RestoreResourcePage();
                            foreach (var category in workspace.DownloadPageController.downloadCategories.Values)
                                category.IsChecked = false;
                            workspace.DownloadPageController.downloadHost.Content = new ScrollViewer
                            {
                                Content = CreateMenuContent(page, null),
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                            };
                            workspace.Window.MainTabs.SelectedIndex = 1;
                        }
                        else
                            OpenMenuExtension(page, instanceIdProvider?.Invoke() ?? instanceId);
                    });
                    item.Content = MenuPageLabel(page);
                    item.ContentTemplate = null;
                    item.Tag = page;
                    host.Children.Add(item);
                }
            }
        }

        Refresh(area);
        MenuPages.Changed += Refresh;
        if (lifetime != null)
            lifetime.Closed += (_, _) => MenuPages.Changed -= Refresh;
    }

    internal void AttachExtensionTabs(LauncherMenuArea area, TabControl host)
    {
        void Refresh(LauncherMenuArea changed)
        {
            if (changed != area)
                return;
            foreach (var old in host.Items.OfType<TabItem>().Where(i => i.Tag is LauncherMenuPage).ToArray())
                host.Items.Remove(old);
            foreach (var page in MenuPages.Pages(area))
            {
                var tab = new TabItem
                {
                    Header = page.Title,
                    Tag = page,
                    Style = (Style)workspace.Window.FindResource("ResourceDetailTab")
                };
                tab.AddHandler(System.Windows.Controls.Primitives.Selector.SelectedEvent, new RoutedEventHandler((_, e) =>
                {
                    if (object.ReferenceEquals(e.Source, tab) && tab.Content == null)
                        tab.Content = CreateMenuContent(page, null);
                }));
                host.Items.Add(tab);
            }
        }

        Refresh(area);
        MenuPages.Changed += Refresh;
    }

    internal FrameworkElement CreateMenuContent(LauncherMenuPage page, string? instanceId)
    {
        try
        {
            return page.CreateContent(new(workspace.Window, instanceId, workspace.ResourceFavoritesController.resourceDetailProject)) ?? FeatureUiElements.Hint("栏目未提供页面内容。");
        }
        catch (Exception ex)
        {
            return FeatureUiElements.Hint("无法加载“" + page.Title + "”：" + ex.Message);
        }
    }

    internal TabItem? extensionPage;
    internal void OpenMenuExtension(LauncherMenuPage page, string? instanceId = null)
    {
        var previous = workspace.Window.MainTabs.SelectedItem;
        foreach (var nav in ((StackPanel)workspace.Controls.HomeNav.Parent).Children.OfType<RadioButton>().Where(r => r.Tag is string))
            nav.IsChecked = false;
        var body = new DockPanel();
        var back = FeatureUiElements.ActionButton("← 返回", (_, _) =>
        {
            workspace.Window.MainTabs.SelectedItem = previous == extensionPage ? workspace.Window.MainTabs.Items[0] : previous;
            var original = ((StackPanel)workspace.Controls.HomeNav.Parent).Children.OfType<RadioButton>().FirstOrDefault(r => r.Tag?.ToString() == workspace.Window.MainTabs.SelectedIndex.ToString());
            if (original != null)
                original.IsChecked = true;
        });
        back.HorizontalAlignment = HorizontalAlignment.Left;
        DockPanel.SetDock(back, Dock.Top);
        body.Children.Add(back);
        body.Children.Add(new ScrollViewer { Content = CreateMenuContent(page, instanceId), VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        if (extensionPage == null)
        {
            extensionPage = new TabItem();
            workspace.Window.MainTabs.Items.Add(extensionPage);
        }

        extensionPage.Header = page.Title;
        extensionPage.Content = body;
        workspace.Window.MainTabs.SelectedItem = extensionPage;
    }
}
