using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class InstallPageView : LauncherPage
{
    public InstallPageView() => InitializeComponent();
    void InstallClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(InstallClick), sender, args);
    void ImportPackClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ImportPackClick), sender, args);
    void FabricClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(FabricClick), sender, args);
    void QuiltClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(QuiltClick), sender, args);
    void LoaderJarClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(LoaderJarClick), sender, args);
}
