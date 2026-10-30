namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    /// <summary>Bind application operations once; version page builders never receive the workspace.</summary>
    internal IReadOnlyDictionary<string, Action> CreateVersionSettingsPages(VersionSettingsPageContext context)
    {
        var key = context.Key;
        var window = context.View.Window;
        var tasks = new VersionTaskActions(() => LaunchCoordinator.busy, ResourceBrowserController.RequireStopped,
            (action, name) => LaunchCoordinator.Run(action, name), () => LaunchCoordinator.CurrentToken, () => LaunchCoordinator.http);
        var identity = new VersionIdentityEditor(context, tasks.IsBusy, tasks.RequireStopped, InstanceWorkspaceController.FeatureDialog);
        var overview = new VersionOverviewPage(context, new(
            tasks.IsBusy,
            () => InstanceLibraryController.OpenInstanceLibrary(key),
            LaunchCoordinator.OpenFolder,
            () => InstanceWorkspaceController.ExportScript(key),
            DownloadQueueController.EnqueueDownload,
            directory => PackUpgradeController.UpgradePack(key, directory, window),
            () => PackUpgradeController.ReturnToOriginal(key, window),
            () => UninstallController.UninstallInstance(key)), identity);
        var launch = new VersionLaunchPage(context, tasks, new(
            () => LaunchCoordinator.prefs.Launch,
            select => JavaManagerController.OpenJavaManager(context.Root, context.Descriptor.LaunchProfileId, select, window)));
        var loader = new VersionLoaderPage(context, tasks, new(
            DownloadQueueController.EnqueueDownload, () => BookmarkLibraryController.CloneInstanceDialog(key)));
        var mods = new VersionModPage(context, () => InstanceContentController.OpenSketchManagerCategory(key, "模组"));
        var export = new VersionExportPage(context, tasks, () => InstanceWorkspaceController.ExportScript(key));
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
