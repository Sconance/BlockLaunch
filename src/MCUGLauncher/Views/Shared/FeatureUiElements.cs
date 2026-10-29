using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;
internal static class FeatureUiElements
{
    internal static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 5, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    internal static TextBlock Hint(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.SlateGray,
        Margin = new Thickness(0, 5, 0, 10)
    };
    internal static Button ActionButton(string title, RoutedEventHandler action)
    {
        var b = new Button
        {
            Content = title
        };
        b.Click += action;
        return b;
    }
}
