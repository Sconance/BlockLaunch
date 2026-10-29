using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

public partial class ContentManagementPageView : LauncherPage
{
    public ContentManagementPageView() => InitializeComponent();
    void ImportModClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ImportModClick), sender, args);
    void ImportResourceClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ImportResourceClick), sender, args);
    void RefreshContentClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(RefreshContentClick), sender, args);
    void ModsFolderClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ModsFolderClick), sender, args);
    void TrashFolderClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(TrashFolderClick), sender, args);
    void ToggleContentClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(ToggleContentClick), sender, args);
    void UpdateContentClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(UpdateContentClick), sender, args);
    void CheckAllUpdatesClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(CheckAllUpdatesClick), sender, args);
    void RestoreContentClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(RestoreContentClick), sender, args);
    void RemoveContentClick(object sender, RoutedEventArgs args) => RaisePageAction(nameof(RemoveContentClick), sender, args);
}
