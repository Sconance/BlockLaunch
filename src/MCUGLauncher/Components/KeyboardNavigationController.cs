using System.Windows;

namespace BlockLaunch;

/// <summary>Initializes keyboard-friendly rendering and tooltips without application state.</summary>
internal sealed class KeyboardNavigationController(Window window, Func<IEnumerable<(FrameworkElement Control, string Hint)>> getHints)
{
    internal void BuildKeyboardNavigation()
    {
        window.UseLayoutRounding = true;
        window.SnapsToDevicePixels = true;
        foreach (var (control, hint) in getHints())
            control.ToolTip = hint;
    }
}
