using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;
internal sealed class ExperienceThemeController
{
    readonly LauncherWorkspace workspace;
    internal ExperienceThemeController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal readonly ConditionalWeakTable<DependencyObject, OriginalTheme> themeOriginals = new();
    internal sealed class OriginalTheme
    {
        internal Brush? Background;
        internal Brush? Foreground;
    }

    internal void ThemeWindow(Window window)
    {
        var dark = workspace.LaunchCoordinator.prefs.DarkMode;
        var surface = new SolidColorBrush(Color.FromRgb(33, 40, 55));
        var background = new SolidColorBrush(Color.FromRgb(23, 29, 42));
        var text = new SolidColorBrush(Color.FromRgb(214, 225, 232));
        Application.Current.Resources["ThemeSurfaceBrush"] = dark ? surface : Brushes.White;
        Application.Current.Resources["ThemeInputBrush"] = dark ? new SolidColorBrush(Color.FromRgb(40, 48, 65)) : new SolidColorBrush(Color.FromRgb(246, 249, 251));
        Application.Current.Resources["ThemeTextBrush"] = dark ? text : new SolidColorBrush(Color.FromRgb(36, 59, 73));
        Application.Current.Resources["ThemeSelectedBrush"] = dark ? new SolidColorBrush(Color.FromRgb(48, 65, 95)) : new SolidColorBrush(Color.FromRgb(232, 242, 248));
        Application.Current.Resources["ThemeBorderBrush"] = dark ? new SolidColorBrush(Color.FromRgb(68, 79, 100)) : new SolidColorBrush(Color.FromRgb(214, 225, 232));
        bool Light(Brush? brush) => brush is SolidColorBrush b && b.Color.A > 0 && b.Color.R > 218 && b.Color.G > 218 && b.Color.B > 218;
        bool Dark(Brush? brush) => brush is SolidColorBrush b && b.Color.A > 0 && b.Color.R < 130 && b.Color.G < 130 && b.Color.B < 180;
        foreach (var node in CompactShellController.Elements(window).Prepend(window))
        {
            if (node is Control control)
            {
                if (control is RadioButton)
                    continue;
                // Selected/hover foreground belongs to its template triggers.
                var old = themeOriginals.GetValue(control, _ => new() { Background = control.Background, Foreground = control.Foreground });
                control.SetCurrentValue(Control.BackgroundProperty, dark && Light(old.Background) ? surface : old.Background);
                control.SetCurrentValue(Control.ForegroundProperty, dark && Dark(old.Foreground) ? text : old.Foreground);
            }
            else if (node is Border border)
            {
                var old = themeOriginals.GetValue(border, _ => new() { Background = border.Background });
                border.Background = dark && Light(old.Background) ? surface : old.Background;
            }
            else if (node is TextBlock block)
            {
                var old = themeOriginals.GetValue(block, _ => new() { Foreground = block.Foreground });
                block.Foreground = dark && Dark(old.Foreground) ? text : old.Foreground;
            }
        }

        var original = themeOriginals.GetValue(window, _ => new() { Background = window.Background });
        window.Background = dark ? background : original.Background;
        if (workspace.InlineHostController.inlineHost != null)
            workspace.InlineHostController.inlineHost.Background = window.Background;
        workspace.ModernShellController.ApplyModernChrome();
        workspace.AppearanceController.ApplyBackground();
    }

    internal void ConfigureUtilityWindow(Window window) => UtilityWindowInitializer.Configure(window, () => ThemeWindow(window));

    internal void ConfigureUtilityWindow(InlineDialog page)
    {
        UtilityWindowInitializer.Configure(page, () => ThemeWindow(workspace.Window));
    }
}
