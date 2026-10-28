using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;
internal sealed class ResourceSearchLayoutController
{
    readonly LauncherWorkspace workspace;
    internal ResourceSearchLayoutController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal readonly TextBlock resourceWorkspaceSubtitle = new()
    {
        FontSize = 12,
        Margin = new Thickness(0, 3, 0, 0)
    };
    internal void BuildResourceSearchLayout(StackPanel filters)
    {
        var search = (Grid)workspace.Controls.SearchQuery.Parent;
        var row = filters.Children.OfType<WrapPanel>().First();
        var actions = row.Children.OfType<Button>().ToArray();
        row.Children.Clear();
        filters.Children.Clear();
        workspace.ResourceFavoritesController.resourceFavoriteFilter.Click += async (_, _) => await workspace.ResourceFavoritesController.ToggleResourceFavoriteFilter();
        var clear = FeatureUiElements.ActionButton("清除筛选", async (_, _) =>
        {
            if (workspace.ResourceFavoritesController.resourceFavoritesOnly)
                await workspace.ResourceFavoritesController.ToggleResourceFavoriteFilter();
        });
        workspace.ResourceFavoritesController.resourceFavoriteClear = clear;
        clear.Visibility = Visibility.Collapsed;
        var view = new ResourceSearchHeader(new(workspace.ResourceWorkspaceController.resourceWorkspaceTitle, resourceWorkspaceSubtitle, workspace.ResourceFavoritesController.resourceFavoriteNotice, workspace.ResourceFavoritesController.resourceFavoriteFilter, clear, search, workspace.Controls.SearchQuery, workspace.ResourceFiltersController.resourceSource, [new("游戏版本", workspace.ResourceFiltersController.resourceGame), new("加载器", workspace.ResourceFiltersController.resourceLoader), new("发布类型", workspace.ResourceFiltersController.releaseChannel), new("排序", workspace.ResourceFavoritesController.resourceSort)], actions, workspace.ResourceFavoritesController.resourceGridView, workspace.ResourceFavoritesController.resourceLinearView), (ControlTemplate)workspace.Window.FindResource("SearchInputTemplate"));
        var parent = (Grid)filters.Parent;
        parent.Children.Remove(filters);
        Grid.SetRow(view, 1);
        parent.Children.Add(view);
        workspace.ResourceWorkspaceController.resourceFilterPanel = view;
        workspace.ResourceFavoritesController.resourceSort.SelectionChanged += async (_, _) =>
        {
            if (workspace.ResourceFavoritesController.updatingResourceFilters)
                return;
            workspace.ContentController.searchOffset = 0;
            await workspace.ContentController.RunBrowse(workspace.ContentController.SearchContent);
        };
        workspace.ResourceFavoritesController.resourceGridView.Click += (_, _) => workspace.ResourceFavoritesController.SetResourceView(false);
        workspace.ResourceFavoritesController.resourceLinearView.Click += (_, _) => workspace.ResourceFavoritesController.SetResourceView(true);
        workspace.Controls.SearchResults.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(ResponsiveResourcePanel)));
        ScrollViewer.SetCanContentScroll(workspace.Controls.SearchResults, false);
        workspace.Controls.SearchResults.SetValue(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.Contained);
    }
}
