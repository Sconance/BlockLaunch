using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class TasksPageView : LauncherPage
{
    public TasksPageView() => InitializeComponent();
    void PauseDownloadClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(PauseDownloadClick), sender, args);
    void ResumeDownloadClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ResumeDownloadClick), sender, args);
    void CancelDownloadClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(CancelDownloadClick), sender, args);
    void RefreshOperationsClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(RefreshOperationsClick), sender, args);
    void LocateOperationClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(LocateOperationClick), sender, args);
}
