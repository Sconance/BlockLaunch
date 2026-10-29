using System.Windows;

namespace BlockLaunch;

/// <summary>Window rendering setup and the reference tooltip initializer.</summary>
internal sealed class KeyboardNavigationController(Window window, Func<LauncherTooltipInitializer> createTooltips)
{
    internal void BuildKeyboardNavigation()
    {
        window.UseLayoutRounding = true;
        window.SnapsToDevicePixels = true;
        createTooltips().Build();
    }
}
