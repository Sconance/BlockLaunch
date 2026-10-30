using System.IO;

namespace BlockLaunch;

/// <summary>Coordinates destination selection and queue submission through explicit ports.</summary>
internal sealed class ResourceInstallationController(
    Func<bool> installToInstance, Func<ResourceInstallSelection> capture, Func<string> currentRoot,
    Action<ContentVersion> downloadFile, Func<string, string?> chooseWorld,
    Action<string, string, DownloadRequest> enqueue, Action<string> showError)
{
    internal void Install(ContentVersion version)
    {
        if (!installToInstance())
        {
            downloadFile(version);
            return;
        }
        try
        {
            var target = capture();
            var directory = target.Directory;
            if (target.Kind == "datapack")
            {
                var chosen = chooseWorld(Path.Combine(directory, "saves"));
                if (chosen == null)
                    return;
                var saves = Path.GetFullPath(Path.Combine(directory, "saves")) + Path.DirectorySeparatorChar;
                var world = Path.GetFullPath(chosen);
                if (!world.StartsWith(saves, StringComparison.OrdinalIgnoreCase) || !File.Exists(Path.Combine(world, "level.dat")))
                    throw new IOException("请选择目标实例 saves 内含 level.dat 的有效存档。");
                directory = world;
            }
            if (!target.Root.Equals(currentRoot(), StringComparison.OrdinalIgnoreCase))
                throw new IOException("游戏目录已变化，请重新选择安装目标。");
            var request = ResourceInstallPlanner.Create(version, target.Requirements, target.Kind, directory);
            enqueue("安装资源 · " + version.Name, target.Key, request);
        }
        catch (Exception ex) { showError(ex.Message); }
    }
}
