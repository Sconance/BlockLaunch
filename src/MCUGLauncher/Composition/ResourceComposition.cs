namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    internal ResourceInstallationController ResourceInstallationController { get; private set; } = null!;

    ResourceInstallationController CreateResourceInstallation() => new(
        () => ResourceWorkspaceController.downloadDestination?.InstallToInstance == true,
        () =>
        {
            var target = ResourceBrowserController.ResourceDownloadTarget();
            if (!LaunchCoordinator.LocalVersions().Contains(target.Version))
                throw new System.IO.IOException("所选实例尚未安装完成。");
            var catalog = new InstanceCatalog(LaunchCoordinator.prefs.Root);
            var descriptor = catalog.GetOrCreate(target.Version);
            var directory = (descriptor.Launch ?? LaunchCoordinator.prefs.Launch).Isolation == "共享根目录"
                ? LaunchCoordinator.prefs.Root : catalog.DirectoryFor(target.Version);
            return new ResourceInstallSelection(LaunchCoordinator.prefs.Root, target.Version, directory,
                new(target.GameVersion, target.Loader, ""), ModpackController.ContentKind);
        }, () => LaunchCoordinator.prefs.Root, DownloadQueueController.QueueContent, saves =>
        {
            var picker = new OpenFolderDialog { Title = "选择目标实例内的存档", InitialDirectory = saves };
            return picker.ShowDialog(Window) == true ? picker.FolderName : null;
        }, DownloadQueueController.EnqueueDownload,
        message => InlineMessageBox.Show(Window, message, "无法安装资源"));

    // Callbacks resolve modules after composition; constructors never read runtime state.
    ResourceFavoritesController CreateResourceFavorites() => new(Window,
        new(Controls.SearchQuery, Controls.SearchResults, Controls.SearchHint, Controls.SearchSummary),
        new(() => BookmarkLibraryController.bookmarks, () => ModpackController.ContentKind,
            () => BookmarkLibraryController.RefreshBookmarkDecoration(),
            ResourceBrowserController.ProjectChanged, LaunchCoordinator.Log,
            () => ResourceBrowserController.searchOffset = 0,
            () => ResourceBrowserController.RunBrowse(ResourceBrowserController.SearchContent),
            () => ResourceFiltersController.SearchTarget(),
            () => ResourceFiltersController.releaseChannel.SelectedIndex,
            () => ResourceBrowserController.searchOffset, () => LaunchCoordinator.http));

    ResourceSearchLayoutController CreateResourceSearchLayout() => new(new(
        Window, Controls.SearchQuery, Controls.SearchResults,
        ResourceWorkspaceController.resourceWorkspaceTitle, ResourceFavoritesController, ResourceFiltersController,
        view => ResourceWorkspaceController.resourceFilterPanel = view,
        () => ResourceBrowserController.searchOffset = 0,
        () => ResourceBrowserController.RunBrowse(ResourceBrowserController.SearchContent)));
}
