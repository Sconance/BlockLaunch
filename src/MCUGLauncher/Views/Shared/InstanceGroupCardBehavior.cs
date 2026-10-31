using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BlockLaunch;

/// <summary>Extends header clicks to the enclosing card's border and padding, excluding instance entries.</summary>
internal static class InstanceGroupCardBehavior
{
    internal static void Attach(Border card, Expander expander)
    {
        bool pressedOnCard = false;
        bool IsCardSurface(object source)
        {
            var node = source as DependencyObject;
            while (node != null && !ReferenceEquals(node, card))
            {
                // The header toggle handles itself; content entries must remain independent.
                if (node is ToggleButton || ReferenceEquals(node, expander.Content))
                    return false;
                node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
            }
            return ReferenceEquals(node, card);
        }
        card.PreviewMouseLeftButtonDown += (_, args) => pressedOnCard = IsCardSurface(args.OriginalSource);
        card.PreviewMouseLeftButtonUp += (_, args) =>
        {
            var toggle = pressedOnCard && IsCardSurface(args.OriginalSource);
            pressedOnCard = false;
            if (!toggle)
                return;
            expander.IsExpanded = !expander.IsExpanded;
            args.Handled = true;
        };
    }
}
