using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;

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

        var heading = new DockPanel { Margin = new Thickness(0, 3, 0, 12) };
        var favoriteTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var favoriteStatus = new Border { CornerRadius = new CornerRadius(12), Child = resourceFavoriteNotice };
        favoriteStatus.SetResourceReference(Border.BackgroundProperty, "ThemeSelectedBrush");
        favoriteStatus.SetBinding(VisibilityProperty, new Binding("Visibility") { Source = resourceFavoriteNotice });
        favoriteTools.Children.Add(favoriteStatus);
        favoriteTools.Children.Add(resourceFavoriteFilter);
        resourceFavoriteFilter.FontSize = 20;
        resourceFavoriteFilter.Click += async (_, _) => await ToggleResourceFavoriteFilter();
        var clearFavorites = ActionButton("清除筛选", async (_, _) =>
        {
            if (!resourceFavoritesOnly)
                return;
            await ToggleResourceFavoriteFilter();
        });
        resourceFavoriteClear = clearFavorites;
        clearFavorites.Visibility = Visibility.Collapsed;
        favoriteTools.Children.Add(clearFavorites);
        DockPanel.SetDock(favoriteTools, Dock.Right);
        heading.Children.Add(favoriteTools);
        var mark = new TextBlock { Text = "✚", FontSize = 38, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        mark.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        DockPanel.SetDock(mark, Dock.Left);
        heading.Children.Add(mark);
        var title = new StackPanel();
        title.Children.Add(resourceWorkspaceTitle);
        resourceWorkspaceSubtitle.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
        title.Children.Add(resourceWorkspaceSubtitle);
        heading.Children.Add(title);
        filters.Children.Add(heading);

        search.Margin = new Thickness(0, 0, 0, 6);
        search.ColumnDefinitions[2].Width = new GridLength(132);
        search.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(74)
        });
        Grid.SetColumn(search.Children.OfType<Button>().Single(), 3);
        resourceSource.Width = double.NaN;
        resourceSource.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetColumn(resourceSource, 2);
        search.Children.Add(resourceSource);
        SearchQuery.Margin = new Thickness(0, 0, 8, 0);
        SearchQuery.MinHeight = 34;
        SearchQuery.Template = (ControlTemplate)FindResource("SearchInputTemplate");
        SearchQuery.Tag = "搜索模组名称、作者或描述…";
        filters.Children.Add(search);
        var options = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        foreach (var pair in new (string Label, Control Control)[] { ("游戏版本", resourceGame), ("加载器", resourceLoader), ("发布类型", releaseChannel), ("排序", resourceSort) })
        {
            var field = new StackPanel { Width = 112, Margin = new Thickness(0, 0, 8, 0) };
            field.Children.Add(new TextBlock { Text = pair.Label, FontSize = 11, Margin = new Thickness(1, 0, 0, 2) });
            pair.Control.Width = double.NaN;
            pair.Control.Margin = new Thickness(0);
            field.Children.Add(pair.Control);
            options.Children.Add(field);
        }
        foreach (var action in actions)
        {
            action.VerticalAlignment = VerticalAlignment.Bottom;
            action.Margin = new Thickness(0, 15, 6, 0);
            options.Children.Add(action);
        }
        filters.Children.Add(options);
        resourceSort.SelectionChanged += async (_, _) =>
        {
            if (updatingResourceFilters)
                return;
            searchOffset = 0;
            await RunBrowse(SearchContent);
        };
        var views = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 15, 0, 0) };
        foreach (var view in new[] { resourceGridView, resourceLinearView })
        {
            view.Width = 34;
            view.FontSize = 18;
            view.Padding = new Thickness(3);
            view.Margin = new Thickness(0, 0, 4, 0);
            views.Children.Add(view);
        }
        resourceGridView.Click += (_, _) => SetResourceView(false);
        resourceLinearView.Click += (_, _) => SetResourceView(true);
        options.Children.Add(views);
        var panel = new FrameworkElementFactory(typeof(ResponsiveResourcePanel));
        SearchResults.ItemsPanel = new ItemsPanelTemplate(panel);
        ScrollViewer.SetCanContentScroll(SearchResults, false);
        SearchResults.SetValue(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.Contained);
    }
}
