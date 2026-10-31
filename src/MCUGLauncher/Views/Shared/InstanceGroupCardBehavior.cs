using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BlockLaunch;

/// <summary>Extends header clicks to the enclosing card's border and padding, excluding instance entries.</summary>
internal static class InstanceGroupCardBehavior
{
    internal static void Attach(Border card, Expander expander)
    {
        bool pressedOnCard = false;
        bool hovered = false;
        var restingCursor = card.Cursor;
        var restingBackground = card.Background;
        card.Tag = "InstanceGroupCard";
        if (card.TryFindResource("ThemeSurfaceBrush") is Brush)
            card.SetResourceReference(Border.BackgroundProperty, "ThemeSurfaceBrush");
        bool IsCardSurface(object source, bool includeHeader = false)
        {
            var node = source as DependencyObject;
            var inHeader = false;
            while (node != null && !ReferenceEquals(node, card))
            {
                // The header toggle handles itself; content entries must remain independent.
                if (ReferenceEquals(node, expander.Content))
                    return false;
                if (node is ToggleButton)
                    inHeader = true;
                node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
            }
            return ReferenceEquals(node, card) && (includeHeader || !inHeader);
        }
        void SetHover(bool value)
        {
            if (hovered == value)
                return;
            hovered = value;
            card.Cursor = value ? Cursors.Hand : restingCursor;
            var key = value ? "InstanceGroupHoverBrush" : "ThemeSurfaceBrush";
            if (card.TryFindResource(key) is Brush)
                card.SetResourceReference(Border.BackgroundProperty, key);
            else
                card.Background = restingBackground;
        }
        card.MouseMove += (_, args) => SetHover(IsCardSurface(args.OriginalSource, includeHeader: true));
        card.MouseLeave += (_, _) => SetHover(false);
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
