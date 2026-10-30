using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BlockLaunch;

public sealed record ResourceFilterField(string Label, Control Editor);
public sealed record ResourceSearchHeaderControls(
    TextBlock Title, TextBlock Subtitle, TextBlock FavoriteNotice,
    Button FavoriteFilter, Button ClearFavorites, Grid SearchRow, TextBox Query, ComboBox Source,
    IReadOnlyList<ResourceFilterField> Filters, IReadOnlyList<Button> Actions, Button Cards, Button List);

/// <summary>Layout for resource browsing. It has no access to search services, settings or MainWindow.</summary>
public sealed class ResourceSearchHeader : StackPanel
{
    public ResourceSearchHeader(ResourceSearchHeaderControls controls, ControlTemplate inputTemplate)
    {
        var heading = new DockPanel { Margin = new Thickness(0, 3, 0, 12) };
        var favorites = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var status = new Border { CornerRadius = new CornerRadius(12), Child = controls.FavoriteNotice };
        status.SetResourceReference(Border.BackgroundProperty, "ThemeSelectedBrush");
        status.SetBinding(VisibilityProperty, new Binding("Visibility") { Source = controls.FavoriteNotice });
        favorites.Children.Add(status);
        controls.FavoriteFilter.FontSize = 20;
        favorites.Children.Add(controls.FavoriteFilter);
        favorites.Children.Add(controls.ClearFavorites);
        DockPanel.SetDock(favorites, Dock.Right);
        heading.Children.Add(favorites);
        var mark = new TextBlock { Text = "✚", FontSize = 38, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        mark.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        DockPanel.SetDock(mark, Dock.Left);
        heading.Children.Add(mark);
        var title = new StackPanel();
        title.Children.Add(controls.Title);
        controls.Subtitle.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
        title.Children.Add(controls.Subtitle);
        heading.Children.Add(title);
        Children.Add(heading);

        var search = controls.SearchRow;
        search.Margin = new Thickness(0, 0, 0, 6);
        search.ColumnDefinitions[2].Width = new GridLength(132);
        search.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(74)
        });
        Grid.SetColumn(search.Children.OfType<Button>().Single(), 3);
        controls.Source.Width = double.NaN;
        controls.Source.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetColumn(controls.Source, 2);
        search.Children.Add(controls.Source);
        controls.Query.Margin = new Thickness(0, 0, 8, 0);
        controls.Query.MinHeight = 34;
        controls.Query.Template = inputTemplate;
        controls.Query.Tag = "搜索模组名称、作者或描述…";
        Children.Add(search);

        var options = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        foreach (var filter in controls.Filters)
        {
            var field = new StackPanel { Width = 112, Margin = new Thickness(0, 0, 8, 0) };
            field.Children.Add(new TextBlock { Text = filter.Label, FontSize = 11, Margin = new Thickness(1, 0, 0, 2) });
            filter.Editor.Width = double.NaN;
            filter.Editor.Margin = new Thickness(0);
            field.Children.Add(filter.Editor);
            field.SetBinding(VisibilityProperty, new Binding("Visibility") { Source = filter.Editor });
            options.Children.Add(field);
        }
        foreach (var action in controls.Actions)
        {
            action.VerticalAlignment = VerticalAlignment.Bottom;
            action.Margin = new Thickness(0, 15, 6, 0);
            options.Children.Add(action);
        }
        var views = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 15, 0, 0) };
        foreach (var button in new[] { controls.Cards, controls.List })
        {
            button.Width = 34;
            button.FontSize = 18;
            button.Padding = new Thickness(3);
            button.Margin = new Thickness(0, 0, 4, 0);
            views.Children.Add(button);
        }
        options.Children.Add(views);
        Children.Add(options);
    }
}
