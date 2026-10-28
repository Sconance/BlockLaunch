using System.IO;
using System.Net.Http;
using System.Text.Json;
using CmlLib.Core;

namespace BlockLaunch;

public sealed class VersionManagementService(HttpClient http)
{
    public Task<VersionListSnapshot> Read(string root, CancellationToken token) => Read(root, async cancellation =>
    {
        var source = new MinecraftLauncher(MinecraftLauncherParameters.CreateDefault(new MinecraftPath(root), http));
        var versions = await source.GetAllVersionsAsync(cancellation);
        return versions.Select(version => new VersionRow(version.Name, version.Name, version.Type?.ToString() ?? "local", version.ReleaseTime == default ? null : version.ReleaseTime)).ToList();
    }, token);

    internal async Task<VersionListSnapshot> Read(string root, Func<CancellationToken, Task<List<VersionRow>>> readOnline, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var local = LocalVersions(root);
        var known = ProfileKeys(root);
        try
        {
            var versions = (await readOnline(token)).Select(version => version with { Label = VersionLabel(root, version.Id, local) }).ToList();
            token.ThrowIfCancellationRequested();
            foreach (var key in known.Where(key => !versions.Any(version => version.Id == key)))
                versions.Insert(0, new(key, VersionLabel(root, key, local), "local"));
            return new(root, versions, local, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            token.ThrowIfCancellationRequested();
            return new(root, known.Select(key => new VersionRow(key, VersionLabel(root, key, local), "local")).ToList(), local, exception.Message);
        }
    }

    public HashSet<string> LocalVersions(string root) => ProfileKeys(root).Where(key => VersionInstallation.IsReady(root, key)).ToHashSet();

    public HashSet<string> ProfileKeys(string root)
    {
        var directory = Path.Combine(root, "versions");
        return Directory.Exists(directory)
            ? Directory.GetDirectories(directory).Where(path => File.Exists(Path.Combine(path, Path.GetFileName(path) + ".json"))).Select(Path.GetFileName).OfType<string>().ToHashSet()
            : new();
    }

    public string InstanceDirectory(string root, string key, LaunchSettings defaults)
    {
        var catalog = new InstanceCatalog(root);
        var descriptor = catalog.GetOrCreate(key);
        return (descriptor.Launch ?? defaults).Isolation == "共享根目录" ? root : catalog.DirectoryFor(key);
    }

    public string LaunchProfile(string root, string key) => new InstanceCatalog(root).GetOrCreate(key).LaunchProfileId;

    public PackRequirements Requirements(string root, string profile) => ProfileInspector.Read(root, profile);

    public InstanceDescriptor Descriptor(string root, string key) => new InstanceCatalog(root).GetOrCreate(key);

    public void Save(string root, InstanceDescriptor descriptor) => new InstanceCatalog(root).Save(descriptor);

    public Task<UninstallPlan> PlanUninstall(string root, string key, LaunchSettings defaults, bool deleteSharedData = false) =>
        Task.Run(() => InstanceUninstaller.Plan(root, key, defaults, deleteSharedData));

    public Task Uninstall(UninstallPlan plan, LaunchSettings defaults) => Task.Run(() => InstanceUninstaller.Delete(plan, defaults));

    public InstanceTarget Target(string root, string key, IEnumerable<VersionRow> versions)
    {
        if (!File.Exists(Path.Combine(root, "versions", key, key + ".json")) && versions.Any(version => version.Id == key && (version.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || version.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            return new(key, key, "vanilla");
        var requirements = Requirements(root, LaunchProfile(root, key));
        return new(key, requirements.Minecraft, requirements.Loader);
    }

    public Task<List<LoaderRelease>> Loaders(string minecraft, string loader, CancellationToken token) => LoaderCatalog.Get(http, minecraft, loader, token);

    public async Task<DownloadRequest> StableFabric(string version, IEnumerable<VersionRow> versions, CancellationToken token)
    {
        if (!versions.Any(row => row.Id == version && (row.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || row.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            throw new IOException("请选择一个官方原版版本。");
        using var list = JsonDocument.Parse(await http.GetStringAsync("https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(version), token));
        var item = list.RootElement.EnumerateArray().FirstOrDefault(entry => entry.GetProperty("loader").GetProperty("stable").GetBoolean());
        if (item.ValueKind == JsonValueKind.Undefined)
            throw new IOException("该版本暂时没有稳定 Fabric 加载器。");
        return new() { Version = version, Loader = "fabric", LoaderVersion = item.GetProperty("loader").GetProperty("version").GetString()! };
    }

    public string VersionLabel(string root, string id, HashSet<string> local)
    {
        var customFile = Path.Combine(root, "instances", id, ".blocklaunch-instance.json");
        if (File.Exists(customFile))
        {
            try
            {
                var custom = AtomicFile.ReadJson<InstanceDescriptor>(customFile);
                if (custom.DisplayName != id || custom.Favorite)
                    return (custom.Favorite ? "★ " : "") + custom.DisplayName + (VersionInstallation.IsReady(root, id) ? "" : " · 需修复");
            }
            catch
            {
            }
        }

        var record = Path.Combine(root, "instances", id, ".blocklaunch-pack.json");
        if (File.Exists(record))
        {
            try
            {
                var pack = JsonSerializer.Deserialize<InstalledPack>(File.ReadAllText(record));
                if (pack != null)
                    return $"▦ {pack.Name} · {pack.Minecraft} · {pack.Loader}";
            }
            catch
            {
            }
        }

        return id + (local.Contains(id) ? "   · 已安装" : !VersionInstallation.IsReady(root, id) ? " · 未完成，需修复" : "");
    }
}
