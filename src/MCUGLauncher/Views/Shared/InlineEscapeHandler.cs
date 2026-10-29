using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Dismisses an expanded selector before Escape navigates away from its editor.</summary>
internal static class InlineEscapeHandler
{
    internal static bool DismissSelector(DependencyObject root)
    {
        if (root is ComboBox { IsDropDownOpen: true } selector)
        {
            selector.IsDropDownOpen = false;
            return true;
        }
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            if (DismissSelector(child))
                return true;
        return false;
    }
}
