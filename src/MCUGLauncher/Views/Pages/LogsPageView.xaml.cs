using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class LogsPageView : LauncherPage
{
    public LogsPageView() => InitializeComponent();
    void ReadGameLogClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ReadGameLogClick), sender, args);
    void LogsFolderClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(LogsFolderClick), sender, args);
}
