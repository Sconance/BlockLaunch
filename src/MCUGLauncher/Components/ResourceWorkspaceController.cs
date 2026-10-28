using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockLaunch;
internal sealed class ResourceWorkspaceController
{
    readonly LauncherWorkspace workspace;
    internal ResourceWorkspaceController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal Grid? resourceResultsLayout;
    internal Border? resourceDetailSurface;
    internal Grid? resourceDetailColumns;
    internal FrameworkElement? resourceFilterPanel;
    internal FrameworkElement? resourcePagination;
    internal readonly Image resourceProjectIcon = new()
    {
        Width = 58,
        Height = 58,
        Margin = new Thickness(0, 0, 14, 0)
    };
    internal readonly TextBlock resourceProjectMeta = new()
    {
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap
    };
    internal readonly TextBlock resourceVersionFacts = new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12
    };
    internal readonly TextBlock resourceDependencies = new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12
    };
    internal readonly TextBlock resourceWorkspaceTitle = new()
    {
        FontSize = 22,
        FontWeight = FontWeights.SemiBold
    };
    internal readonly WrapPanel resourceGallery = new()
    {
        Margin = new Thickness(0, 8, 0, 12)
    };
    internal readonly TextBlock resourceBody = new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontSize = 13,
        LineHeight = 21
    };
    internal readonly TextBlock resourceDependencyList = new()
    {
        TextWrapping = TextWrapping.Wrap,
        FontSize = 13,
        Margin = new Thickness(0, 8, 0, 0)
    };
    internal readonly WrapPanel resourceTagChips = new()
    {
        Margin = new Thickness(0, 7, 0, 5)
    };
    internal void BuildResourceWorkspace()
    {
        var oldPanel = (StackPanel)workspace.Controls.ProjectTitle.Parent;
        var details = (Border)((ScrollViewer)oldPanel.Parent).Parent;
        var results = (Grid)details.Parent;
        var page = (Grid)results.Parent;
        var actions = oldPanel.Children.OfType<Button>().Where(b => b != workspace.Controls.InstallContentButton).ToArray();
        foreach (var control in new UIElement[]
        {
            workspace.Controls.ProjectTitle,
            workspace.Controls.ProjectDescription,
            workspace.Controls.ProjectVersions,
            workspace.Controls.InstallContentButton
        }.Concat(actions))
            oldPanel.Children.Remove(control);
        results.Children.Remove(details);
        results.ColumnDefinitions.Clear();
        results.ColumnDefinitions.Add(new());
        resourceResultsLayout = results;
        // This is an in-page detail view; the existing controls retain their handlers and download behavior.
        var body = new Grid();
        body.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        body.RowDefinitions.Add(new());
        var back = FeatureUiElements.ActionButton("← 返回搜索结果", (_, _) =>
        {
            workspace.ContentController.browseCancellation?.Cancel();
            ShowResourceResults();
        });
        back.HorizontalAlignment = HorizontalAlignment.Left;
        body.Children.Add(back);
        workspace.ResourceFavoritesController.resourceDetailFavorite.HorizontalAlignment = HorizontalAlignment.Right;
        workspace.ResourceFavoritesController.resourceDetailFavorite.VerticalAlignment = VerticalAlignment.Top;
        workspace.ResourceFavoritesController.resourceDetailFavorite.Click += (_, _) =>
        {
            if (workspace.ResourceFavoritesController.resourceDetailProject != null)
                workspace.ResourceFavoritesController.ToggleResourceFavorite(workspace.ResourceFavoritesController.resourceDetailProject);
        };
        body.Children.Add(workspace.ResourceFavoritesController.resourceDetailFavorite);
        resourceDetailColumns = new Grid
        {
            Margin = new Thickness(0, 10, 0, 0)
        };
        resourceDetailColumns.ColumnDefinitions.Add(new());
        resourceDetailColumns.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(270)
        });
        Grid.SetRow(resourceDetailColumns, 1);
        body.Children.Add(resourceDetailColumns);
        var introduction = new StackPanel
        {
            Margin = new Thickness(0, 0, 16, 0)
        };
        var identity = new DockPanel();
        DockPanel.SetDock(resourceProjectIcon, Dock.Left);
        identity.Children.Add(resourceProjectIcon);
        workspace.Controls.ProjectTitle.FontSize = 24;
        workspace.Controls.ProjectTitle.Margin = new Thickness(0, 0, 0, 6);
        var name = new StackPanel();
        name.Children.Add(workspace.Controls.ProjectTitle);
        resourceProjectMeta.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
        name.Children.Add(resourceProjectMeta);
        name.Children.Add(resourceTagChips);
        identity.Children.Add(name);
        introduction.Children.Add(identity);
        workspace.Controls.ProjectDescription.MaxHeight = double.PositiveInfinity;
        workspace.Controls.ProjectDescription.FontSize = 13;
        workspace.Controls.ProjectDescription.Margin = new Thickness(0, 0, 0, 16);
        workspace.Controls.ProjectDescription.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        var overview = new StackPanel();
        overview.Children.Add(resourceGallery);
        overview.Children.Add(workspace.Controls.ProjectDescription);
        overview.Children.Add(resourceBody);
        var versions = new StackPanel();
        versions.Children.Add(resourceVersionFacts);
        var versionList = new ListBox
        {
            MaxHeight = 320,
            Margin = new Thickness(0, 12, 0, 0),
            DisplayMemberPath = "Label"
        };
        versionList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ItemsSource") { Source = workspace.Controls.ProjectVersions });
        versionList.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, new Binding("SelectedItem") { Source = workspace.Controls.ProjectVersions, Mode = BindingMode.TwoWay });
        versions.Children.Add(versionList);
        var tabs = new TabControl
        {
            Margin = new Thickness(0, 14, 0, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        workspace.MenuExtensionsController.resourceDetailTabs = tabs;
        tabs.Items.Add(new TabItem { Header = "介绍", Content = overview, Style = (Style)workspace.Window.FindResource("ResourceDetailTab") });
        tabs.Items.Add(new TabItem { Header = "版本", Content = versions, Style = (Style)workspace.Window.FindResource("ResourceDetailTab") });
        tabs.Items.Add(new TabItem { Header = "依赖", Content = resourceDependencyList, Style = (Style)workspace.Window.FindResource("ResourceDetailTab") });
        introduction.Children.Add(tabs);
        var links = new WrapPanel
        {
            Margin = new Thickness(0, 16, 0, 0)
        };
        foreach (var action in actions)
            if (action.Content?.ToString()?.Contains("一键收藏") != true)
                links.Children.Add(action);
        introduction.Children.Add(links);
        resourceDetailColumns.Children.Add(new ScrollViewer { Content = introduction, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var options = new StackPanel();
        options.Children.Add(FeatureUiElements.Heading("下载选项"));
        options.Children.Add(FeatureUiElements.Hint("资源版本"));
        workspace.Controls.ProjectVersions.Width = double.NaN;
        options.Children.Add(workspace.Controls.ProjectVersions);
        workspace.Controls.InstallContentButton.Content = "↓ 下载所选文件";
        workspace.Controls.InstallContentButton.Margin = new Thickness(0, 16, 0, 8);
        options.Children.Add(workspace.Controls.InstallContentButton);
        options.Children.Add(FeatureUiElements.Hint("依赖信息"));
        options.Children.Add(resourceDependencies);
        options.Children.Add(FeatureUiElements.Hint("资源文件保存到上方独立下载目录；整合包按现有流程导入独立实例。"));
        var optionCard = workspace.InstancePagesController.FeatureCard(options);
        optionCard.Padding = new Thickness(14);
        optionCard.VerticalAlignment = VerticalAlignment.Top;
        var optionScroll = new ScrollViewer
        {
            Content = optionCard,
            VerticalContentAlignment = VerticalAlignment.Top,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(optionScroll, 1);
        resourceDetailColumns.Children.Add(optionScroll);
        resourceDetailSurface = new Border
        {
            Child = body,
            Visibility = Visibility.Collapsed
        };
        Grid.SetRow(resourceDetailSurface, 2);
        page.Children.Add(resourceDetailSurface);
        workspace.Controls.ProjectVersions.SelectionChanged += (_, _) => UpdateResourceVersionFacts();
        page.SizeChanged += (_, _) => ArrangeResourceDetail(page.ActualWidth);
        var filters = (StackPanel)((Grid)workspace.Controls.SearchQuery.Parent).Parent;
        resourceFilterPanel = filters;
        resourcePagination = page.Children.OfType<Grid>().First(g => Grid.GetRow(g) == 3);
        resourceWorkspaceTitle.Margin = new Thickness(0, 0, 0, 6);
        workspace.ResourceSearchLayoutController.BuildResourceSearchLayout(filters);
        workspace.Controls.SearchQuery.ToolTip = "搜索名称、作者或关键词；按 Enter 搜索";
        workspace.Controls.SearchResults.ItemTemplate = (DataTemplate)workspace.Window.FindResource("ResourceSearchItem");
        var style = new Style(typeof(ListBoxItem), (Style)workspace.Window.FindResource(typeof(ListBoxItem)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12)));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 8)));
        workspace.Controls.SearchResults.ItemContainerStyle = style;
        workspace.Controls.SearchResults.AddHandler(Button.ClickEvent, new RoutedEventHandler(workspace.ResourceFavoritesController.CardFavoriteClick));
    }

    internal void ShowResourceResults()
    {
        if (resourceResultsLayout == null || resourceDetailSurface == null)
            return;
        resourceResultsLayout.Visibility = Visibility.Visible;
        resourceDetailSurface.Visibility = Visibility.Collapsed;
        resourceFilterPanel!.Visibility = Visibility.Visible;
        resourcePagination!.Visibility = Visibility.Visible;
        // Clear only selection, keeping items and scroll position so the same project can be opened again.
        workspace.Controls.SearchResults.SelectedIndex = -1;
        resourceWorkspaceTitle.Text = workspace.DownloadPageController.downloadCategory == "Mod" ? "模组" : workspace.DownloadPageController.downloadCategory;
        workspace.ResourceSearchLayoutController.resourceWorkspaceSubtitle.Text = "发现、下载和收藏 Minecraft " + resourceWorkspaceTitle.Text + "。";
        workspace.Controls.SearchQuery.Tag = "搜索" + resourceWorkspaceTitle.Text + "名称、作者或描述…";
    }

    internal void ShowResourceDetail(ContentProject project)
    {
        workspace.ResourceFavoritesController.resourceDetailProject = project;
        workspace.ResourceFavoritesController.UpdateResourceFavoriteState(project.Id);
        workspace.MenuExtensionsController.resourceDetailTabs!.SelectedIndex = 0;
        foreach (var tab in workspace.MenuExtensionsController.resourceDetailTabs.Items.OfType<TabItem>().Where(t => t.Tag is LauncherMenuPage))
            tab.Content = null;
        resourceProjectIcon.SetBinding(Image.SourceProperty, new Binding(nameof(ContentProject.IconUrl)) { Source = project });
        resourceProjectIcon.ToolTip = project.Title;
        resourceProjectMeta.Text = $"作者：{project.Author} · ↓ {project.Downloads:N0} 次下载";
        resourceTagChips.Children.Clear();
        foreach (var tag in project.Tags.Split('·', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var chip = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 5, 3),
                Child = new TextBlock
                {
                    Text = tag,
                    FontSize = 11
                }
            };
            chip.SetResourceReference(Border.BackgroundProperty, "ThemeInputBrush");
            resourceTagChips.Children.Add(chip);
        }

        resourceGallery.Children.Clear();
        resourceBody.Text = "";
        resourceVersionFacts.Text = "正在读取兼容版本…";
        resourceDependencies.Text = "正在读取依赖信息…";
        resourceDependencyList.Text = "正在读取依赖信息…";
        resourceResultsLayout!.Visibility = Visibility.Collapsed;
        resourceDetailSurface!.Visibility = Visibility.Visible;
        resourceFilterPanel!.Visibility = Visibility.Collapsed;
        resourcePagination!.Visibility = Visibility.Collapsed;
    }

    internal void UpdateResourceVersionFacts()
    {
        if (workspace.Controls.ProjectVersions.SelectedItem is not ContentVersion version)
        {
            resourceVersionFacts.Text = "暂无可用版本。可返回搜索结果调整游戏版本和加载器筛选。";
            resourceDependencies.Text = "尚未选择资源版本。";
            resourceDependencyList.Text = resourceDependencies.Text;
            return;
        }

        resourceVersionFacts.Text = $"游戏版本：{string.Join("、", version.GameVersions)}\n加载器：{string.Join("、", version.Loaders)}\n发布时间：{version.DatePublished.ToLocalTime():yyyy/MM/dd HH:mm}\n文件大小：{version.Files.Sum(f => f.Size) / 1048576d:0.##} MB";
        var required = version.Dependencies.Count(d => d.DependencyType == "required");
        var optional = version.Dependencies.Count(d => d.DependencyType == "optional");
        resourceDependencies.Text = $"必需依赖：{required} 项\n可选依赖：{optional} 项\n\n独立文件下载不自动安装依赖。使用实例内安装功能时解析必需依赖。";
        resourceDependencyList.Text = version.Dependencies.Count == 0 ? "所选版本没有声明依赖。" : string.Join("\n\n", version.Dependencies.Select(d => (d.DependencyType == "required" ? "必需" : d.DependencyType == "optional" ? "可选" : d.DependencyType) + " · " + (d.ProjectId ?? d.VersionId ?? "未提供项目标识")));
    }

    internal void ArrangeResourceDetail(double width)
    {
        if (resourceDetailColumns == null)
            return;
        var stacked = width < 760;
        resourceDetailColumns.RowDefinitions.Clear();
        resourceDetailColumns.RowDefinitions.Add(new());
        resourceDetailColumns.RowDefinitions.Add(new()
        {
            Height = stacked ? new GridLength(1, GridUnitType.Star) : new GridLength(0)
        });
        resourceDetailColumns.ColumnDefinitions[1].Width = stacked ? new GridLength(0) : new GridLength(270);
        var options = resourceDetailColumns.Children[1];
        Grid.SetColumn(options, stacked ? 0 : 1);
        Grid.SetRow(options, stacked ? 1 : 0);
    }
}
