using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class ResourcePageView : LauncherPage
{
    public ResourcePageView() => InitializeComponent();
    void SearchKeyDown(object sender, KeyEventArgs args) => RaisePageAction(nameof(SearchKeyDown), sender, args);
    void ContentTypeChanged(object sender, SelectionChangedEventArgs args) => RaisePageAction(nameof(ContentTypeChanged), sender, args);
    void SearchClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(SearchClick), sender, args);
    void ProjectChanged(object sender, SelectionChangedEventArgs args) => RaisePageAction(nameof(ProjectChanged), sender, args);
    void InstallContentClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(InstallContentClick), sender, args);
    void ProjectPageClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ProjectPageClick), sender, args);
    void PreviousSearch(object sender, RoutedEventArgs args) => RaisePageAction(nameof(PreviousSearch), sender, args);
    void NextSearch(object sender, RoutedEventArgs args) => RaisePageAction(nameof(NextSearch), sender, args);
}
