using System.Windows;

namespace BlockLaunch;

internal static class UtilityWindowInitializer
{
    public static void Configure(Window window, Action applyTheme)
    {
        window.MaxWidth = Math.Max(620, SystemParameters.WorkArea.Width - 24);
        window.MaxHeight = Math.Max(420, SystemParameters.WorkArea.Height - 24);
        window.MinWidth = Math.Min(window.MinWidth, window.MaxWidth);
        window.MinHeight = Math.Min(window.MinHeight, window.MaxHeight);
        window.Loaded += (_, _) => applyTheme();
    }

    public static void Configure(InlineDialog page, Action applyTheme)
    {
        page.Loaded += (_, _) => applyTheme();
    }
}
