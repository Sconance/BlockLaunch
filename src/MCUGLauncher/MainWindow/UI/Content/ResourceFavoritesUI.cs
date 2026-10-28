using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public partial class MainWindow
{
    bool resourceFavoritesOnly;
    readonly Button resourceFavoriteFilter = new() { Content = "♡", ToolTip = "仅显示收藏" };
    readonly Button resourceDetailFavorite = new() { Content = "♡ 收藏" };
    readonly TextBlock resourceFavoriteNotice = new()
    {
        Text = "已开启收藏筛选",
        Visibility = Visibility.Collapsed,
        Margin = new Thickness(8),
        VerticalAlignment = VerticalAlignment.Center
    };
    readonly ComboBox resourceSort = new()
    {
        ItemsSource = new[] { "相关性", "下载量", "最近更新", "收藏优先" },
        SelectedIndex = 0
    };
    bool resourceListView;
    readonly Button resourceGridView = new() { Content = "▦", ToolTip = "卡片视图" };
    readonly Button resourceLinearView = new() { Content = "☰", ToolTip = "列表视图" };
    ContentProject? resourceDetailProject;
    bool updatingResourceFilters;
    Button? resourceFavoriteClear;

    string ResourceSearchSort() => resourceSort.SelectedIndex switch
    {
        1 => "downloads",
        2 => "updated",
        _ => "relevance"
    };

    void ResetResourceDisplayFilters()
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

    void CardFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (e.Source is Button { Tag: "FavoriteResource", DataContext: ContentProject project })
        {
            e.Handled = true;
            ToggleResourceFavorite(project);
        }
    }

    void ToggleResourceFavorite(ContentProject project)
    {
        if (bookmarks == null)
        {
            InlineMessageBox.Show("收藏数据暂不可用，请查看运行日志。");
            return;
        }

        try
        {
            if (bookmarks.Read().Any(item => item.Project.Id == project.Id))
                bookmarks.Remove(project.Id);
            else
                bookmarks.Save(project, ContentKind, "");
            RefreshBookmarkDecoration();
            UpdateResourceFavoriteState(project.Id);
            if (!resourceFavoritesOnly)
                return;

            var items = ContentSearchResultsControl.Items.Cast<ContentProject>().Where(item => item.IsFavorite).ToList();
            ContentSearchResultsControl.SelectionChanged -= ProjectChanged;
            try
            {
                ContentSearchResultsControl.ItemsSource = items;
            }
            finally
            {
                ContentSearchResultsControl.SelectionChanged += ProjectChanged;
            }
            ContentSearchHintControl.Text = "没有符合条件的收藏，试试清除收藏筛选。";
            ContentSearchHintControl.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ContentSearchSummaryLabel.Text = "收藏已更新，搜索可刷新匹配结果。";
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show("无法更新收藏：" + ex.Message);
        }
    }

    void UpdateResourceFavoriteState(string? projectId = null)
    {
        resourceFavoriteFilter.Content = resourceFavoritesOnly ? "♥" : "♡";
        resourceFavoriteFilter.ToolTip = resourceFavoritesOnly ? "取消收藏筛选" : "仅显示收藏";
        resourceFavoriteNotice.Visibility = resourceFavoritesOnly ? Visibility.Visible : Visibility.Collapsed;
        if (resourceFavoriteClear != null)
            resourceFavoriteClear.Visibility = resourceFavoriteNotice.Visibility;
        projectId ??= (ContentSearchResultsControl.SelectedItem as ContentProject)?.Id;

        bool saved;
        try
        {
            saved = bookmarks?.Read().Any(item => item.Project.Id == projectId) == true;
        }
        catch (Exception ex)
        {
            Log("无法读取收藏状态：" + ex.Message);
            saved = false;
        }
        resourceDetailFavorite.Content = saved ? "♥ 已收藏" : "♡ 收藏";
        resourceDetailFavorite.Foreground = saved ? Brushes.Crimson : (Brush)FindResource("AccentBrush");
    }

    async Task ToggleResourceFavoriteFilter()
    {
        resourceFavoritesOnly = !resourceFavoritesOnly;
        searchOffset = 0;
        UpdateResourceFavoriteState();
        await RunBrowse(SearchContent);
    }

    async Task<SearchPage> SearchFavoriteResources(CancellationToken token)
    {
        var kind = ContentKind;
        var candidates = bookmarks?.Query(ContentSearchQueryInput.Text.Trim(), kind: kind)
            .Select(item => item.Project)
            .ToList() ?? [];
        return await resourceManagement.SearchFavorites(candidates, ResourceQuery(SearchTarget()), token);
    }

    void ToggleResourceView() => SetResourceView(!resourceListView);

    void SetResourceView(bool list)
    {
        resourceListView = list;
        ContentSearchResultsControl.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(
            resourceListView ? typeof(StackPanel) : typeof(ResponsiveResourcePanel)));
        ContentSearchResultsControl.ItemTemplate = (DataTemplate)FindResource(resourceListView ? "ResourceListItem" : "ResourceSearchItem");
        resourceGridView.SetResourceReference(Control.BackgroundProperty, resourceListView ? "ThemeSurfaceBrush" : "ThemeSelectedBrush");
        resourceLinearView.SetResourceReference(Control.BackgroundProperty, resourceListView ? "ThemeSelectedBrush" : "ThemeSurfaceBrush");
    }
}
