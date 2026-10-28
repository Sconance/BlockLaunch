namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    internal LaunchOptionsDialog CreateLaunchOptionsDialog() => new(new(
        () => LaunchCoordinator.busy,
        () => LaunchCoordinator.prefs,
        () => LaunchCoordinator.settings.Save(LaunchCoordinator.prefs),
        ContentController.RequireStopped,
        InstancePagesController.FeatureDialog,
        () =>
        {
            Controls.Memory.Text = LaunchCoordinator.prefs.Ram.ToString();
            Controls.JavaPath.Text = LaunchCoordinator.prefs.Java;
            Controls.GameWidth.Text = LaunchCoordinator.prefs.Width.ToString();
            Controls.GameHeight.Text = LaunchCoordinator.prefs.Height.ToString();
        }), LaunchCoordinator.settingsDir, InstancePagesController.ApplyPersonalization, AppearanceController.OpenAppearance);

    internal InstancePackExportDialog CreateInstancePackExportDialog() => new(new(
        () => LaunchCoordinator.busy,
        () => LaunchCoordinator.prefs.Root,
        () => LaunchCoordinator.prefs.Launch,
        InstancePagesController.FeatureDialog,
        (action, name) => LaunchCoordinator.Run(action, name),
        ContentController.RequireStopped,
        InstancePagesController.RequirementsFor,
        () => LaunchCoordinator.CurrentToken,
        LaunchCoordinator.Log));

    internal SketchContentManagerDialog CreateSketchContentManagerDialog(Action showSketchVersions) => new(Window,
        new(() => LaunchCoordinator.prefs.Root,
            () => LaunchCoordinator.prefs.GameRoots,
            () => LaunchCoordinator.prefs.Launch,
            () => (InstancePagesController.instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection.Cast<InstanceView>()),
        new(() => !LaunchCoordinator.busy || DownloadTaskController.DownloadInProgress,
            () => LaunchCoordinator.busy,
            InstancePagesController.RefreshInstanceViews,
            e => PackController.ImportPackClick(Window, e),
            VersionSettingsController.OpenVersionSettings,
            VersionSettingsController.OpenVersionSection,
            UninstallController.UninstallInstance,
            LaunchCoordinator.OpenFolder,
            ExperienceController.OpenServers,
            key => InstanceLibraryController.OpenInstanceLibrary(key),
            InstancePagesController.SwitchRoot,
            (panel, page, getKey) => MenuExtensionsController.AttachExtensionButtons(LauncherMenuArea.InstanceContent,
                panel, lifetime: page, instanceIdProvider: getKey)),
        new(key => DropFeedbackController.sketchManagerKey = key,
            refresh => DropFeedbackController.refreshSketchManager = refresh,
            refresh => InstancePagesController.InstanceViewsChanged += refresh,
            refresh => InstancePagesController.InstanceViewsChanged -= refresh),
        showSketchVersions);
}
