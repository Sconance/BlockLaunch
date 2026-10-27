using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Arranges the small, paged resource result set into equal-width cards.</summary>
public sealed class ResponsiveResourcePanel : Panel
{
    const double MinimumCardWidth = 280;
    const double CardHeight = 152;

    static int Columns(double width) => Math.Clamp((int)(width / MinimumCardWidth), 1, 4);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? MinimumCardWidth : Math.Max(1, availableSize.Width);
        var columns = Columns(width);
        foreach (UIElement child in InternalChildren)
            child.Measure(new Size(width / columns, CardHeight));
        return new Size(width, Math.Ceiling(InternalChildren.Count / (double)columns) * CardHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Columns(finalSize.Width);
        var width = finalSize.Width / columns;
        for (var index = 0; index < InternalChildren.Count; index++)
            InternalChildren[index].Arrange(new Rect(index % columns * width, index / columns * CardHeight, width, CardHeight));
        return finalSize;
    }
}
