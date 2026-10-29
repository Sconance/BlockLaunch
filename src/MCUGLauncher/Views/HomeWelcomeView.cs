using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public sealed class HomeWelcomeView : StackPanel
{
    public HomeWelcomeView(string brandName)
    {
        Margin = new Thickness(36);
        VerticalAlignment = VerticalAlignment.Center;
        Children.Add(new TextBlock { Text = "MINECRAFT JAVA EDITION", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(100, 119, 130)), Margin = new Thickness(0, 0, 0, 14) });
        Children.Add(new TextBlock { Text = brandName, FontSize = 36, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 109, 184)) });
        Children.Add(new TextBlock { Text = "选择版本，开始游戏。", FontSize = 16, Margin = new Thickness(0, 14, 0, 26) });
        Children.Add(new Border { Height = 2, Width = 48, HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(Color.FromRgb(158, 167, 171)), Margin = new Thickness(0, 0, 0, 22) });
    }
}
