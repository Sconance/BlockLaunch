namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
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
