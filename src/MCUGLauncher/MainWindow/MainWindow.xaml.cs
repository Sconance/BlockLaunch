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
    internal bool IncludeOptionalPackContent => PackOptional.IsChecked == true;
    MinecraftLauncher launcher = null!;
    List<VersionRow> rows = new();
    internal string? downloadVersion;
    internal MSession? online;
    Process? game;
    internal bool busy;
    bool closeAfterTask;
    LoaderInstallationController loaderController = null!;
    FileImportController fileImportController = null!;
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
    internal void ThemeWindow(Window window) => windowThemeStyler.Apply(window);
    async void RefreshAvatarClick(object sender, RoutedEventArgs e)
    {
        if (busy)
            return;
        if (AccountMode.SelectedIndex == 1 && savedAccount != null)
            await Run(() => MicrosoftLogin(false), "刷新玩家档案与头像");
        playerIdentityController.Refresh(true);
    }

    string? ConfirmPackImport(PackOverview overview, string root) => fileImportController.ConfirmPackImport(overview, root);
    string DropTargetDirectory() => fileImportController.DropTargetDirectory();
    internal void OpenJavaManager(string root, string profile, Action<string> select, object? owner = null) => javaManagerController.Open(root, profile, select, owner);
    PlayerIdentityState GetPlayerIdentityState() => new(AccountMode.SelectedIndex, muaAccount, muaAccounts, savedAccount, online?.Username, PlayerName.Text, prefs.OfflineSkin, settingsDir, http);

    public MainWindow() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory))
    {
    }

    public MainWindow(string configurationDirectory)
    {
        settingsDir = Path.GetFullPath(configurationDirectory);
        InitializeComponent();
        inlinePageHost = new InlinePageHost(this, inlinePages, HomeNav, MainTabs);
        windowThemeStyler = new WindowThemeStyler(this);
        contentUpdateController = new ContentUpdateController(this);
        instanceUninstallationController = new InstanceUninstallationController(this);
        packUpgradeController = new PackUpgradeController(
            this,
            key => Versions.SelectedValue = key,
            () => HomeNav.IsChecked = true);
        loaderController = new LoaderInstallationController(this);
        fileImportController = new FileImportController(this);
        playerIdentityController = new PlayerIdentityController(PlayerName, SkinAvatar, AvatarCaption, AvatarPlayerName, GetPlayerIdentityState);
        instanceSearchController = new InstanceSearchController(instanceList, instanceQuery, instanceSearchSummary);
        Directory.CreateDirectory(settingsDir);
        settings = new SettingsStore(settingsDir);
        javaManagerController = new JavaManagerController(this);
        pageNavigationController = new PageNavigationController(
            MainTabs,
            PageTitle,
            PageSubtitle,
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
        operations.Changed += record => Dispatcher.BeginInvoke(() =>
        {
            ApplyOperation(record);
            RefreshMiniDownloads(true);
            if (MainTabs.SelectedIndex == 6)
                RefreshOperations();
        });
        GameRoot.Text = prefs.Root;
        JavaPath.Text = prefs.Java;
        PlayerName.Text = prefs.Player;
        Memory.Text = prefs.Ram.ToString();
        GameWidth.Text = prefs.Width.ToString();
        GameHeight.Text = prefs.Height.ToString();
        prefs.Network ??= new();
        runtimeNetwork = JsonSerializer.Deserialize<NetworkSettings>(JsonSerializer.Serialize(prefs.Network))!;
        http.Dispose();
        http = NetworkPolicyHandler.Create(runtimeNetwork);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MCUGLauncher/0.20.0 (Windows-desktop-launcher)");
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
        new LauncherTooltipInitializer(HomeNav, downloadNav, SearchQuery, vanillaQuery).Build();
        BuildSketchUI();
        BuildModernUI();
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
                operations.Cancel(operations.Current?.Record.Download != null, operations.Current?.Record.Download != null);
                Status.Text = "正在清理当前任务，完成后自动关闭…";
            }
        };
        Closed += (_, _) => browseCancellation?.Cancel();
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

        TaskStatusBar.Visibility = Visibility.Visible;
        busy = true;
        Progress.IsIndeterminate = true;
        SetInputs(false);
        try
        {
            var result = await operations.Run(name ?? OperationName(action.Method.Name), prefs.Root, Versions.SelectedValue as string ?? "", async context =>
            {
                context.Report("正在执行…");
                await action();
            });
            Status.Text = result.State == OperationState.Cancelled ? "已取消，可以重新执行原操作。" : "就绪";
        }
        catch (Exception ex)
        {
            Status.Text = "操作失败，详情见运行日志";
            Log(ex.GetType().Name + ": " + ex.Message);
            InlineMessageBox.Show(ex.Message, "操作失败");
        }
        finally
        {
            TaskStatusBar.Visibility = Visibility.Collapsed;
            busy = false;
            Progress.IsIndeterminate = false;
            Progress.Value = 0;
            SetInputs(true);
            CancelTaskButton.IsEnabled = false;
            RefreshOperations();
            if (closeAfterTask)
                Close();
        }
    }

    void SetInputs(bool enabled)
    {
        LaunchGameButton.IsEnabled = enabled && Versions.SelectedItem != null;
        StopGameButton.Visibility = game is
        {
            HasExited: false
        } ? Visibility.Visible : Visibility.Collapsed;
        LaunchGameButton.ToolTip = DownloadInProgress ? "请先暂停下载，再启动游戏，避免安装文件与游戏进程同时使用。" : "启动当前选择的已安装版本";
        System.Windows.Controls.ToolTipService.SetShowOnDisabled(LaunchGameButton, true);
        Versions.IsEnabled = enabled || DownloadInProgress;
        Snapshots.IsEnabled = enabled;
        AccountMode.IsEnabled = enabled || DownloadInProgress;
        PlayerName.IsEnabled = enabled || DownloadInProgress;
        GameRoot.IsEnabled = enabled;
        JavaPath.IsEnabled = enabled;
        Memory.IsEnabled = enabled;
        GameWidth.IsEnabled = enabled;
        GameHeight.IsEnabled = enabled;
        var browse = enabled || operations.Current?.Record.Download != null;
        SearchQuery.IsEnabled = browse;
        ContentType.IsEnabled = browse;
        SearchResults.IsEnabled = browse;
        ProjectVersions.IsEnabled = browse;
        InstalledContent.IsEnabled = enabled;
        PackOptional.IsEnabled = enabled;
    }

    internal void Log(string text)
    {
        // Never write command lines or authentication payloads: they can contain access tokens.
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}\n";
        void Append()
        {
            LogBox.AppendText(LogPrivacy.Redact(line));
            if (LogBox.Text.Length > 150000)
                LogBox.Text = LogBox.Text[^100000..];
            LogBox.ScrollToEnd();
        }

        if (Dispatcher.CheckAccess())
            Append();
        else
            Dispatcher.BeginInvoke(Append);
    }
}
