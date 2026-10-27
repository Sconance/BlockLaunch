using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BlockLaunch;

// Persisted task models. Keep property names and enum values compatible with existing queues.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationState
{
    Running,
    Cancelling,
    Completed,
    Cancelled,
    Failed,
    Interrupted,
    Queued,
    Pausing,
    Paused
}

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
    public string PackDisplayName { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public bool Optional
    {
        get;
        set;
    }
    public ContentVersion? Content
    {
        get;
        set;
    }
    public InstallPlan? PreparedPlan
    {
        get;
        set;
    }
    public string? ContentFingerprint
    {
        get;
        set;
    }
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
    public OperationState State
    {
        get;
        set;
    }
    public DateTimeOffset Started { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
    public double? Percent
    {
        get;
        set;
    }
    public int ProcessId { get; set; } = Environment.ProcessId;
    public long ProcessStartTicks { get; set; } = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
    public bool Committing
    {
        get;
        set;
    }
    public DownloadRequest? Download
    {
        get;
        set;
    }
    public long QueueOrder
    {
        get;
        set;
    }

    public string StateLabel => State switch
    {
        OperationState.Running => "进行中",
        OperationState.Cancelling => "正在取消",
        OperationState.Pausing => "正在暂停",
        OperationState.Paused => "已暂停",
        OperationState.Queued => "排队中",
        OperationState.Completed => "已完成",
        OperationState.Cancelled => "已取消",
        OperationState.Failed => "失败",
        _ => "上次中断"
    };

    public string Summary => $"{Name} · {StateLabel} · {Started.ToLocalTime():MM-dd HH:mm:ss}";

    public string Detail => $"{Target}  {Message}";
}
