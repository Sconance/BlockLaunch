using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class ResourceInstallationService(Preferences preferences, VersionManagementService versions,
    ResourceManagementService resources, DownloadQueueService queue)
{
    public IReadOnlyList<FrontInstallTarget> Targets(string kind, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (kind == "modpack")
            return [new("", "创建新的整合包实例", preferences.Root)];
        var result = new List<FrontInstallTarget>();
        foreach (var key in versions.LocalVersions(preferences.Root))
        {
            token.ThrowIfCancellationRequested();
            var descriptor = versions.Descriptor(preferences.Root, key);
            var requirements = versions.Requirements(preferences.Root, descriptor.LaunchProfileId);
            var directory = versions.InstanceDirectory(preferences.Root, key, preferences.Launch);
            if (kind == "mod" && requirements.Loader == "vanilla")
                continue;
            if (kind == "datapack")
            {
                foreach (var world in WorldArchives.List(directory))
                    result.Add(new(key, descriptor.DisplayName + " · " + world.Name, world.Directory) { World = world.Directory });
            }
            else
                result.Add(new(key, descriptor.DisplayName + " · " + requirements.Minecraft, directory));
        }
        return result;
    }

    public async Task<bool> Enqueue(string project, string version, FrontResourceQuery query, string key, string world,
        Func<string, bool> confirm, bool gameRunning, CancellationToken token)
    {
        var root = preferences.Root;
        if (query.Kind == "modpack")
        {
            var pack = (await resources.Versions(project, new(query.Text, query.GameVersion, query.Loader, query.Kind), token))
                .SingleOrDefault(item => item.Id == version) ?? throw new IOException("整合包版本已变化，请重新读取。");
            if (!pack.Files.Any(file => file.Filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase)))
                throw new IOException("此版本没有支持的 mrpack 文件。");
            if (!confirm("将整合包创建为新实例？不会覆盖已有实例：" + pack.Name))
                return false;
            token.ThrowIfCancellationRequested();
            queue.Enqueue("安装整合包 · " + pack.Name, root, pack.Id, new() { Kind = "pack", Content = pack, Java = preferences.Java });
            return true;
        }
        if (!versions.LocalVersions(root).Contains(key))
            throw new IOException("安装目标不存在或尚未安装完成，请刷新列表。");
        var descriptor = versions.Descriptor(root, key);
        var requirements = versions.Requirements(root, descriptor.LaunchProfileId);
        var directory = versions.InstanceDirectory(root, key, preferences.Launch);
        var instanceDirectory = directory;
        if (query.Kind == "datapack")
        {
            var validWorld = WorldArchives.List(directory).SingleOrDefault(item => item.Directory.Equals(world, StringComparison.OrdinalIgnoreCase))
                ?? throw new IOException("请选择目标实例中的有效世界。");
            directory = resources.ValidateWorld(directory, validWorld.Directory);
        }
        var selected = (await resources.Versions(project,
            new("", requirements.Minecraft, requirements.Loader, query.Kind), token))
            .SingleOrDefault(item => item.Id == version) ?? throw new IOException("所选资源版本与目标实例不兼容，请重新选择版本。");
        var fingerprint = await ContentUpdates.Fingerprint(directory, token);
        var plan = await resources.PrepareInstallation(selected, new(root, key, directory, requirements.Minecraft, requirements.Loader), query.Kind, gameRunning, token);
        ContentService.ValidateUpdateDependencies(directory, plan);
        var files = plan.Versions.Select(item => item.Files.FirstOrDefault(file => file.Primary) ?? item.Files.First());
        var bytes = files.Sum(file => file.Size);
        var summary = "目标：" + descriptor.DisplayName + "\n目录：" + directory + "\n下载：" + bytes.ToString("N0") + " 字节\n\n资源及必需依赖：\n"
            + string.Join("\n", plan.Versions.Select(item => item.Name + " · " + item.VersionNumber));
        if (!confirm(summary))
            return false;
        token.ThrowIfCancellationRequested();
        if (!string.Equals(root, preferences.Root, StringComparison.OrdinalIgnoreCase) || !versions.LocalVersions(root).Contains(key)
            || !Path.GetFullPath(instanceDirectory).Equals(Path.GetFullPath(versions.InstanceDirectory(root, key, preferences.Launch)), StringComparison.OrdinalIgnoreCase))
            throw new IOException("目标实例已变化，请重新预览。");
        queue.Enqueue("安装资源 · " + selected.Name + " · " + descriptor.DisplayName, root, key, new()
        {
            Kind = "content", Content = selected, PreparedPlan = plan, Destination = directory,
            GameVersion = requirements.Minecraft, Loader = query.Kind == "datapack" ? "datapack" : requirements.Loader,
            Java = (descriptor.Launch ?? preferences.Launch).Java, ContentFingerprint = fingerprint
        });
        return true;
    }
}
