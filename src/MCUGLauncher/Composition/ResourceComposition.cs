namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    // Callbacks resolve modules after composition; constructors never read runtime state.
    ResourceFavoritesController CreateResourceFavorites() => new(Window,
        new(Controls.SearchQuery, Controls.SearchResults, Controls.SearchHint, Controls.SearchSummary),
        new(() => CollectionsController.bookmarks, () => PackController.ContentKind,
            () => CollectionsController.RefreshBookmarkDecoration(),
            ContentController.ProjectChanged, LaunchCoordinator.Log,
            () => ContentController.searchOffset = 0,
            () => ContentController.RunBrowse(ContentController.SearchContent),
            () => ResourceFiltersController.SearchTarget(),
            () => ResourceFiltersController.releaseChannel.SelectedIndex,
            () => ContentController.searchOffset, () => LaunchCoordinator.http));

    ResourceSearchLayoutController CreateResourceSearchLayout() => new(new(
        Window, Controls.SearchQuery, Controls.SearchResults,
        ResourceWorkspaceController.resourceWorkspaceTitle, ResourceFavoritesController, ResourceFiltersController,
        view => ResourceWorkspaceController.resourceFilterPanel = view,
        () => ContentController.searchOffset = 0,
        () => ContentController.RunBrowse(ContentController.SearchContent)));
}
