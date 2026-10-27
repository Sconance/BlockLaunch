using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal static class ModernThemeStyler
{
    public static void Apply(Window window, Grid? sketchHome, bool darkMode)
    {
        if (sketchHome == null || window.Content is not Grid root)
            return;
        Application.Current.Resources["UiSecondaryTextBrush"] = new SolidColorBrush(darkMode ? Color.FromRgb(174, 190, 208) : Color.FromRgb(100, 119, 130));
        var accent = ((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color;
        Application.Current.Resources["UiAccentTextBrush"] = new SolidColorBrush(darkMode ? Color.FromRgb((byte)(accent.R * 0.55 + 115), (byte)(accent.G * 0.55 + 115), (byte)(accent.B * 0.55 + 115)) : accent);
        var header = (DockPanel)root.Children[0];
        header.SetResourceReference(Panel.BackgroundProperty, "ThemeSurfaceBrush");
        foreach (var text in header.Children.OfType<TextBlock>())
            text.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        foreach (var controls in header.Children.OfType<StackPanel>().Where(p => Equals(p.Tag, "WindowControls")))
            foreach (var button in controls.Children.OfType<Button>())
                if (button.Content is TextBlock glyph)
                    glyph.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        var left = (DockPanel)sketchHome.Children[0];
        foreach (var scroll in left.Children.OfType<ScrollViewer>())
            if (scroll.Content is Border card)
                card.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");
    }
}
