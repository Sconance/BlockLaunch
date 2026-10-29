using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class ResourceFavoritesController
{
    readonly Window owner;
    readonly ResourceFavoriteControls controls;
    readonly ResourceFavoriteActions actions;
    internal ResourceFavoritesController(Window owner, ResourceFavoriteControls controls, ResourceFavoriteActions actions)
    {
        this.owner = owner;
        this.controls = controls;
        this.actions = actions;
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
        var bookmarks = actions.Bookmarks();
        if (bookmarks == null)
        {
            InlineMessageBox.Show("收藏数据暂不可用，请查看运行日志。");
            return;
        }

        try
        {
            if (bookmarks.Read().Any(i => i.Project.Id == project.Id))
                bookmarks.Remove(project.Id);
            else
                bookmarks.Save(project, actions.ContentKind(), "");
            actions.RefreshDecoration();
            UpdateResourceFavoriteState(project.Id);
            if (resourceFavoritesOnly)
            {
                var items = controls.Results.Items.Cast<ContentProject>().Where(p => p.IsFavorite).ToList();
                controls.Results.SelectionChanged -= actions.ProjectChanged;
                try
                {
                    controls.Results.ItemsSource = items;
                }
                finally
                {
                    controls.Results.SelectionChanged += actions.ProjectChanged;
                }

                controls.Hint.Text = "没有符合条件的收藏，试试清除收藏筛选。";
                controls.Hint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                controls.Summary.Text = "收藏已更新，搜索可刷新匹配结果。";
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
            projectId = (controls.Results.SelectedItem as ContentProject)?.Id;
        bool saved;
        try
        {
            saved = actions.Bookmarks()?.Read().Any(i => i.Project.Id == projectId) == true;
        }
        catch (Exception ex)
        {
            actions.Log("无法读取收藏状态：" + ex.Message);
            saved = false;
        }

        resourceDetailFavorite.Content = saved ? "♥ 已收藏" : "♡ 收藏";
        resourceDetailFavorite.Foreground = saved ? System.Windows.Media.Brushes.Crimson : (System.Windows.Media.Brush)owner.FindResource("AccentBrush");
    }

    internal async Task ToggleResourceFavoriteFilter()
    {
        resourceFavoritesOnly = !resourceFavoritesOnly;
        actions.ResetSearchOffset();
        UpdateResourceFavoriteState();
        await actions.Search();
    }

    internal Task<SearchPage> SearchFavoriteResources(CancellationToken token)
    {
        var request = new ResourceFavoriteQuery(controls.Query.Text.Trim(), actions.ContentKind(),
            actions.SearchTarget(), actions.ReleaseChannel(), resourceSort.SelectedIndex, actions.SearchOffset());
        return ResourceFavoriteSearch.Search(actions.Bookmarks(), actions.Http(), request, token);
    }

    internal void ToggleResourceView()
    {
        SetResourceView(!resourceListView);
    }

    internal void SetResourceView(bool list)
    {
        resourceListView = list;
        controls.Results.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(resourceListView ? typeof(StackPanel) : typeof(ResponsiveResourcePanel)));
        controls.Results.ItemTemplate = (DataTemplate)owner.FindResource(resourceListView ? "ResourceListItem" : "ResourceSearchItem");
        resourceGridView.SetResourceReference(Control.BackgroundProperty, resourceListView ? "ThemeSurfaceBrush" : "ThemeSelectedBrush");
        resourceLinearView.SetResourceReference(Control.BackgroundProperty, resourceListView ? "ThemeSelectedBrush" : "ThemeSurfaceBrush");
    }
}

internal sealed record ResourceFavoriteControls(TextBox Query, ListBox Results, TextBlock Hint, TextBlock Summary);

/// <summary>Only resource-favorite operations cross this component boundary.</summary>
internal sealed record ResourceFavoriteActions(
    Func<ResourceBookmarkStore?> Bookmarks,
    Func<string> ContentKind,
    Action RefreshDecoration,
    SelectionChangedEventHandler ProjectChanged,
    Action<string> Log,
    Action ResetSearchOffset,
    Func<Task> Search,
    Func<InstanceTarget> SearchTarget,
    Func<int> ReleaseChannel,
    Func<int> SearchOffset,
    Func<System.Net.Http.HttpClient> Http);
