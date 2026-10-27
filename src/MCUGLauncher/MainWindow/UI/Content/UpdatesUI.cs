using System.Windows;

namespace BlockLaunch;

public partial class MainWindow
{
    internal Task OpenContentUpdates(string directory, string key, string minecraft, string loader, string? project = null, object? owner = null) =>
        contentUpdateController.Open(directory, key, minecraft, loader, project, owner);

    async void CheckAllUpdatesClick(object sender, RoutedEventArgs e) => await contentUpdateController.CheckAll();

    async void RestoreContentClick(object sender, RoutedEventArgs e) => await contentUpdateController.Restore(ManagedDirectory());

    internal Task RestoreContent(string directory) => contentUpdateController.Restore(directory);
}
