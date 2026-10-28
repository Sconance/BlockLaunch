using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class FeatureDialogBuilder(MainWindow host)
{
    public InlineDialog Build(string title, StackPanel content)
    {
        var window = new InlineDialog
        {
            Owner = host,
            Title = title,
            Width = 700,
            Height = 700,
            MinWidth = 540,
            MinHeight = 300,
            SizeToContent = SizeToContent.Height,
            Background = new SolidColorBrush(Color.FromRgb(243, 247, 249)),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new ScrollViewer
            {
                Content = content,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(16)
            }
        };
        window.PrepareContent = () =>
        {
            content.MaxWidth = 920;
            if (content.Children.OfType<TextBlock>().FirstOrDefault() is
                {
                } heading && heading.Text == title)
                heading.Visibility = Visibility.Collapsed;
            for (var i = 0; i < content.Children.Count; i++)
                if (content.Children[i] is Button)
                {
                    var actions = new WrapPanel
                    {
                        Margin = new Thickness(0, 6, 0, 0)
                    };
                    while (i < content.Children.Count && content.Children[i] is Button button)
                    {
                        content.Children.RemoveAt(i);
                        button.MinWidth = Math.Max(button.MinWidth, 145);
                        button.HorizontalAlignment = HorizontalAlignment.Left;
                        actions.Children.Add(button);
                    }

                    content.Children.Insert(i, actions);
                }
        };
        host.ConfigureUtilityWindow(window);
        return window;
    }
}
