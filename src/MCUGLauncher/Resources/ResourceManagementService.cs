using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class ResourceManagementService(HttpClient http)
{
    public async Task<FrontResourcePresentation> ReadPresentation(string project, CancellationToken token)
    {
        using var document = JsonDocument.Parse(await http.GetStringAsync("https://api.modrinth.com/v2/project/" + Uri.EscapeDataString(project), token));
        var root = document.RootElement;
        var body = root.TryGetProperty("body", out var content) ? content.GetString() ?? "" : "";
        body = body[..Math.Min(body.Length, 12000)];
        body = Regex.Replace(body, @"!\[[^\]]*\]\([^)]*\)", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        body = Regex.Replace(body, @"\[([^\]]+)\]\([^)]*\)", "$1", RegexOptions.None, TimeSpan.FromSeconds(1));
        body = Regex.Replace(body, "<[^>]*>", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        body = Regex.Replace(body, @"(?m)^#{1,6}\s+", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        var gallery = new List<string>();
        if (root.TryGetProperty("gallery", out var images))
            foreach (var image in images.EnumerateArray().Take(3))
                if (image.TryGetProperty("url", out var url) && Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps && uri.Host == "cdn.modrinth.com")
                    gallery.Add(uri.AbsoluteUri);
        var slug = root.TryGetProperty("slug", out var name) ? name.GetString() ?? project : project;
        var type = root.TryGetProperty("project_type", out var kind) ? kind.GetString() ?? "mod" : "mod";
        return new(body.Replace("**", "").Replace("__", "").Replace("```", "").Trim(), gallery,
            "https://modrinth.com/" + Uri.EscapeDataString(type) + "/" + Uri.EscapeDataString(slug));
    }

    public Task<SearchPage> Search(ResourceSearchQuery query, CancellationToken token) =>
        new ContentService(http, token).Search(query.Query, query.GameVersion, query.Loader, query.Kind, query.Offset, query.Sort);

    public async Task<List<ContentVersion>> Versions(string project, ResourceSearchQuery query, CancellationToken token)
    {
        var versions = await new ContentService(http, token).Versions(project, query.GameVersion, query.Loader, query.Kind);
        token.ThrowIfCancellationRequested();
        return versions.Where(version => query.Channel.Length == 0 || version.VersionType == query.Channel).ToList();
    }

    public async Task<SearchPage> SearchFavorites(IEnumerable<ContentProject> projects, ResourceSearchQuery query, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var candidates = projects.ToList();
        if (query.GameVersion.Length > 0 || query.Loader.Length > 0 || query.Channel.Length > 0)
        {
            using var gate = new SemaphoreSlim(4);
            var matches = await Task.WhenAll(candidates.Select(async project =>
            {
                await gate.WaitAsync(token);
                try
                {
                    return (await Versions(project.Id, query, token)).Count > 0 ? project : null;
                }
                finally
                {
                    gate.Release();
                }
            }));
            candidates = matches.OfType<ContentProject>().ToList();
        }

        if (query.Sort == "downloads")
            candidates = candidates.OrderByDescending(project => project.Downloads).ToList();
        token.ThrowIfCancellationRequested();
        return new(candidates.Skip(query.Offset).Take(15).ToList(), candidates.Count);
    }

    public DownloadRequest PrepareDownload(string root, string destination, string kind, ContentVersion version)
    {
        if (InstanceUninstaller.Inside(destination, root))
            throw new IOException("请选择游戏目录之外的独立下载目录。");
        ResourceDownloads.PathFor(destination, kind, version);
        return new() { Kind = "resource", ResourceKind = kind, Content = version, Destination = destination };
    }

    public async Task<InstallPlan> PrepareInstallation(ContentVersion version, ResourceInstallTarget target, string kind, bool gameRunning, CancellationToken token)
    {
        RequireStopped(gameRunning);
        RequireInstalled(target);
        return await new ContentService(http, token).Resolve(version, target.GameVersion, kind == "datapack" ? "datapack" : target.Loader);
    }

    public string ValidateWorld(string directory, string world)
    {
        var fullWorld = Path.GetFullPath(world);
        var saves = Path.GetFullPath(Path.Combine(directory, "saves")) + Path.DirectorySeparatorChar;
        if (!fullWorld.StartsWith(saves, StringComparison.OrdinalIgnoreCase) || !File.Exists(Path.Combine(fullWorld, "level.dat")))
            throw new IOException("请选择当前实例的有效存档目录（含 level.dat）。");
        return fullWorld;
    }

    public Task<int> Install(InstallPlan plan, string directory, bool gameRunning, CancellationToken token, Action<string>? progress = null)
    {
        RequireStopped(gameRunning);
        return new ContentService(http, token).Install(plan, directory, progress);
    }

    public Task<int> Import(ResourceInstallTarget target, string child, string[] files, bool gameRunning, CancellationToken token)
    {
        RequireStopped(gameRunning);
        RequireInstalled(target);
        if (child == "mods" && target.Loader == "vanilla")
            throw new IOException("请先选择已安装加载器的实例。");
        return new ContentService(http, token).ImportFiles(target.Directory, child, files);
    }

    public List<LocalContent> ListLocal(string directory) => ContentService.ListLocal(directory);

    public void Toggle(LocalContent content, bool gameRunning)
    {
        RequireStopped(gameRunning);
        ContentService.Toggle(content);
    }

    public void MoveToBackup(string directory, LocalContent content, bool gameRunning)
    {
        RequireStopped(gameRunning);
        ContentService.MoveToBackup(directory, content);
    }

    public Task<List<ContentUpdate>> CheckUpdates(string directory, string minecraft, string loader, bool gameRunning, CancellationToken token)
    {
        RequireStopped(gameRunning);
        return ContentUpdates.Check(new ContentService(http, token), directory, minecraft, loader, token);
    }

    public Task<ContentUpdatePreview> PreviewUpdates(string directory, IEnumerable<ContentVersion> selected, string minecraft, string loader, bool gameRunning, CancellationToken token)
    {
        RequireStopped(gameRunning);
        return ContentUpdates.Preview(new ContentService(http, token), directory, selected, minecraft, loader, token);
    }

    public Task Restore(string directory, bool gameRunning, CancellationToken token)
    {
        RequireStopped(gameRunning);
        return Task.Run(() => ContentTransaction.RestoreLatest(directory, token), token);
    }

    public DownloadRequest PrepareUpdate(ContentUpdatePreview preview, string directory, string minecraft, string loader) => new()
    {
        Kind = "content",
        PreparedPlan = preview.Plan,
        ContentFingerprint = preview.Fingerprint,
        Destination = directory,
        GameVersion = minecraft,
        Loader = loader
    };

    public static void RequireStopped(bool gameRunning)
    {
        if (gameRunning)
            throw new IOException("请先关闭游戏，再修改实例内容。");
    }

    static void RequireInstalled(ResourceInstallTarget target)
    {
        var profile = Path.Combine(target.Root, "versions", target.Version, target.Version + ".json");
        if (!File.Exists(profile) || !VersionInstallation.IsReady(target.Root, target.Version))
            throw new IOException("请先安装并选择本地游戏版本。");
    }
}
