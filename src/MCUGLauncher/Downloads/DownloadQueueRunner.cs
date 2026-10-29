namespace BlockLaunch;

public sealed class DownloadQueueRunner(
    DownloadQueueService queue,
    Func<OperationRecord, OperationContext, Task> execute,
    Func<bool> canRun,
    Action<string> report)
{
    bool stopping;
    public bool IsRunning { get; private set; }
    public Task Completion { get; private set; } = Task.CompletedTask;

    public Task Tick()
    {
        if (stopping || IsRunning || !canRun())
            return Task.CompletedTask;
        var next = queue.NextQueued();
        if (next == null)
            return Task.CompletedTask;
        IsRunning = true;
        Completion = Run(next);
        return Completion;
    }

    async Task Run(OperationRecord record)
    {
        try
        {
            var result = await queue.Execute(record, context => execute(record, context));
            report(result.Name + " · " + result.StateLabel);
        }
        catch (Exception exception)
        {
            report("下载失败，可在任务页重试：" + LogPrivacy.Redact(exception.Message));
        }
        finally
        {
            IsRunning = false;
        }
    }

    public async Task Stop()
    {
        stopping = true;
        if (IsRunning)
            queue.SuspendForShutdown();
        await Completion;
    }
}
