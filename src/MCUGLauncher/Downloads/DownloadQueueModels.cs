namespace BlockLaunch;

public sealed record DownloadPauseState(bool IsPaused = false, string TaskId = "");

public sealed record DownloadCommandResult(bool Changed, string Warning = "");

public sealed record DownloadTaskSnapshot(
    string Id, string Name, string Root, OperationState State, string StateLabel, string Message,
    double? Percent, bool CanPause, bool CanResume, bool CanCancel);

public interface IDownloadPauseStore
{
    DownloadPauseState Read();
    void Save(DownloadPauseState state);
}
