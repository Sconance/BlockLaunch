using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class SettingsPageView : LauncherPage
{
    public SettingsPageView()
    {
        InitializeComponent();
        VersionText.Text = ProductInfo.Version;
    }
    void SaveClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(SaveClick), sender, args);
    void NetworkClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(NetworkClick), sender, args);
    void NoticesClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(NoticesClick), sender, args);
}
