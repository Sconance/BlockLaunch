using System.IO;
using System.Windows;

namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    UIElement CreateInstanceModsPage(string key, InlineDialog owner)
    {
        var catalog = new InstanceCatalog(LaunchCoordinator.prefs.Root);
        var descriptor = catalog.GetOrCreate(key);
        var directory = (descriptor.Launch ?? LaunchCoordinator.prefs.Launch).Isolation == "共享根目录"
            ? LaunchCoordinator.prefs.Root : catalog.DirectoryFor(key);
        var requirements = InstanceWorkspaceController.RequirementsFor(descriptor.LaunchProfileId);
        var tasks = new VersionTaskActions(() => LaunchCoordinator.busy, ResourceBrowserController.RequireStopped,
            (action, name) => LaunchCoordinator.Run(action, name), () => LaunchCoordinator.CurrentToken, () => LaunchCoordinator.http);
        return new InstanceModManagementPage(directory, requirements, owner, tasks, new(
            path => ExperienceController.ShowModScan(key, path, owner),
            path => ContentUpdatesController.OpenContentUpdates(path, key, requirements.Minecraft, requirements.Loader, owner: owner),
            ContentUpdatesController.RestoreContent, () => LaunchCoordinator.LocalVersions().Contains(key),
            () =>
            {
                Controls.Versions.SelectedValue = key;
                owner.Close();
                CompactShellController.NavigateDownloads("Mod");
            }, async item =>
            {
                if (item.Managed == null)
                {
                    InlineMessageBox.Show(owner, "请选择通过 Modrinth 安装的内容。");
                    return;
                }
                await ContentUpdatesController.OpenContentUpdates(directory, key, requirements.Minecraft,
                    requirements.Loader, item.Managed.ProjectId, owner);
            }, () => LaunchCoordinator.OpenFolder(Path.Combine(directory, "mods")),
            () => LaunchCoordinator.OpenFolder(Path.Combine(directory, ".blocklaunch", "backups")))).Build();
    }
}
