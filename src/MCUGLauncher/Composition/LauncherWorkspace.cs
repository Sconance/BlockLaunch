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
    internal BookmarkLibraryController BookmarkLibraryController
    {
        get;
    }
    internal CompactShellController CompactShellController
    {
        get;
    }
    internal ResourceBrowserController ResourceBrowserController
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
    internal InstanceWorkspaceController InstanceWorkspaceController
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
    internal LoaderSelectionController LoaderSelectionController
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
    internal DownloadMiniBarController DownloadMiniBarController
    {
        get;
    }
    internal ModernShellController ModernShellController
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
    internal ModpackController ModpackController
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
    internal InstanceContentController InstanceContentController
    {
        get;
    }
    internal TaskRecoveryController TaskRecoveryController
    {
        get;
    }
    internal UninstallController UninstallController
    {
        get;
    }
    internal ContentUpdatesController ContentUpdatesController
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
        BookmarkLibraryController = new BookmarkLibraryController(this);
        CompactShellController = new CompactShellController(this);
        ResourceBrowserController = new ResourceBrowserController(this);
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
        InstanceWorkspaceController = new InstanceWorkspaceController(this);
        JavaManagerController = new JavaManagerController(this);
        LaunchCoordinator = new LaunchCoordinator(this, configurationDirectory);
        LoaderSelectionController = new LoaderSelectionController(this);
        MenuExtensionsController = new MenuExtensionsController(this);
        DownloadMiniBarController = new DownloadMiniBarController(this);
        ModernShellController = new ModernShellController(this);
        MuaAccountController = new MuaAccountController(this);
        NetworkController = new NetworkController(() => LaunchCoordinator.prefs.Network, value =>
        {
            LaunchCoordinator.prefs.Network = value;
            LaunchCoordinator.settings.Save(LaunchCoordinator.prefs);
        }, (title, content) => InstanceWorkspaceController.FeatureDialog(title, content));
        ModpackController = new ModpackController(this);
        PackUpgradeController = new PackUpgradeController(this);
        ResourceFavoritesController = CreateResourceFavorites();
        ResourceFiltersController = new ResourceFiltersController(this);
        ResourcePresentationController = new ResourcePresentationController(this);
        ResourceWorkspaceController = new ResourceWorkspaceController(this);
        ResourceSearchLayoutController = CreateResourceSearchLayout();
        InstanceContentController = new InstanceContentController(this);
        TaskRecoveryController = new TaskRecoveryController(this);
        UninstallController = new UninstallController(this);
        ContentUpdatesController = new ContentUpdatesController(new(
            window, () => LaunchCoordinator.busy, () => LaunchCoordinator.http, () => LaunchCoordinator.CurrentToken,
            (action, name) => LaunchCoordinator.Run(action, name), ResourceBrowserController.RequireStopped,
            InstanceWorkspaceController.FeatureDialog, DownloadQueueController.EnqueueDownload,
            ResourceBrowserController.Target, ResourceBrowserController.ManagedDirectory, ResourceBrowserController.RefreshLocalContent, LaunchCoordinator.Log));
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
        KeyboardNavigationController = new KeyboardNavigationController(window, () =>
            new LauncherTooltipInitializer(Controls.HomeNav, CompactShellController.downloadNav, Controls.SearchQuery, DownloadPageController.vanillaQuery));
        LoaderOverviewController = new LoaderOverviewController(window, () => LaunchCoordinator.http,
            () => (LaunchCoordinator.rows, LaunchCoordinator.downloadVersion), selection =>
            {
                try
                {
                    ResourceBrowserController.RequireStopped();
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
