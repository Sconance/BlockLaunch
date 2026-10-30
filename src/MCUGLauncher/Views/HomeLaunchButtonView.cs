using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class HomeLaunchButtonView
{
    readonly TextBlock title = new() { Text = "▷  启动游戏", FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
    readonly ProgressBar progress = new() { Height = 3, Margin = new Thickness(0, 6, 0, 0), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), BorderThickness = new Thickness(0), Visibility = Visibility.Collapsed };
    internal bool IsLaunching
    {
        get; private set;
    }
    internal HomeLaunchButtonView(Button button, TextBlock version)
    {
        var body = new StackPanel();
        body.Children.Add(title);
        body.Children.Add(version);
        body.Children.Add(progress);
        button.ContentTemplate = null;
        button.Content = body;
    }
    internal void Begin()
    {
        IsLaunching = true;
        title.Text = "正在准备…";
        progress.Visibility = Visibility.Visible;
        progress.IsIndeterminate = true;
    }
    internal void Report(double? percent)
    {
        if (!IsLaunching)
            return;
        progress.IsIndeterminate = percent == null;
        if (percent is { } value && double.IsFinite(value))
            progress.Value = Math.Clamp(value, 0, 100);
    }
    internal void Launching() => title.Text = "正在启动…";
    internal void End()
    {
        IsLaunching = false;
        progress.IsIndeterminate = false;
        progress.Visibility = Visibility.Collapsed;
        title.Text = "▷  启动游戏";
    }
}
