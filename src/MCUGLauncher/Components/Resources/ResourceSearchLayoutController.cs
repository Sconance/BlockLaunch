using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;
internal sealed class ResourceSearchLayoutController
{
    readonly ResourceSearchLayoutContext context;
    internal ResourceSearchLayoutController(ResourceSearchLayoutContext context)
    {
        this.context = context;
    }

    internal readonly TextBlock resourceWorkspaceSubtitle = new()
    {
        FontSize = 12,
        Margin = new Thickness(0, 3, 0, 0)
    };
    internal void BuildResourceSearchLayout(StackPanel filters)
    {
        var search = (Grid)context.Query.Parent;
        var row = filters.Children.OfType<WrapPanel>().First();
        var actions = row.Children.OfType<Button>().ToArray();
        row.Children.Clear();
        filters.Children.Clear();
        context.Favorites.resourceFavoriteFilter.Click += async (_, _) => await context.Favorites.ToggleResourceFavoriteFilter();
        var clear = FeatureUiElements.ActionButton("清除筛选", async (_, _) =>
        {
            if (context.Favorites.resourceFavoritesOnly)
                await context.Favorites.ToggleResourceFavoriteFilter();
        });
        context.Favorites.resourceFavoriteClear = clear;
        clear.Visibility = Visibility.Collapsed;
        var fields = new ResourceFilterField[]
        {
            new("游戏版本", context.Filters.resourceGame),
            new("加载器", context.Filters.resourceLoader),
            new("发布类型", context.Filters.releaseChannel),
            new("排序", context.Favorites.resourceSort)
        };
        var header = new ResourceSearchHeaderControls(
            context.Title, resourceWorkspaceSubtitle, context.Favorites.resourceFavoriteNotice,
            context.Favorites.resourceFavoriteFilter, clear, search, context.Query,
            context.Filters.resourceSource, fields, actions,
            context.Favorites.resourceGridView, context.Favorites.resourceLinearView);
        var view = new ResourceSearchHeader(header, (ControlTemplate)context.Owner.FindResource("SearchInputTemplate"));
        var parent = (Grid)filters.Parent;
        parent.Children.Remove(filters);
        Grid.SetRow(view, 1);
        parent.Children.Add(view);
        context.SetFilterPanel(view);
        context.Favorites.resourceSort.SelectionChanged += async (_, _) =>
        {
            if (context.Favorites.updatingResourceFilters)
                return;
            context.ResetSearchOffset();
            await context.Search();
        };
        context.Favorites.resourceGridView.Click += (_, _) => context.Favorites.SetResourceView(false);
        context.Favorites.resourceLinearView.Click += (_, _) => context.Favorites.SetResourceView(true);
        context.Results.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(ResponsiveResourcePanel)));
        ScrollViewer.SetCanContentScroll(context.Results, false);
        context.Results.SetValue(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.Contained);
    }
}

internal sealed record ResourceSearchLayoutContext(
    Window Owner, TextBox Query, ListBox Results, TextBlock Title,
    ResourceFavoritesController Favorites, ResourceFiltersController Filters,
    Action<FrameworkElement> SetFilterPanel, Action ResetSearchOffset, Func<Task> Search);
