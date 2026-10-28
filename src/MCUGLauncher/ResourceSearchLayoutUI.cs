using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly TextBlock resourceWorkspaceSubtitle = new() { FontSize = 12, Margin = new Thickness(0, 3, 0, 0) };

    void BuildResourceSearchLayout(StackPanel filters)
    {
        var search = (Grid)SearchQuery.Parent;
        var row = filters.Children.OfType<WrapPanel>().First();
        var actions = row.Children.OfType<Button>().ToArray();
        row.Children.Clear();
        filters.Children.Clear();
        resourceFavoriteFilter.Click += async (_, _) => await ToggleResourceFavoriteFilter();
        var clear = ActionButton("清除筛选", async (_, _) =>
        {
            if (resourceFavoritesOnly)
                await ToggleResourceFavoriteFilter();
        });
        resourceFavoriteClear = clear;
        clear.Visibility = Visibility.Collapsed;
        var view = new ResourceSearchHeader(new(
            resourceWorkspaceTitle, resourceWorkspaceSubtitle, resourceFavoriteNotice,
            resourceFavoriteFilter, clear, search, SearchQuery, resourceSource,
            [new("游戏版本", resourceGame), new("加载器", resourceLoader), new("发布类型", releaseChannel), new("排序", resourceSort)],
            actions, resourceGridView, resourceLinearView), (ControlTemplate)FindResource("SearchInputTemplate"));
        var parent = (Grid)filters.Parent;
        parent.Children.Remove(filters);
        Grid.SetRow(view, 1);
        parent.Children.Add(view);
        resourceFilterPanel = view;
        resourceSort.SelectionChanged += async (_, _) =>
        {
            if (updatingResourceFilters)
                return;
            searchOffset = 0;
            await RunBrowse(SearchContent);
        };
        resourceGridView.Click += (_, _) => SetResourceView(false);
        resourceLinearView.Click += (_, _) => SetResourceView(true);
        SearchResults.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(ResponsiveResourcePanel)));
        ScrollViewer.SetCanContentScroll(SearchResults, false);
        SearchResults.SetValue(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.Contained);
    }
}
