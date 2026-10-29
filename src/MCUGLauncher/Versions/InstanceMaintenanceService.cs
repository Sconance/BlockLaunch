using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class InstanceMaintenanceService(Preferences preferences, VersionManagementService versions, string configurationDirectory)
{
    public string DirectoryFor(string key)
    {
        if (!versions.ProfileKeys(preferences.Root).Contains(key))
            throw new IOException("实例不存在，请刷新列表。");
        return versions.InstanceDirectory(preferences.Root, key, preferences.Launch);
    }

    public IReadOnlyList<FrontBackup> Backups(string key) => SaveProtection.List(BackupRoot, DirectoryFor(key))
        .Select(value => new FrontBackup(value.Id, value.Reason, value.Created, value.Worlds.Select(world => world.World).ToArray())).ToArray();

    string BackupRoot => Path.Combine(configurationDirectory, "world-backups");

    public Task<SaveBackup?> CreateBackup(string key, string reason, CancellationToken token) =>
        Task.Run(() => SaveProtection.Create(DirectoryFor(key), BackupRoot, reason, preferences.SaveBackupCount, token), token);

    public async Task<string> RestoreBackup(string key, string backupId, string world, CancellationToken token)
    {
        var directory = DirectoryFor(key);
        var backup = SaveProtection.List(BackupRoot, directory).SingleOrDefault(value => value.Id == backupId)
            ?? throw new IOException("备份已变化，请刷新列表。");
        var item = backup.Worlds.SingleOrDefault(value => value.World == world) ?? throw new IOException("请选择备份中的世界。");
        var archive = SaveProtection.ArchivePath(BackupRoot, directory, backup, item);
        return await Task.Run(() => WorldArchives.Import(directory, archive, token: token), token);
    }

    public Task<DiagnosticSnapshot> DiagnosticsFor(string key, string processLog, CancellationToken token) =>
        Task.Run(() => Diagnostics.Capture(new(DirectoryFor(key), key, processLog), token), token);

    public IReadOnlyList<ScreenshotEntry> Screenshots(string key) => WorldArchives.Screenshots(DirectoryFor(key));

    public Task<string> ImportWorld(string key, string archive, CancellationToken token) =>
        Task.Run(() => WorldArchives.Import(DirectoryFor(key), archive, token: token), token);

    public Task BackupWorld(string key, string world, string target, CancellationToken token)
    {
        var selected = WorldArchives.List(DirectoryFor(key)).SingleOrDefault(value => value.Directory == world)
            ?? throw new IOException("请选择当前实例中的有效世界。");
        return Task.Run(() => WorldArchives.Backup(selected.Directory, target, token: token), token);
    }
}
