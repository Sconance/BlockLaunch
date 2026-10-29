using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class NavigationPageView : LauncherPage
{
    public NavigationPageView()
    {
        InitializeComponent();
        VersionLabel.Text = $"BLOCKLAUNCH  /  {ProductInfo.Version}";
    }
    void Navigate(object sender, RoutedEventArgs args) => RaisePageAction(nameof(Navigate), sender, args);
}
