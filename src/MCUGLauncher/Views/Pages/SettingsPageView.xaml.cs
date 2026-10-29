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
    internal (UIElement Launch, UIElement About) TakeSections()
    {
        SettingsSections.Children.Remove(AboutCard);
        return (SettingsRoot, new ScrollViewer { Content = AboutCard, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    void SaveClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(SaveClick), sender, args);
    void NoticesClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(NoticesClick), sender, args);
}
