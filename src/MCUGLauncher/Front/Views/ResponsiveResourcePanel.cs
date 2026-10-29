using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch.Front;

public sealed class ResponsiveResourcePanel : Panel
{
    const double MinimumItemWidth = 250;
    const double Gap = 10;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? MinimumItemWidth : availableSize.Width;
        var columns = Math.Max(1, (int)((width + Gap) / (MinimumItemWidth + Gap)));
        var itemWidth = Math.Max(0, (width - Gap * (columns - 1)) / columns);
        var rowHeight = 0d;
        var totalHeight = 0d;
        for (var index = 0; index < InternalChildren.Count; index++)
        {
            var child = InternalChildren[index];
            child.Measure(new Size(itemWidth, double.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            if ((index + 1) % columns == 0 || index == InternalChildren.Count - 1)
            {
                totalHeight += rowHeight + Gap;
                rowHeight = 0;
            }
        }
        return new Size(width, Math.Max(0, totalHeight - Gap));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Math.Max(1, (int)((finalSize.Width + Gap) / (MinimumItemWidth + Gap)));
        var itemWidth = Math.Max(0, (finalSize.Width - Gap * (columns - 1)) / columns);
        var horizontal = 0d;
        var vertical = 0d;
        var rowHeight = 0d;
        for (var index = 0; index < InternalChildren.Count; index++)
        {
            var child = InternalChildren[index];
            var height = child.DesiredSize.Height;
            rowHeight = Math.Max(rowHeight, height);
            child.Arrange(new Rect(horizontal, vertical, itemWidth, height));
            horizontal += itemWidth + Gap;
            if ((index + 1) % columns == 0 || index == InternalChildren.Count - 1)
            {
                vertical += rowHeight + Gap;
                horizontal = 0;
                rowHeight = 0;
            }
        }
        return finalSize;
    }
}
