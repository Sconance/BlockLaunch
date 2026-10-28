namespace BlockLaunch;

internal interface IContentUpdateInteraction
{
    bool IsBusy { get; }
    bool IsGameRunning { get; }
    ResourceInstallTarget CaptureTarget();
    Task Run(string name, Func<CancellationToken, Task> action);
    IContentUpdateSelection CreateSelection(
        IReadOnlyList<ContentUpdate> updates,
        string? project,
        object? owner,
        Func<IContentUpdateSelection, IReadOnlyList<ContentVersion>, Task> preview);
    void EnqueueUpdate(ResourceInstallTarget target, string name, DownloadRequest request);
    bool ConfirmRestore();
    void RestoreCompleted();
    void ShowError(string message);
}

internal interface IContentUpdateSelection
{
    void Show();
    void Close();
    bool ConfirmPreview(ContentUpdatePreview preview);
}
