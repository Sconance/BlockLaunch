using System.Windows;

namespace BlockLaunch;

public partial class MainWindow : IContentUpdateInteraction
{
    internal Task OpenContentUpdates(string directory, string key, string minecraft, string loader, string? project = null, object? owner = null) =>
        contentUpdateController.Open(new(prefs.Root, key, directory, minecraft, loader), project, owner);

    async void CheckAllUpdatesClick(object sender, RoutedEventArgs e) => await contentUpdateController.CheckAll();

    async void RestoreContentClick(object sender, RoutedEventArgs e) => await contentUpdateController.Restore(ManagedDirectory());

    internal Task RestoreContent(string directory) => contentUpdateController.Restore(directory);

    bool IContentUpdateInteraction.IsBusy => busy;

    bool IContentUpdateInteraction.IsGameRunning => IsGameRunning;

    ResourceInstallTarget IContentUpdateInteraction.CaptureTarget()
    {
        var target = Target();
        return new(prefs.Root, target.Version, ManagedDirectory(), target.GameVersion, target.Loader);
    }

    Task IContentUpdateInteraction.Run(string name, Func<CancellationToken, Task> action) =>
        Run(() => action(CurrentToken), name);

    IContentUpdateSelection IContentUpdateInteraction.CreateSelection(
        IReadOnlyList<ContentUpdate> updates,
        string? project,
        object? owner,
        Func<IContentUpdateSelection, IReadOnlyList<ContentVersion>, Task> preview) =>
        new ContentUpdateSelectionView(FeatureDialog, ActionButton, owner ?? this, updates, project, preview, () => busy);

    void IContentUpdateInteraction.EnqueueUpdate(ResourceInstallTarget target, string name, DownloadRequest request) =>
        EnqueueDownload(name, target.Version, request, target.Root);

    bool IContentUpdateInteraction.ConfirmRestore() =>
        InlineMessageBox.Show("回退最近一次内容安装/更新？\n更新后手动改动的文件会阻止回退。存档不受影响。", "内容回退", MessageBoxButton.OKCancel) == MessageBoxResult.OK;

    void IContentUpdateInteraction.RestoreCompleted()
    {
        RefreshLocalContent();
        Log("最近一次内容操作已回退。");
    }

    void IContentUpdateInteraction.ShowError(string message) => InlineMessageBox.Show(message);
}
