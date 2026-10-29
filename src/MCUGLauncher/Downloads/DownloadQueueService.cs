namespace BlockLaunch;

public sealed class DownloadQueueService(OperationCoordinator operations, IDownloadPauseStore? pauseStore = null)
{
    DownloadPauseState pauseState = new();

    public DownloadPauseState PauseState => pauseStore?.Read() ?? pauseState;
    public bool IsPaused => PauseState.IsPaused;

    public IReadOnlyList<DownloadTaskSnapshot> ReadTasks()
    {
        var current = operations.Current?.Record;
        var paused = IsPaused;
        return operations.History()
            .Where(record => record.Download != null)
            .Select(record => current != null && record.Id == current.Id ? current : record)
            .OrderBy(record => record.State is OperationState.Running or OperationState.Pausing or OperationState.Cancelling ? 0
                : record.State is OperationState.Queued or OperationState.Paused ? 1 : 2)
            .ThenByDescending(record => record.Updated)
            .Take(100)
            .Select(record => new DownloadTaskSnapshot(
                record.Id, record.Name, record.Root, record.State, record.StateLabel, record.Message, record.Percent,
                !record.Committing && (record.State is OperationState.Running or OperationState.Queued),
                !record.Committing && (record.State is OperationState.Paused or OperationState.Failed or OperationState.Interrupted
                    || record.State == OperationState.Queued && paused),
                !record.Committing && (record.State is OperationState.Running or OperationState.Queued or OperationState.Paused or OperationState.Failed or OperationState.Interrupted)))
            .ToArray();
    }

    public OperationRecord Enqueue(string name, string root, string target, DownloadRequest request) =>
        operations.Enqueue(name, root, target, request);

    public OperationRecord? NextQueued() => IsPaused ? null : operations.History()
        .Where(record => record.State == OperationState.Queued && record.Download != null)
        .OrderBy(record => record.QueueOrder == 0 ? record.Started.UtcTicks : record.QueueOrder)
        .ThenBy(record => record.Started)
        .FirstOrDefault();

    public Task<OperationRecord> Execute(OperationRecord record, Func<OperationContext, Task> execute)
    {
        if (IsPaused || record.Download == null || record.State != OperationState.Queued)
            throw new InvalidOperationException("请选择等待执行的下载任务。");
        return operations.Run(record.Name, record.Root, record.Target, execute, record);
    }

    public bool ChangeState(string id, OperationState state) => Change(id, state).Changed;

    public DownloadCommandResult Change(string id, OperationState state)
    {
        if (state is not (OperationState.Queued or OperationState.Paused or OperationState.Cancelled))
            return new(false);
        var current = operations.Current;
        var record = current?.Record.Id == id ? current.Record : operations.History().FirstOrDefault(item => item.Id == id);
        if (record?.Download == null || record.Committing)
            return new(false);
        var changed = current?.Record.Id == id
            ? state != OperationState.Queued && operations.Cancel(state == OperationState.Paused)
            : (state == OperationState.Queued && record.State == OperationState.Queued) || operations.ChangePending(id, state);
        if (!changed)
            return new(false);
        var paused = PauseState;
        var next = state switch
        {
            OperationState.Queued => new DownloadPauseState(),
            OperationState.Paused => new DownloadPauseState(true, id),
            _ => paused.TaskId == id ? paused with { TaskId = "" } : paused
        };
        pauseState = next;
        try
        {
            pauseStore?.Save(next);
            return new(true);
        }
        catch (Exception exception)
        {
            return new(true, "下载暂停状态暂未保存：" + exception.Message);
        }
    }

    public DownloadCommandResult Toggle(string id)
    {
        var current = operations.Current;
        var record = current?.Record.Id == id ? current.Record : operations.History().FirstOrDefault(item => item.Id == id);
        if (record?.Download == null)
            return new(false);
        return Change(id, record.State == OperationState.Failed || IsPaused || record.State == OperationState.Paused
            ? OperationState.Queued : OperationState.Paused);
    }

    public bool Move(string id, int direction) => operations.MovePending(id, direction);

    public bool SuspendForShutdown() => operations.Current?.Record.Download != null && operations.Cancel(pause: true, requeueOnStop: true);
}
