namespace BlockLaunch;
/// <summary>Composition root. Feature state belongs to the individual modules.</summary>
internal sealed partial class LauncherWorkspace
{
    internal MainWindow Window
    {
        get;
    }
    internal LauncherControls Controls
    {
        get;
    }
    internal AccountController AccountController
    {
        get;
    }
    internal AppearanceController AppearanceController
    {
        get;
    }
    internal AvatarController AvatarController
    {
        get;
    }
    internal CollectionsController CollectionsController
    {
        get;
    }
    internal CompactShellController CompactShellController
    {
        get;
    }
    internal ContentController ContentController
    {
        get;
    }
    internal DiagnosticsController DiagnosticsController
    {
        get;
    }
    internal DownloadNavigationController DownloadNavigationController
    {
        get;
    }
    internal DownloadPageController DownloadPageController
    {
        get;
    }
    internal DownloadQueueController DownloadQueueController
    {
        get;
    }
    internal DownloadTaskController DownloadTaskController
    {
        get;
    }
    internal DropFeedbackController DropFeedbackController
    {
        get;
    }
    internal ExperienceController ExperienceController
    {
        get;
    }
    internal ExperienceThemeController ExperienceThemeController
    {
        get;
    }
    internal HomeAccountController HomeAccountController
    {
        get;
    }
    internal InlineHostController InlineHostController
    {
        get;
    }
    internal InstanceLibraryController InstanceLibraryController
    {
        get;
    }
    internal InstancePagesController InstancePagesController
    {
        get;
    }
    internal JavaManagerController JavaManagerController
    {
        get;
    }
    internal KeyboardNavigationController KeyboardNavigationController
    {
        get;
    }
    internal LaunchCoordinator LaunchCoordinator
    {
        get;
    }
    internal LoaderController LoaderController
    {
        get;
    }
    internal LoaderOverviewController LoaderOverviewController
    {
        get;
    }
    internal MenuExtensionsController MenuExtensionsController
    {
        get;
    }
    internal MiniDownloadController MiniDownloadController
    {
        get;
    }
    internal ModernController ModernController
    {
        get;
    }
    internal MuaAccountController MuaAccountController
    {
        get;
    }
    internal NetworkController NetworkController
    {
        get;
    }
    internal PackController PackController
    {
        get;
    }
    internal PackUpgradeController PackUpgradeController
    {
        get;
    }
    internal ResourceFavoritesController ResourceFavoritesController
    {
        get;
    }
    internal ResourceFiltersController ResourceFiltersController
    {
        get;
    }
    internal ResourcePresentationController ResourcePresentationController
    {
        get;
    }
    internal ResourceSearchLayoutController ResourceSearchLayoutController
    {
        get;
    }
    internal ResourceWorkspaceController ResourceWorkspaceController
    {
        get;
    }
    internal SketchController SketchController
    {
        get;
    }
    internal StabilityController StabilityController
    {
        get;
    }
    internal UninstallController UninstallController
    {
        get;
    }
    internal UpdatesController UpdatesController
    {
        get;
    }
    internal VersionSettingsController VersionSettingsController
    {
        get;
    }
    internal WindowChromeController WindowChromeController
    {
        get;
    }
    internal LauncherWorkspace(MainWindow window, string configurationDirectory)
    {
        Window = window;
        Controls = new LauncherControls(window.HomePageView, window.InstallPageView, window.ResourcePageView, window.ContentManagementPageView, window.SettingsPageView, window.LogsPageView, window.TasksPageView, window.NavigationPageView, window.HeadingPageView);
        AccountController = new AccountController(this);
        AppearanceController = new AppearanceController(this);
        AvatarController = new AvatarController(this);
        CollectionsController = new CollectionsController(this);
        CompactShellController = new CompactShellController(this);
        ContentController = new ContentController(this);
        DiagnosticsController = new DiagnosticsController(this);
        DownloadNavigationController = new DownloadNavigationController(this);
        DownloadPageController = new DownloadPageController(this);
        DownloadQueueController = new DownloadQueueController(this);
        DownloadTaskController = new DownloadTaskController(this);
        DropFeedbackController = new DropFeedbackController(this);
        ExperienceController = new ExperienceController(this);
        ExperienceThemeController = new ExperienceThemeController(this);
        HomeAccountController = new HomeAccountController(this);
        InlineHostController = new InlineHostController(this);
        InstanceLibraryController = new InstanceLibraryController(this);
        InstancePagesController = new InstancePagesController(this);
        JavaManagerController = new JavaManagerController(this);
        KeyboardNavigationController = new KeyboardNavigationController(this);
        LaunchCoordinator = new LaunchCoordinator(this, configurationDirectory);
        LoaderController = new LoaderController(this);
        LoaderOverviewController = new LoaderOverviewController(this);
        MenuExtensionsController = new MenuExtensionsController(this);
        MiniDownloadController = new MiniDownloadController(this);
        ModernController = new ModernController(this);
        MuaAccountController = new MuaAccountController(this);
        NetworkController = new NetworkController(this);
        PackController = new PackController(this);
        PackUpgradeController = new PackUpgradeController(this);
        ResourceFavoritesController = new ResourceFavoritesController(this);
        ResourceFiltersController = new ResourceFiltersController(this);
        ResourcePresentationController = new ResourcePresentationController(this);
        ResourceSearchLayoutController = new ResourceSearchLayoutController(this);
        ResourceWorkspaceController = new ResourceWorkspaceController(this);
        SketchController = new SketchController(this);
        StabilityController = new StabilityController(this);
        UninstallController = new UninstallController(this);
        UpdatesController = new UpdatesController(this);
        VersionSettingsController = new VersionSettingsController(this);
        WindowChromeController = new WindowChromeController(this);
    }
}
