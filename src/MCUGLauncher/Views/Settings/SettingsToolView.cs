using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>A reusable tool surface. It has no dependency on a dialog or workspace.</summary>
internal sealed class SettingsToolView : ScrollViewer
{
    internal SettingsToolView(StackPanel body)
    {
        body.MaxWidth = 920;
        // Keep action rows compact in either the settings host or a legacy dialog.
        for (var i = 0; i < body.Children.Count; i++)
            if (body.Children[i] is Button)
            {
                var actions = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
                while (i < body.Children.Count && body.Children[i] is Button button)
                {
                    body.Children.RemoveAt(i);
                    button.MinWidth = Math.Max(button.MinWidth, 145);
                    button.HorizontalAlignment = HorizontalAlignment.Left;
                    actions.Children.Add(button);
                }
                body.Children.Insert(i, actions);
            }
        Content = body;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Margin = new Thickness(16);
    }
}
