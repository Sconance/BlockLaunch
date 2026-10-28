using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class ResourceFavoritesController
{
    readonly LauncherWorkspace workspace;
    internal ResourceFavoritesController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal bool resourceFavoritesOnly;
    internal readonly Button resourceFavoriteFilter = new()
    {
        Content = "♡",
        ToolTip = "仅显示收藏"
    };
    internal readonly Button resourceDetailFavorite = new()
    {
        Content = "♡ 收藏"
    };
    internal readonly TextBlock resourceFavoriteNotice = new()
    {
        Text = "已开启收藏筛选",
        Visibility = Visibility.Collapsed,
        Margin = new Thickness(8),
        VerticalAlignment = VerticalAlignment.Center
    };
    internal readonly ComboBox resourceSort = new()
    {
        ItemsSource = new[]
        {
            "相关性",
            "下载量",
            "最近更新",
            "收藏优先"
        },
        SelectedIndex = 0
    };
    internal bool resourceListView;
    internal readonly Button resourceGridView = new()
    {
        Content = "▦",
        ToolTip = "卡片视图"
    };
    internal readonly Button resourceLinearView = new()
    {
        Content = "☰",
        ToolTip = "列表视图"
    };
    internal ContentProject? resourceDetailProject;
    internal bool updatingResourceFilters;
    internal Button? resourceFavoriteClear;
    internal void ResetResourceDisplayFilters()
    {
        updatingResourceFilters = true;
        try
        {
            resourceFavoritesOnly = false;
            resourceSort.SelectedIndex = 0;
            UpdateResourceFavoriteState();
        }
        finally
        {
            updatingResourceFilters = false;
        }
    }

    internal void CardFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (e.Source is Button { Tag: "FavoriteResource", DataContext: ContentProject project })
        {
            e.Handled = true;
            ToggleResourceFavorite(project);
        }
    }

    internal void ToggleResourceFavorite(ContentProject project)
    {
        if (workspace.CollectionsController.bookmarks == null)
        {
            InlineMessageBox.Show("收藏数据暂不可用，请查看运行日志。");
            return;
        }

        try
        {
            if (workspace.CollectionsController.bookmarks.Read().Any(i => i.Project.Id == project.Id))
                workspace.CollectionsController.bookmarks.Remove(project.Id);
            else
                workspace.CollectionsController.bookmarks.Save(project, workspace.PackController.ContentKind, "");
            workspace.CollectionsController.RefreshBookmarkDecoration();
            UpdateResourceFavoriteState(project.Id);
            if (resourceFavoritesOnly)
            {
                var items = workspace.Controls.SearchResults.Items.Cast<ContentProject>().Where(p => p.IsFavorite).ToList();
                workspace.Controls.SearchResults.SelectionChanged -= workspace.ContentController.ProjectChanged;
                try
                {
                    workspace.Controls.SearchResults.ItemsSource = items;
                }
                finally
                {
                    workspace.Controls.SearchResults.SelectionChanged += workspace.ContentController.ProjectChanged;
                }

                workspace.Controls.SearchHint.Text = "没有符合条件的收藏，试试清除收藏筛选。";
                workspace.Controls.SearchHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                workspace.Controls.SearchSummary.Text = "收藏已更新，搜索可刷新匹配结果。";
            }
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show("无法更新收藏：" + ex.Message);
        }
    }

    internal void UpdateResourceFavoriteState(string? projectId = null)
    {
        resourceFavoriteFilter.Content = resourceFavoritesOnly ? "♥" : "♡";
        resourceFavoriteFilter.ToolTip = resourceFavoritesOnly ? "取消收藏筛选" : "仅显示收藏";
        resourceFavoriteNotice.Visibility = resourceFavoritesOnly ? Visibility.Visible : Visibility.Collapsed;
        if (resourceFavoriteClear != null)
            resourceFavoriteClear.Visibility = resourceFavoriteNotice.Visibility;
        if (projectId == null)
            projectId = (workspace.Controls.SearchResults.SelectedItem as ContentProject)?.Id;
        bool saved;
        try
        {
            saved = workspace.CollectionsController.bookmarks?.Read().Any(i => i.Project.Id == projectId) == true;
        }
        catch (Exception ex)
        {
            workspace.LaunchCoordinator.Log("无法读取收藏状态：" + ex.Message);
            saved = false;
        }

        resourceDetailFavorite.Content = saved ? "♥ 已收藏" : "♡ 收藏";
        resourceDetailFavorite.Foreground = saved ? System.Windows.Media.Brushes.Crimson : (System.Windows.Media.Brush)workspace.Window.FindResource("AccentBrush");
    }

    internal async Task ToggleResourceFavoriteFilter()
    {
        resourceFavoritesOnly = !resourceFavoritesOnly;
        workspace.ContentController.searchOffset = 0;
        UpdateResourceFavoriteState();
        await workspace.ContentController.RunBrowse(workspace.ContentController.SearchContent);
    }

    internal async Task<SearchPage> SearchFavoriteResources(CancellationToken token)
    {
        var kind = workspace.PackController.ContentKind;
        var candidates = workspace.CollectionsController.bookmarks?.Query(workspace.Controls.SearchQuery.Text.Trim(), kind: kind).Select(i => i.Project).ToList() ?? [];
        var target = workspace.ResourceFiltersController.SearchTarget();
        if (target.GameVersion.Length > 0 || target.Loader.Length > 0 || workspace.ResourceFiltersController.releaseChannel.SelectedIndex > 0)
        {
            var channel = workspace.ResourceFiltersController.releaseChannel.SelectedIndex;
            using var gate = new SemaphoreSlim(4);
            var matches = await Task.WhenAll(candidates.Select(async project =>
            {
                await gate.WaitAsync(token);
                try
                {
                    var versions = await new ContentService(workspace.LaunchCoordinator.http, token).Versions(project.Id, target.GameVersion, target.Loader, kind);
                    return versions.Any(v => channel == 0 || v.VersionType == new[] { "", "release", "beta", "alpha" }[channel]) ? project : null;
                }
                finally
                {
                    gate.Release();
                }
            }));
            candidates = matches.OfType<ContentProject>().ToList();
        }

        if (resourceSort.SelectedIndex == 1)
            candidates = candidates.OrderByDescending(p => p.Downloads).ToList();
        return new(candidates.Skip(workspace.ContentController.searchOffset).Take(15).ToList(), candidates.Count);
    }

    internal void ToggleResourceView()
    {
        SetResourceView(!resourceListView);
    }

    internal void SetResourceView(bool list)
    {
        resourceListView = list;
        workspace.Controls.SearchResults.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(resourceListView ? typeof(StackPanel) : typeof(ResponsiveResourcePanel)));
        workspace.Controls.SearchResults.ItemTemplate = (DataTemplate)workspace.Window.FindResource(resourceListView ? "ResourceListItem" : "ResourceSearchItem");
        resourceGridView.SetResourceReference(Control.BackgroundProperty, resourceListView ? "ThemeSurfaceBrush" : "ThemeSelectedBrush");
        resourceLinearView.SetResourceReference(Control.BackgroundProperty, resourceListView ? "ThemeSelectedBrush" : "ThemeSurfaceBrush");
    }
}
