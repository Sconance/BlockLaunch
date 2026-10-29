using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core;
using CmlLib.Core.Auth;

namespace BlockLaunch;

public partial class MainWindow : Window
{
    readonly string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory);
    internal HttpClient http = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };
    internal Preferences prefs = new();
    NetworkSettings runtimeNetwork = new();
    internal bool IncludeOptionalPackContent => PackOptionalCheckBox.IsChecked == true;
    MinecraftLauncher launcher = null!;
    List<VersionRow> rows = new();
    internal string? downloadVersion;
    internal MSession? online;
    Process? game;
    internal bool busy;
    bool closeAfterTask;
    bool xamlReady;
    LoaderInstallationController loaderController = null!;
    FileImportController fileImportController = null!;
    GameWindowBehaviorController gameWindowBehaviorController = null!;
    GameLaunchController gameLaunchController = null!;
    DownloadTaskExecutor downloadTaskExecutor = null!;
    DownloadQueueService downloadQueue = null!;
    DownloadFrontendService downloadFrontend = null!;
    internal LauncherSettingsService launcherSettings = null!;
    internal AccountSessionService accountSessions = null!;
    internal GameSessionMonitor gameSessionMonitor = new();
    internal VersionManagementService versionManagement = null!;
    internal ResourceManagementService resourceManagement = null!;
    FeatureDialogBuilder featureDialogBuilder = null!;
    LaunchOptionsDialog launchOptionsDialog = null!;
    PlayerIdentityController playerIdentityController = null!;
    InstanceSearchController instanceSearchController = null!;
    JavaManagerController javaManagerController = null!;
    PageNavigationController pageNavigationController = null!;
    WindowThemeStyler windowThemeStyler = null!;
    ContentUpdateController contentUpdateController = null!;
    InstanceUninstallationController instanceUninstallationController = null!;
    PackUpgradeController packUpgradeController = null!;
    internal Border? dropFeedback;
    internal string? sketchManagerKey;
    internal Action? refreshSketchManager;
    internal SettingsStore settings = null!;
    internal OperationCoordinator operations = null!;
    GameSession? gameSession;
    string? lastGameDirectory;

    internal CancellationToken CurrentToken => operations?.Current?.Token ?? CancellationToken.None;

    DateTime lastProgress;

    void NetworkClick(object sender, RoutedEventArgs e) => new NetworkSettingsController(this).Open();
    internal void ConfigureUtilityWindow(Window window) => UtilityWindowInitializer.Configure(window, () => windowThemeStyler.Apply(window));
    internal void ConfigureUtilityWindow(InlineDialog page) => UtilityWindowInitializer.Configure(page, () => windowThemeStyler.Apply(this));
    internal InlineDialog FeatureDialog(string title, StackPanel content) => featureDialogBuilder.Build(title, content);
    internal void ThemeWindow(Window window) => windowThemeStyler.Apply(window);
    internal int AccountModeIndex
    {
        get => AccountModeControl.SelectedIndex;
        set => AccountModeControl.SelectedIndex = value;
    }

    internal void InitializeAccountControls(int initialMode, Action accountModeChanged, Action playerNameChanged, RoutedEventHandler manageAccount)
    {
        AccountModeControl.SelectedIndex = initialMode;
        AccountModeControl.SelectionChanged += (_, _) => accountModeChanged();
        PlayerNameControl.TextChanged += (_, _) => playerNameChanged();
        ((StackPanel)AccountModeControl.Parent).Children.Add(ActionButton("管理正版账号", manageAccount));
    }

    internal void SetAccountStatusText(string text) => AccountStatusControl.Text = text;

    T FindXamlControl<T>(string name) where T : FrameworkElement =>
        FindName(name) as T ?? throw new InvalidOperationException($"XAML control '{name}' is missing or is not a {typeof(T).Name}.");

    internal ComboBox AccountModeControl => FindXamlControl<ComboBox>("AccountMode");
    internal Image SkinAvatarControl => FindXamlControl<Image>("SkinAvatar");
    internal TextBox PlayerNameControl => FindXamlControl<TextBox>("PlayerName");
    internal TextBlock AvatarPlayerNameControl => FindXamlControl<TextBlock>("AvatarPlayerName");
    internal TextBlock AccountStatusControl => FindXamlControl<TextBlock>("AccountStatus");
    internal TextBlock AvatarCaptionLabel => FindXamlControl<TextBlock>("AvatarCaption");
    internal TextBox JavaPathInput => FindXamlControl<TextBox>("JavaPath");
    internal TextBox MemoryInput => FindXamlControl<TextBox>("Memory");
    internal TextBox GameWidthInput => FindXamlControl<TextBox>("GameWidth");
    internal TextBox GameHeightInput => FindXamlControl<TextBox>("GameHeight");
    internal CheckBox SnapshotFilterCheckBox => FindXamlControl<CheckBox>("Snapshots");
    internal CheckBox PackOptionalCheckBox => FindXamlControl<CheckBox>("PackOptional");
    internal TextBox LogTextBox => FindXamlControl<TextBox>("LogBox");

    internal RadioButton ContentDiscoverNavigation => FindXamlControl<RadioButton>("DiscoverNav");
    internal RadioButton ContentManageNavigation => FindXamlControl<RadioButton>("ManageNav");
    internal RadioButton HomeNavigation => FindXamlControl<RadioButton>("HomeNav");
    internal TextBlock PageTitleLabel => FindXamlControl<TextBlock>("PageTitle");
    internal TextBlock PageSubtitleLabel => FindXamlControl<TextBlock>("PageSubtitle");
    internal TabControl MainTabControl => FindXamlControl<TabControl>("MainTabs");
    internal ComboBox VersionSelectorControl => FindXamlControl<ComboBox>("Versions");
    internal Button LaunchGameButtonControl => FindXamlControl<Button>("LaunchGameButton");
    internal TextBlock InstalledVersionsHintLabel => FindXamlControl<TextBlock>("InstalledVersionsHint");
    internal TextBox GameRootInput => FindXamlControl<TextBox>("GameRoot");
    internal WrapPanel InstallActionsPanel => FindXamlControl<WrapPanel>("InstallActions");
    internal WrapPanel LoaderActionsPanel => FindXamlControl<WrapPanel>("LoaderActions");
    internal ComboBox ContentKindSelector => FindXamlControl<ComboBox>("ContentType");
    internal TextBlock ContentTargetLabel => FindXamlControl<TextBlock>("ContentTarget");
    internal TextBlock LocalVersionCountLabel => FindXamlControl<TextBlock>("LocalCount");
    internal TextBlock InstallTargetLabel => FindXamlControl<TextBlock>("InstallTarget");
    internal TextBlock ManageTargetLabel => FindXamlControl<TextBlock>("ManageTarget");
    internal ListBox ContentSearchResultsControl => FindXamlControl<ListBox>("SearchResults");
    internal TextBlock ContentSearchHintControl => FindXamlControl<TextBlock>("SearchHint");
    internal TextBox ContentSearchQueryInput => FindXamlControl<TextBox>("SearchQuery");
    internal TextBlock ContentProjectTitleLabel => FindXamlControl<TextBlock>("ProjectTitle");
    internal TextBlock ContentProjectDescriptionLabel => FindXamlControl<TextBlock>("ProjectDescription");
    internal ComboBox ContentProjectVersionsControl => FindXamlControl<ComboBox>("ProjectVersions");
    internal Button ContentInstallButtonControl => FindXamlControl<Button>("InstallContentButton");
    internal TextBlock ContentSearchSummaryLabel => FindXamlControl<TextBlock>("SearchSummary");
    internal TextBlock OperationStatusLabel => FindXamlControl<TextBlock>("Status");
    internal ListBox InstalledContentListControl => FindXamlControl<ListBox>("InstalledContent");
    internal Grid TaskStatusBarControl => FindXamlControl<Grid>("TaskStatusBar");
    internal ProgressBar TaskProgressBarControl => FindXamlControl<ProgressBar>("Progress");
    internal Button CancelTaskButtonControl => FindXamlControl<Button>("CancelTaskButton");
    internal ListBox TaskHistoryListControl => FindXamlControl<ListBox>("TaskHistoryList");
    internal Button StopGameButtonControl => FindXamlControl<Button>("StopGameButton");
    internal TextBox GameLogViewControl => FindXamlControl<TextBox>("GameLogView");

    async void RefreshAvatarClick(object sender, RoutedEventArgs e)
    {
        if (busy)
            return;
        if (AccountModeControl.SelectedIndex == 1 && savedAccount != null)
            await Run(() => MicrosoftLogin(false), "刷新玩家档案与头像");
        playerIdentityController.Refresh(true);
    }

    string? ConfirmPackImport(PackOverview overview, string root) => fileImportController.ConfirmPackImport(overview, root);
    string DropTargetDirectory() => fileImportController.DropTargetDirectory();
    internal void OpenJavaManager(string root, string profile, Action<string> select, object? owner = null) => javaManagerController.Open(root, profile, select, owner);
    PlayerIdentityState GetPlayerIdentityState() => new(AccountModeControl.SelectedIndex, muaAccount, muaAccounts, savedAccount, online?.Username, PlayerNameControl.Text, prefs.OfflineSkin, settingsDir, http);

    public MainWindow() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory))
    {
    }

    public MainWindow(string configurationDirectory)
    {
        settingsDir = Path.GetFullPath(configurationDirectory);
        // Load the compiled XAML without referencing its generated InitializeComponent method.
        Application.LoadComponent(this, new Uri("/MCUGLauncher;component/mainwindow.xaml", UriKind.Relative));
        xamlReady = true;
        inlinePageHost = new InlinePageHost(this, inlinePages, HomeNavigation, MainTabControl);
        gameWindowBehaviorController = new GameWindowBehaviorController(this, Log);
        gameLaunchController = new GameLaunchController(this);
        windowThemeStyler = new WindowThemeStyler(this);
        featureDialogBuilder = new FeatureDialogBuilder(this);
        launchOptionsDialog = new LaunchOptionsDialog(this, settingsDir, ApplyPersonalization, OpenAppearance);
        instanceUninstallationController = new InstanceUninstallationController(this);
        packUpgradeController = new PackUpgradeController(
            this,
            key => VersionSelectorControl.SelectedValue = key,
            () => HomeNavigation.IsChecked = true);
        loaderController = new LoaderInstallationController(this);
        fileImportController = new FileImportController(this);
        playerIdentityController = new PlayerIdentityController(PlayerNameControl, SkinAvatarControl, AvatarCaptionLabel, AvatarPlayerNameControl, GetPlayerIdentityState);
        instanceSearchController = new InstanceSearchController(instanceList, instanceQuery, instanceSearchSummary);
        Directory.CreateDirectory(settingsDir);
        settings = new SettingsStore(settingsDir);
        launcherSettings = new LauncherSettingsService(settings, () => game is { HasExited: false });
        javaManagerController = new JavaManagerController(this);
        pageNavigationController = new PageNavigationController(
            MainTabControl,
            PageTitleLabel,
            PageSubtitleLabel,
            () => compactShellReady,
            () => ContentKind,
            () => downloadCategory,
            NavigateDownloads,
            OpenVersionSettings,
            Selected,
            RestoreResourcePage,
            SelectDownloadCategory,
            RefreshLocalContent,
            RefreshOperations);
        try
        {
            prefs = settings.Load();
        }
        catch (Exception ex)
        {
            ShowStartupProblem(ex.Message);
            return;
        }

        try
        {
            queueLease = new FileStream(Path.Combine(settingsDir, "download-queue.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            ShowStartupProblem("此配置目录已有启动器运行，请使用原窗口。");
            return;
        }

        operations = new OperationCoordinator(Path.Combine(settingsDir, "tasks"));
        downloadQueue = new DownloadQueueService(operations, new DownloadPauseStore(settings, () => prefs));
        downloadFrontend = new DownloadFrontendService(downloadQueue);
        operations.Changed += record => Dispatcher.BeginInvoke(() =>
        {
            ApplyOperation(record);
            RefreshMiniDownloads(true);
            if (MainTabControl.SelectedIndex == 6)
                RefreshOperations();
        });
        GameRootInput.Text = prefs.Root;
        JavaPathInput.Text = prefs.Java;
        PlayerNameControl.Text = prefs.Player;
        MemoryInput.Text = prefs.Ram.ToString();
        GameWidthInput.Text = prefs.Width.ToString();
        GameHeightInput.Text = prefs.Height.ToString();
        prefs.Network ??= new();
        runtimeNetwork = JsonSerializer.Deserialize<NetworkSettings>(JsonSerializer.Serialize(prefs.Network))!;
        http.Dispose();
        http = NetworkPolicyHandler.Create(runtimeNetwork);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MCUGLauncher/0.21.0 (Windows-desktop-launcher)");
        versionManagement = new VersionManagementService(http);
        resourceManagement = new ResourceManagementService(http);
        contentUpdateController = new ContentUpdateController(resourceManagement, this);
        downloadTaskExecutor = new DownloadTaskExecutor(new(
            http,
            runtimeNetwork,
            ProtectDownloadWorlds,
            () => prefs.Launch,
            root => Path.GetFullPath(prefs.Root).Equals(root, StringComparison.OrdinalIgnoreCase),
            RefreshAfterDownload,
            Log));
        SetupLauncher();
        BuildFeaturePages();
        ApplyPersonalization();
        BuildAccountUI();
        BuildDiagnosticsUI();
        BuildCompactShell();
        BuildHomeAccountPanel();
        BuildMiniDownloads();
        BuildExperienceUI();
        BuildInlineHost();
        new WindowChromeBuilder(this).Build();
        new LauncherTooltipInitializer(HomeNavigation, downloadNav, ContentSearchQueryInput, vanillaQuery).Build();
        BuildSketchUI();
        BuildModernUI();
        BuildMenuExtensionHosts();
        Loaded += async (_, _) =>
        {
            EnsureOutputTimer();
            if (settings.Notice != null)
                Log(settings.Notice);
            var loginRequested = Environment.GetCommandLineArgs().Contains("--login");
            if (loginRequested)
                await Run(Login, "微软正版登录");
            if (closeAfterTask)
                return;
            await Run(StartupRecovery, "启动恢复");
            if (!loginRequested)
                await HandleScriptLaunch();
            StartDownloadQueue();
        };
        Closing += (_, e) =>
        {
            if (busy)
            {
                e.Cancel = true;
                closeAfterTask = true;
                if (DownloadInProgress)
                    downloadQueue.SuspendForShutdown();
                else
                    operations.Cancel();
                OperationStatusLabel.Text = "正在清理当前任务，完成后自动关闭…";
            }
        };
        Closed += (_, _) => browseCancellation?.Cancel();
        Closed += (_, _) => downloadTasksView?.Dispose();
    }

    internal void SetupLauncher()
    {
        downloadVersion = null;
        Directory.CreateDirectory(prefs.Root);
        launcher = new MinecraftLauncher(MinecraftLauncherParameters.CreateDefault(new MinecraftPath(prefs.Root), http));
        launcher.FileProgressChanged += (_, a) =>
        {
            var owner = operations.Current;
            Dispatcher.BeginInvoke(() =>
            {
                if (owner == null || operations.Current != owner)
                    return;
                if ((DateTime.UtcNow - lastProgress).TotalMilliseconds < 150)
                    return;
                lastProgress = DateTime.UtcNow;
                owner.Report($"{a.EventType} · {a.Name} · {a.ProgressedTasks}/{a.TotalTasks}", a.TotalTasks > 0 ? a.ProgressedTasks * 100.0 / a.TotalTasks : null);
            });
        };
    }

    internal async Task Run(Func<Task> action, string? name = null)
    {
        if (busy)
        {
            InlineMessageBox.Show("请等待当前任务完成。", Brand.Name);
            return;
        }

        TaskStatusBarControl.Visibility = Visibility.Visible;
        busy = true;
        TaskProgressBarControl.IsIndeterminate = true;
        SetInputs(false);
        try
        {
            var result = await operations.Run(name ?? OperationName(action.Method.Name), prefs.Root, VersionSelectorControl.SelectedValue as string ?? "", async context =>
            {
                context.Report("正在执行…");
                await action();
            });
            OperationStatusLabel.Text = result.State == OperationState.Cancelled ? "已取消，可以重新执行原操作。" : "就绪";
        }
        catch (Exception ex)
        {
            OperationStatusLabel.Text = "操作失败，详情见运行日志";
            Log(ex.GetType().Name + ": " + ex.Message);
            InlineMessageBox.Show(ex.Message, "操作失败");
        }
        finally
        {
            TaskStatusBarControl.Visibility = Visibility.Collapsed;
            busy = false;
            TaskProgressBarControl.IsIndeterminate = false;
            TaskProgressBarControl.Value = 0;
            SetInputs(true);
            CancelTaskButtonControl.IsEnabled = false;
            RefreshOperations();
            if (closeAfterTask)
                Close();
        }
    }

    void SetInputs(bool enabled)
    {
        LaunchGameButtonControl.IsEnabled = enabled && VersionSelectorControl.SelectedItem != null;
        StopGameButtonControl.Visibility = game is
        {
            HasExited: false
        } ? Visibility.Visible : Visibility.Collapsed;
        LaunchGameButtonControl.ToolTip = DownloadInProgress ? "请先暂停下载，再启动游戏，避免安装文件与游戏进程同时使用。" : "启动当前选择的已安装版本";
        System.Windows.Controls.ToolTipService.SetShowOnDisabled(LaunchGameButtonControl, true);
        VersionSelectorControl.IsEnabled = enabled || DownloadInProgress;
        SnapshotFilterCheckBox.IsEnabled = enabled;
        AccountModeControl.IsEnabled = enabled || DownloadInProgress;
        PlayerNameControl.IsEnabled = enabled || DownloadInProgress;
        GameRootInput.IsEnabled = enabled;
        JavaPathInput.IsEnabled = enabled;
        MemoryInput.IsEnabled = enabled;
        GameWidthInput.IsEnabled = enabled;
        GameHeightInput.IsEnabled = enabled;
        var browse = enabled || operations.Current?.Record.Download != null;
        ContentSearchQueryInput.IsEnabled = browse;
        ContentKindSelector.IsEnabled = browse;
        ContentSearchResultsControl.IsEnabled = browse;
        ContentProjectVersionsControl.IsEnabled = browse;
        InstalledContentListControl.IsEnabled = enabled;
        PackOptionalCheckBox.IsEnabled = enabled;
    }

    internal void Log(string text)
    {
        // Never write command lines or authentication payloads: they can contain access tokens.
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}\n";
        void Append()
        {
            LogTextBox.AppendText(LogPrivacy.Redact(line));
            if (LogTextBox.Text.Length > 150000)
                LogTextBox.Text = LogTextBox.Text[^100000..];
            LogTextBox.ScrollToEnd();
        }

        if (Dispatcher.CheckAccess())
            Append();
        else
            Dispatcher.BeginInvoke(Append);
    }
}
