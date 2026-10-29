namespace BlockLaunch.Contracts;

public sealed record FrontJava(string Path, int Major, string Architecture, string Vendor, string Error)
{
    public string Label => $"Java {Major} · {Architecture} · {Vendor}\n{Path}" + (Error.Length > 0 ? "\n" + Error : "");
}
public sealed record FrontBackup(string Id, string Reason, DateTimeOffset Created, IReadOnlyList<string> Worlds)
{
    public string Label => $"{Created:yyyy-MM-dd HH:mm:ss} · {Reason} · {Worlds.Count} 个世界";
}
public sealed record FrontContentUpdate(string ProjectId, string Path, string Name, string Current, string Latest, string Error, string VersionId)
{
    public string Label => Name + " · " + (Latest.Length > 0 ? Current + " → " + Latest : Error);
}
public sealed record FrontUpgradeFile(string Path, string Reason, bool Conflict, bool KeepOld);
public sealed record FrontPackUpgrade(string Id, IReadOnlyList<FrontUpgradeFile> Files, bool HasBaseline);

public interface IFrontManagement
{
    Task<UninstallPlan> PlanUninstall(string key, bool deleteSharedData, CancellationToken token);
    Task Uninstall(UninstallPlan plan, CancellationToken token);
    Task<IReadOnlyList<FrontJava>> ScanJava(CancellationToken token);
    Task<IReadOnlyList<FrontContentUpdate>> CheckContentUpdates(string key, CancellationToken token);
    Task EnqueueContentUpdates(string key, IReadOnlyList<string> projectIds, CancellationToken token);
    Task RestoreLatestContent(string key, CancellationToken token);
    IReadOnlyList<FrontBackup> ReadBackups(string key);
    Task CreateBackup(string key, string reason, CancellationToken token);
    Task RestoreBackup(string key, string backupId, string world, CancellationToken token);
    Task SaveJava(string key, string path, bool global, CancellationToken token);
    Task<FrontPackUpgrade> PreparePackUpgrade(string key, string archive, CancellationToken token);
    Task<string> CommitPackUpgrade(string key, string previewId, IReadOnlyList<string> keepOld, CancellationToken token);
    void DiscardPackUpgrade(string previewId);
}
