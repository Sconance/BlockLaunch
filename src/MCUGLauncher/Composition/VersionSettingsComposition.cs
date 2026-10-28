namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    /// <summary>Bind application operations once; version page builders never receive the workspace.</summary>
    internal IReadOnlyDictionary<string, Action> CreateVersionSettingsPages(VersionSettingsPageContext context)
    {
        var key = context.Key;
        var window = context.View.Window;
        var tasks = new VersionTaskActions(() => LaunchCoordinator.busy, ContentController.RequireStopped,
            (action, name) => LaunchCoordinator.Run(action, name), () => LaunchCoordinator.CurrentToken, () => LaunchCoordinator.http);
        var identity = new VersionIdentityEditor(context, tasks.IsBusy, tasks.RequireStopped, InstancePagesController.FeatureDialog);
        var overview = new VersionOverviewPage(context, new(
            tasks.IsBusy,
            () => InstanceLibraryController.OpenInstanceLibrary(key),
            LaunchCoordinator.OpenFolder,
            () => InstancePagesController.ExportScript(key),
            DownloadQueueController.EnqueueDownload,
            directory => PackUpgradeController.UpgradePack(key, directory, window),
            () => PackUpgradeController.ReturnToOriginal(key, window),
            () => UninstallController.UninstallInstance(key)), identity);
        var launch = new VersionLaunchPage(context, tasks, new(
            () => LaunchCoordinator.prefs.Launch,
            select => JavaManagerController.OpenJavaManager(context.Root, context.Descriptor.LaunchProfileId, select, window)));
        var loader = new VersionLoaderPage(context, tasks, new(
            DownloadQueueController.EnqueueDownload, () => CollectionsController.CloneInstanceDialog(key)));
        var mods = new VersionModPage(context, tasks, new(
            directory => ExperienceController.ShowModScan(key, directory, window),
            directory => UpdatesController.OpenContentUpdates(directory, key, context.Requirements.Minecraft, context.Requirements.Loader, owner: window),
            UpdatesController.RestoreContent,
            () => LaunchCoordinator.LocalVersions().Contains(key),
            () =>
            {
                Controls.Versions.SelectedValue = key;
                Controls.ContentType.SelectedIndex = 0;
                window.Close();
                Controls.DiscoverNav.IsChecked = true;
            }));
        var export = new VersionExportPage(context, tasks, () => InstancePagesController.ExportScript(key));
        return new Dictionary<string, Action>
        {
            ["概览"] = overview.Build,
            ["设置"] = launch.Build,
            ["修改"] = loader.Build,
            ["Mod 管理"] = mods.Build,
            ["导出"] = export.Build
        };
    }
}
