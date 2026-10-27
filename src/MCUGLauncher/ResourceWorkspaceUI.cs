using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockLaunch;

public partial class MainWindow
{
    Grid? resourceResultsLayout;
    Border? resourceDetailSurface;
    Grid? resourceDetailColumns;
    FrameworkElement? resourceFilterPanel;
    FrameworkElement? resourcePagination;
    readonly Image resourceProjectIcon = new() { Width = 58, Height = 58, Margin = new Thickness(0, 0, 14, 0) };
    readonly TextBlock resourceProjectMeta = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock resourceVersionFacts = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    readonly TextBlock resourceDependencies = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    readonly TextBlock resourceWorkspaceTitle = new() { FontSize = 22, FontWeight = FontWeights.SemiBold };

    void BuildResourceWorkspace()
    {
        var oldPanel = (StackPanel)ProjectTitle.Parent;
        var details = (Border)((ScrollViewer)oldPanel.Parent).Parent;
        var results = (Grid)details.Parent;
        var page = (Grid)results.Parent;
        var actions = oldPanel.Children.OfType<Button>().Where(b => b != InstallContentButton).ToArray();
        foreach (var control in new UIElement[] { ProjectTitle, ProjectDescription, ProjectVersions, InstallContentButton }.Concat(actions))
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
        var back = ActionButton("← 返回搜索结果", (_, _) =>
        {
            browseCancellation?.Cancel();
            ShowResourceResults();
        });
        back.HorizontalAlignment = HorizontalAlignment.Left;
        body.Children.Add(back);
        resourceDetailColumns = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        resourceDetailColumns.ColumnDefinitions.Add(new());
        resourceDetailColumns.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(270)
        });
        Grid.SetRow(resourceDetailColumns, 1);
        body.Children.Add(resourceDetailColumns);

        var introduction = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
        var identity = new DockPanel();
        DockPanel.SetDock(resourceProjectIcon, Dock.Left);
        identity.Children.Add(resourceProjectIcon);
        ProjectTitle.FontSize = 24;
        ProjectTitle.Margin = new Thickness(0, 0, 0, 6);
        var name = new StackPanel();
        name.Children.Add(ProjectTitle);
        resourceProjectMeta.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
        name.Children.Add(resourceProjectMeta);
        identity.Children.Add(name);
        introduction.Children.Add(identity);
        introduction.Children.Add(Heading("项目介绍"));
        ProjectDescription.MaxHeight = double.PositiveInfinity;
        ProjectDescription.FontSize = 13;
        ProjectDescription.Margin = new Thickness(0, 0, 0, 16);
        ProjectDescription.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        introduction.Children.Add(ProjectDescription);
        introduction.Children.Add(Heading("版本与兼容性"));
        introduction.Children.Add(resourceVersionFacts);
        var links = new WrapPanel { Margin = new Thickness(0, 16, 0, 0) };
        foreach (var action in actions)
            links.Children.Add(action);
        introduction.Children.Add(links);
        resourceDetailColumns.Children.Add(new ScrollViewer { Content = introduction, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

        var options = new StackPanel();
        options.Children.Add(Heading("下载选项"));
        options.Children.Add(Hint("资源版本"));
        ProjectVersions.Width = double.NaN;
        options.Children.Add(ProjectVersions);
        InstallContentButton.Content = "↓ 下载所选文件";
        InstallContentButton.Margin = new Thickness(0, 16, 0, 8);
        options.Children.Add(InstallContentButton);
        options.Children.Add(Hint("依赖信息"));
        options.Children.Add(resourceDependencies);
        options.Children.Add(Hint("资源文件保存到上方独立下载目录；整合包按现有流程导入独立实例。"));
        var optionCard = FeatureCard(options);
        optionCard.Padding = new Thickness(14);
        var optionScroll = new ScrollViewer { Content = optionCard, VerticalContentAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(optionScroll, 1);
        resourceDetailColumns.Children.Add(optionScroll);
        resourceDetailSurface = new Border { Child = body, Visibility = Visibility.Collapsed };
        Grid.SetRow(resourceDetailSurface, 2);
        page.Children.Add(resourceDetailSurface);
        ProjectVersions.SelectionChanged += (_, _) => UpdateResourceVersionFacts();
        page.SizeChanged += (_, _) => ArrangeResourceDetail(page.ActualWidth);
        var filters = (StackPanel)((Grid)SearchQuery.Parent).Parent;
        resourceFilterPanel = filters;
        resourcePagination = page.Children.OfType<Grid>().First(g => Grid.GetRow(g) == 3);
        resourceWorkspaceTitle.Margin = new Thickness(0, 0, 0, 6);
        filters.Children.Insert(0, resourceWorkspaceTitle);
        SearchQuery.ToolTip = "搜索名称、作者或关键词；按 Enter 搜索";
        SearchResults.ItemTemplate = (DataTemplate)FindResource("ResourceSearchItem");
        var style = new Style(typeof(ListBoxItem), (Style)FindResource(typeof(ListBoxItem)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 9, 12, 9)));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 6)));
        SearchResults.ItemContainerStyle = style;
    }

    void ShowResourceResults()
    {
        if (resourceResultsLayout == null || resourceDetailSurface == null)
            return;
        resourceResultsLayout.Visibility = Visibility.Visible;
        resourceDetailSurface.Visibility = Visibility.Collapsed;
        resourceFilterPanel!.Visibility = Visibility.Visible;
        resourcePagination!.Visibility = Visibility.Visible;
        // Clear only selection, keeping items and scroll position so the same project can be opened again.
        SearchResults.SelectedIndex = -1;
        resourceWorkspaceTitle.Text = downloadCategory == "Mod" ? "模组" : downloadCategory;
    }

    void ShowResourceDetail(ContentProject project)
    {
        resourceProjectIcon.SetBinding(Image.SourceProperty, new Binding(nameof(ContentProject.IconUrl)) { Source = project });
        resourceProjectIcon.ToolTip = project.Title;
        resourceProjectMeta.Text = $"作者：{project.Author} · {project.Downloads:N0} 次下载\n{project.Tags}";
        resourceVersionFacts.Text = "正在读取兼容版本…";
        resourceDependencies.Text = "正在读取依赖信息…";
        resourceResultsLayout!.Visibility = Visibility.Collapsed;
        resourceDetailSurface!.Visibility = Visibility.Visible;
        resourceFilterPanel!.Visibility = Visibility.Collapsed;
        resourcePagination!.Visibility = Visibility.Collapsed;
    }

    void UpdateResourceVersionFacts()
    {
        if (ProjectVersions.SelectedItem is not ContentVersion version)
        {
            resourceVersionFacts.Text = "暂无可用版本。可返回搜索结果调整游戏版本和加载器筛选。";
            resourceDependencies.Text = "尚未选择资源版本。";
            return;
        }
        resourceVersionFacts.Text = $"游戏版本：{string.Join("、", version.GameVersions)}\n加载器：{string.Join("、", version.Loaders)}\n发布时间：{version.DatePublished.ToLocalTime():yyyy/MM/dd HH:mm}\n文件大小：{version.Files.Sum(f => f.Size) / 1048576d:0.##} MB";
        var required = version.Dependencies.Count(d => d.DependencyType == "required");
        var optional = version.Dependencies.Count(d => d.DependencyType == "optional");
        resourceDependencies.Text = $"必需依赖：{required} 项\n可选依赖：{optional} 项\n\n独立文件下载不自动安装依赖。使用实例内安装功能时解析必需依赖。";
    }

    void ArrangeResourceDetail(double width)
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
