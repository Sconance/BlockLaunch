using System.Diagnostics;
using System.Text.Json.Serialization;
namespace BlockLaunch;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationState { Running, Cancelling, Completed, Cancelled, Failed, Interrupted, Queued, Pausing, Paused }
public sealed class DownloadRequest
{
    public string Kind { get; set; } = "game";
    public string Version { get; set; } = "";
    public string Loader { get; set; } = "vanilla";
    public string LoaderVersion { get; set; } = "";
    public string Java { get; set; } = "";
    public string Destination { get; set; } = "";
    public string File { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public string PackInstance { get; set; } = "pack-" + Guid.NewGuid().ToString("N")[..12];
    public string GameVersion { get; set; } = "";
    public bool Optional { get; set; }
    public ContentVersion? Content { get; set; }
    public InstallPlan? PreparedPlan { get; set; }
    public string? ContentFingerprint { get; set; }
    public string ResourceKind { get; set; } = "mod";
    public string BackupInstanceDirectory { get; set; } = "";
}
public class OperationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Root { get; set; } = "";
    public string Target { get; set; } = "";
    public string Message { get; set; } = "";
    public OperationState State { get; set; }
    public DateTimeOffset Started { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
    public double? Percent { get; set; }
    public int ProcessId { get; set; } = Environment.ProcessId;
    public long ProcessStartTicks { get; set; } = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
    public bool Committing { get; set; }
    public DownloadRequest? Download { get; set; }
    public long QueueOrder { get; set; }
    public string StateLabel => State switch { OperationState.Running => "进行中", OperationState.Cancelling => "正在取消", OperationState.Pausing => "正在暂停", OperationState.Paused => "已暂停", OperationState.Queued => "排队中", OperationState.Completed => "已完成", OperationState.Cancelled => "已取消", OperationState.Failed => "失败", _ => "上次中断" };
    public string Summary => $"{Name} · {StateLabel} · {Started.ToLocalTime():MM-dd HH:mm:ss}";
    public string Detail => $"{Target}  {Message}";
}
public sealed class OperationContext
{
    long transferredBytes;
    public long TransferredBytes => Interlocked.Read(ref transferredBytes);
    internal void AddTransferredBytes(int bytes) { if (bytes > 0) Interlocked.Add(ref transferredBytes, bytes); }
    internal void ReportFileProgress(long current, long total)
    {
        if (Record.Download?.Kind == "resource" && total > 0) owner.Report(this, Record.Message, Math.Clamp(current * 100.0 / total, 0, 100));
    }
    readonly OperationCoordinator owner;
    internal readonly CancellationTokenSource Cancellation = new();
    internal bool RequeueOnStop;
    public CancellationToken Token => Cancellation.Token;
    public OperationRecord Record { get; }
    internal OperationContext(OperationCoordinator owner, OperationRecord record) { this.owner = owner; Record = record; }
    public void Report(string message, double? percent = null) => owner.Report(this, message, percent);
    public IDisposable BeginCommit()
    {
        lock (owner.Gate)
        {
            Token.ThrowIfCancellationRequested(); Record.Committing = true; owner.Persist(this);
        }
        return new CommitScope(this);
    }
    sealed class CommitScope(OperationContext context) : IDisposable
    { public void Dispose() { lock (context.owner.Gate) { context.Record.Committing = false; context.owner.Persist(context); } } }
}
public static class OperationExecution
{
    internal static readonly AsyncLocal<OperationContext?> Ambient = new();
    public static IDisposable BeginCommit() => Ambient.Value?.BeginCommit() ?? new Empty();
    sealed class Empty : IDisposable { public void Dispose() { } }
}
public sealed class OperationCoordinator
{
    readonly string directory;
    internal readonly object Gate = new();
    DateTime lastPersist;
    public OperationContext? Current { get; private set; }
    public event Action<OperationRecord>? Changed;
    public OperationCoordinator(string directory) { this.directory = directory; Directory.CreateDirectory(directory); Recover(); }
    string FileFor(OperationRecord record) => Path.Combine(directory, record.Id + ".json");
    internal void Persist(OperationContext context)
    { context.Record.Updated = DateTimeOffset.UtcNow; AtomicFile.WriteJson(FileFor(context.Record), context.Record); Changed?.Invoke(Snapshot(context.Record)); }
    static OperationRecord Snapshot(OperationRecord record) => System.Text.Json.JsonSerializer.Deserialize<OperationRecord>(System.Text.Json.JsonSerializer.Serialize(record))!;
    internal void Report(OperationContext context, string message, double? percent)
    {
        lock (Gate)
        {
            if (Current != context || context.Record.State is not (OperationState.Running or OperationState.Cancelling)) return;
            if (context.Record.State == OperationState.Cancelling) return;
            context.Record.Message = message; context.Record.Percent = percent;
            if ((DateTime.UtcNow - lastPersist).TotalMilliseconds < 200) return;
            lastPersist = DateTime.UtcNow;
            try { Persist(context); } catch (IOException) { context.Record.Message = "进度记录暂时无法保存，请检查磁盘。"; Changed?.Invoke(Snapshot(context.Record)); }
        }
    }
    public bool Cancel(bool pause = false, bool requeueOnStop = false)
    {
        lock (Gate)
        {
            var context = Current; if (context == null || context.Record.Committing || context.Record.State != OperationState.Running || pause && context.Record.Download == null) return false;
            context.RequeueOnStop = requeueOnStop;
            context.Record.State = pause ? OperationState.Pausing : OperationState.Cancelling; context.Record.Message = pause ? "正在暂停并保存进度…" : "正在取消，请等待清理完成…";
            Persist(context); context.Cancellation.Cancel(); return true;
        }
    }
    public OperationRecord Enqueue(string name, string root, string target, DownloadRequest request)
    {
        lock (Gate)
        {
            var record = new OperationRecord { Name = name, Root = Path.GetFullPath(root), Target = target, Download = request, State = OperationState.Queued, Message = "等待执行", QueueOrder = Math.Max(DateTimeOffset.UtcNow.UtcTicks, History().Select(r => r.QueueOrder).DefaultIfEmpty().Max()+1) };
            AtomicFile.WriteJson(FileFor(record), record); Changed?.Invoke(Snapshot(record)); return record;
        }
    }
    public bool ChangePending(string id, OperationState state)
    {
        lock (Gate)
        {
            var record = History().FirstOrDefault(r => r.Id == id);
            if (record?.Download == null || record.Id == Current?.Record.Id) return false;
            var allowed = state switch {
                OperationState.Queued => record.State is OperationState.Paused or OperationState.Failed or OperationState.Interrupted,
                OperationState.Paused => record.State == OperationState.Queued,
                OperationState.Cancelled => record.State is OperationState.Queued or OperationState.Paused or OperationState.Failed or OperationState.Interrupted,
                _ => false };
            if (!allowed) return false;
            record.State = state; record.Message = state == OperationState.Queued ? "等待继续 / 重试" : state == OperationState.Paused ? "已暂停，保留下载进度" : "已取消";
            record.Updated = DateTimeOffset.UtcNow; AtomicFile.WriteJson(FileFor(record), record); Changed?.Invoke(Snapshot(record)); return true;
        }
    }
    public async Task<OperationRecord> Run(string name, string root, string target, Func<OperationContext, Task> action, OperationRecord? queued = null)
    {
        OperationContext context;
        lock (Gate)
        {
            if (Current != null) throw new InvalidOperationException("已有任务正在进行。");
            var record = queued == null ? new OperationRecord { Name = name, Root = root, Target = target } : AtomicFile.ReadJson<OperationRecord>(FileFor(queued));
            if (queued != null && record.State != OperationState.Queued) throw new InvalidOperationException("此任务已不在队列中。");
            record.State = OperationState.Running; record.ProcessId = Environment.ProcessId; record.ProcessStartTicks = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks; record.Percent = null;
            context = new(this, record);
            Current = context;
            try { Persist(context); } catch { Current = null; context.Cancellation.Dispose(); throw; }
        }
        OperationExecution.Ambient.Value = context;
        try { await action(context); lock (Gate) { context.Record.State = OperationState.Completed; context.Record.Message = "完成"; } }
        catch (OperationCanceledException) when (context.Token.IsCancellationRequested) { lock (Gate) { var paused = context.Record.State == OperationState.Pausing; context.Record.State = context.RequeueOnStop ? OperationState.Queued : paused ? OperationState.Paused : OperationState.Cancelled; context.Record.Message = context.RequeueOnStop ? "等待下次启动继续" : paused ? "已暂停；继续时校验已有文件并恢复下载" : "已取消，已校验缓存保留"; } }
        catch (Exception ex) { lock (Gate) { context.Record.State = OperationState.Failed; context.Record.Message = LogPrivacy.Redact(ex.Message); } throw; }
        finally
        {
            OperationExecution.Ambient.Value = null;
            lock (Gate) { context.Record.Committing = false; try { Persist(context); } finally { Current = null; context.Cancellation.Dispose(); } }
        }
        return context.Record;
    }
    public bool MovePending(string id,int direction)
    {
        lock(Gate)
        {
            var pending=History().Where(r=>r.Download!=null && r.State==OperationState.Queued).OrderBy(r=>r.QueueOrder==0 ? r.Started.UtcTicks : r.QueueOrder).ThenBy(r=>r.Started).ToList();
            var index=pending.FindIndex(r=>r.Id==id); var other=index+Math.Sign(direction); if(index<0 || other<0 || other>=pending.Count || direction==0) return false;
            (pending[index],pending[other])=(pending[other],pending[index]); var start=DateTimeOffset.UtcNow.UtcTicks;
            for(var i=0;i<pending.Count;i++) { pending[i].QueueOrder=start+i; AtomicFile.WriteJson(FileFor(pending[i]),pending[i]); Changed?.Invoke(Snapshot(pending[i])); } return true;
        }
    }
    public List<OperationRecord> History()
    {
        lock (Gate)
        {
        var result = new List<OperationRecord>();
        foreach (var file in Directory.GetFiles(directory, "*.json"))
            try
            {
                var record = AtomicFile.ReadJson<OperationRecord>(file);
                if (!Guid.TryParseExact(record.Id, "N", out _) || Path.GetFileNameWithoutExtension(file) != record.Id) continue;
                result.Add(record);
            }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException) { }
        return result.OrderByDescending(r => r.Started).ToList();
        }
    }
    void Recover()
    {
        foreach (var record in History().Where(r => r.State is OperationState.Running or OperationState.Cancelling or OperationState.Pausing))
        {
            var alive = false;
            try { using var process = Process.GetProcessById(record.ProcessId); alive = process.StartTime.ToUniversalTime().Ticks == record.ProcessStartTicks && !process.HasExited; } catch { }
            if (alive) continue;
            record.State = record.State == OperationState.Pausing ? OperationState.Paused : record.State == OperationState.Cancelling ? OperationState.Cancelled : record.Download != null ? OperationState.Queued : OperationState.Interrupted;
            record.Committing = false; record.Message = record.Download != null ? "已恢复上次任务；执行时重新校验文件" : "上次进程异常结束；请重新执行原操作以校验或修复。";
            record.Updated = DateTimeOffset.UtcNow; AtomicFile.WriteJson(FileFor(record), record);
        }
    }
}
