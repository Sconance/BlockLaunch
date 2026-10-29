using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class HomePageView : LauncherPage
{
    public HomePageView() => InitializeComponent();
    void VersionChanged(object sender, SelectionChangedEventArgs args) => RaisePageAction(nameof(VersionChanged), sender, args);
    void FilterChanged(object sender, RoutedEventArgs args) => RaisePageAction(nameof(FilterChanged), sender, args);
    void RefreshClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(RefreshClick), sender, args);
    void LaunchClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(LaunchClick), sender, args);
    void StopClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(StopClick), sender, args);
    void NewInstanceClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(NewInstanceClick), sender, args);
    void RefreshAvatarClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(RefreshAvatarClick), sender, args);
    void LoginClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(LoginClick), sender, args);
}
