using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class SettingsNavigationView : Grid, ISettingsNavigationView
{
    readonly StackPanel navigation = new();
    readonly ContentControl page = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    readonly Dictionary<string, RadioButton> buttons = new();
    readonly Style navigationStyle;
    internal Dictionary<string, ImageSource?> CategoryIcons { get; } = new();
    bool synchronizing;
    public event Action<string>? SectionRequested;

    internal SettingsNavigationView(Style navigationStyle)
    {
        this.navigationStyle = navigationStyle;
        ColumnDefinitions.Add(new()
        {
            Width = new GridLength(148)
        });
        ColumnDefinitions.Add(new());
        Children.Add(new ScrollViewer { Content = navigation, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        page.Margin = new Thickness(16, 0, 0, 0);
        SetColumn(page, 1);
        Children.Add(page);
    }

    public void RenderSections(IReadOnlyList<SettingsSection> sections)
    {
        synchronizing = true;
        navigation.Children.Clear();
        buttons.Clear();
        string? group = null;
        foreach (var section in sections)
        {
            if (group != section.Group)
            {
                group = section.Group;
                navigation.Children.Add(new TextBlock { Text = group, FontSize = 11, Margin = new Thickness(10, 12, 0, 6), Foreground = Brushes.SlateGray });
            }
            var label = new Grid();
            label.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(27)
            });
            label.ColumnDefinitions.Add(new());
            label.Children.Add(CategoryIcons.GetValueOrDefault(section.Id) is { } icon
                ? new Image { Source = icon, Width = 20, Height = 20, Margin = new Thickness(0, 0, 8, 0) }
                : new TextBlock { Text = section.Glyph, Width = 27, VerticalAlignment = VerticalAlignment.Center });
            var title = new TextBlock { Text = section.Title, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            SetColumn(title, 1);
            label.Children.Add(title);
            var button = new RadioButton
            {
                Content = label,
                ContentTemplate = null,
                Style = navigationStyle,
                GroupName = "SettingsCategory",
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(8),
                Tag = section.Id,
                ToolTip = section.Title
            };
            button.Checked += (_, _) => { if (!synchronizing) SectionRequested?.Invoke(section.Id); };
            buttons.Add(section.Id, button);
            navigation.Children.Add(button);
        }
        synchronizing = false;
    }

    public void SelectSection(string id)
    {
        synchronizing = true;
        if (buttons.TryGetValue(id, out var button))
            button.IsChecked = true;
        synchronizing = false;
    }

    internal void ShowContent(UIElement? content) => page.Content = content;
}
