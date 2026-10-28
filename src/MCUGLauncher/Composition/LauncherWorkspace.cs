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
        InlineHostController = new InlineHostController(this);
        InstanceLibraryController = new InstanceLibraryController(this);
        InstancePagesController = new InstancePagesController(this);
        JavaManagerController = new JavaManagerController(this);
        LaunchCoordinator = new LaunchCoordinator(this, configurationDirectory);
        LoaderController = new LoaderController(this);
        MenuExtensionsController = new MenuExtensionsController(this);
        MiniDownloadController = new MiniDownloadController(this);
        ModernController = new ModernController(this);
        MuaAccountController = new MuaAccountController(this);
        NetworkController = new NetworkController(() => LaunchCoordinator.prefs.Network, value =>
        {
            LaunchCoordinator.prefs.Network = value;
            LaunchCoordinator.settings.Save(LaunchCoordinator.prefs);
        }, (title, content) => InstancePagesController.FeatureDialog(title, content));
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
        WindowChromeController = new WindowChromeController(window);
        AvatarController = new AvatarController(Controls.SkinAvatar, Controls.AvatarCaption, Controls.AvatarPlayerName,
            () => new PlayerIdentityState(Controls.AccountMode.SelectedIndex, MuaAccountController.muaAccount,
                MuaAccountController.muaAccounts, AccountController.savedAccount, LaunchCoordinator.online?.Username,
                Controls.PlayerName.Text, LaunchCoordinator.prefs.OfflineSkin, LaunchCoordinator.settingsDir, LaunchCoordinator.http),
            () => LaunchCoordinator.busy,
            () => LaunchCoordinator.Run(() => AccountController.MicrosoftLogin(false), "刷新玩家档案与头像"));
        HomeAccountController = new HomeAccountController(
            new(Controls.AccountMode, Controls.PlayerName, Controls.AvatarPlayerName, Controls.AccountStatus,
                (System.Windows.Controls.Border)Controls.SkinAvatar.Parent, (System.Windows.Style)window.FindResource("AccountLoginChip")),
            new(() => AvatarController.RefreshAvatar(true),
                () => DownloadTaskController.RunAccountAction(LaunchCoordinator.Login, "微软正版登录"),
                () => DownloadTaskController.RunAccountAction(MuaAccountController.MuaLogin, "MUA 登录"),
                () => AccountController.AccountManagementClick(window, new System.Windows.RoutedEventArgs()),
                () => MuaAccountController.MuaManagementClick(window, new System.Windows.RoutedEventArgs())),
            () => (AccountController.savedAccount != null || LaunchCoordinator.online != null, MuaAccountController.muaAccount != null),
            card => HomeWelcomePresenter.Configure((System.Windows.Controls.Grid)((System.Windows.Controls.TabItem)window.MainTabs.Items[0]).Content,
                card, LaunchCoordinator.prefs.BackgroundPath));
        KeyboardNavigationController = new KeyboardNavigationController(window, () => new (System.Windows.FrameworkElement, string)[]
        {
            (Controls.HomeNav, "启动首页"), (CompactShellController.downloadNav, "下载"),
            (Controls.SearchQuery, "搜索资源"), (DownloadPageController.vanillaQuery, "搜索游戏版本")
        });
        LoaderOverviewController = new LoaderOverviewController(window, () => LaunchCoordinator.http,
            () => (LaunchCoordinator.rows, LaunchCoordinator.downloadVersion), selection =>
            {
                try
                {
                    ContentController.RequireStopped();
                    DownloadQueueController.EnqueueDownload(selection.Loader + " · " + selection.Minecraft, selection.Minecraft,
                        new()
                        {
                            Version = selection.Minecraft,
                            Loader = selection.Loader,
                            LoaderVersion = selection.Version
                        });
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            });
    }
}
