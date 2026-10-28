namespace BlockLaunch;

public sealed class DownloadQueueService(OperationCoordinator operations)
{
    public OperationRecord Enqueue(string name, string root, string target, DownloadRequest request) =>
        operations.Enqueue(name, root, target, request);

    public OperationRecord? NextQueued() => operations.History()
        .Where(record => record.State == OperationState.Queued && record.Download != null)
        .OrderBy(record => record.QueueOrder == 0 ? record.Started.UtcTicks : record.QueueOrder)
        .ThenBy(record => record.Started)
        .FirstOrDefault();

    public Task<OperationRecord> Execute(OperationRecord record, Func<OperationContext, Task> execute)
    {
        if (record.Download == null || record.State != OperationState.Queued)
            throw new InvalidOperationException("请选择等待执行的下载任务。");
        return operations.Run(record.Name, record.Root, record.Target, execute, record);
    }

    public bool ChangeState(string id, OperationState state)
    {
        if (state is not (OperationState.Queued or OperationState.Paused or OperationState.Cancelled))
            return false;
        var current = operations.Current;
        if (current?.Record.Id == id)
            return current.Record.Download != null && state != OperationState.Queued
                && operations.Cancel(state == OperationState.Paused);
        return operations.ChangePending(id, state);
    }
}
