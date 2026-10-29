using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class DownloadFrontendService(DownloadQueueService queue) : IFrontDownloadTasks
{
    public FrontDownloadQueue Read() => new(queue.IsPaused, queue.ReadTasks().Select(task => new FrontDownloadTask(
        task.Id, task.Name, task.Root, task.StateLabel, task.Message, task.Percent,
        task.State == OperationState.Running && task.Percent == null,
        task.CanPause, task.CanResume, task.CanCancel, task.State == OperationState.Failed ? "重试" : "继续")).ToArray());

    public FrontDownloadResult Change(string id, FrontDownloadAction action)
    {
        var state = action switch
        {
            FrontDownloadAction.Pause => OperationState.Paused,
            FrontDownloadAction.Resume => OperationState.Queued,
            FrontDownloadAction.Cancel => OperationState.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        var result = queue.Change(id, state);
        return new(result.Changed, result.Warning);
    }
}
