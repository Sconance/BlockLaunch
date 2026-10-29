namespace BlockLaunch.Contracts;

public enum FrontDownloadAction { Pause, Resume, Cancel }

public sealed record FrontDownloadTask(
    string Id, string Name, string Root, string Status, string Message, double? Percent,
    bool IsIndeterminate, bool CanPause, bool CanResume, bool CanCancel, string ResumeLabel);

public sealed record FrontDownloadQueue(bool IsPaused, IReadOnlyList<FrontDownloadTask> Tasks);

public sealed record FrontDownloadResult(bool Changed, string Warning = "");

public interface IFrontDownloadTasks
{
    FrontDownloadQueue Read();
    FrontDownloadResult Change(string id, FrontDownloadAction action);
}
