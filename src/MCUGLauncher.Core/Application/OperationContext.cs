namespace BlockLaunch;

// One execution's cancellation, progress and commit boundary; owned by OperationCoordinator.
public sealed class OperationContext
{
    long transferredBytes;

    public long TransferredBytes => Interlocked.Read(ref transferredBytes);

    internal void AddTransferredBytes(int bytes)
    {
        if (bytes > 0)
            Interlocked.Add(ref transferredBytes, bytes);
    }

    internal void ReportFileProgress(long current, long total)
    {
        if (Record.Download?.Kind == "resource" && total > 0)
            owner.Report(this, Record.Message, Math.Clamp(current * 100.0 / total, 0, 100));
    }

    readonly OperationCoordinator owner;
    internal readonly CancellationTokenSource Cancellation = new();
    internal bool RequeueOnStop;

    public CancellationToken Token => Cancellation.Token;

    public OperationRecord Record
    {
        get;
    }

    internal OperationContext(OperationCoordinator owner, OperationRecord record)
    {
        this.owner = owner;
        Record = record;
    }

    public void Report(string message, double? percent = null) => owner.Report(this, message, percent);

    public IDisposable BeginCommit()
    {
        lock (owner.Gate)
        {
            Token.ThrowIfCancellationRequested();
            Record.Committing = true;
            owner.Persist(this);
        }

        return new CommitScope(this);
    }

    sealed class CommitScope(OperationContext context) : IDisposable
    {
        public void Dispose()
        {
            lock (context.owner.Gate)
            {
                context.Record.Committing = false;
                context.owner.Persist(context);
            }
        }
    }
}
