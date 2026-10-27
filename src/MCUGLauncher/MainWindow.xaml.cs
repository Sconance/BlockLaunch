using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using Microsoft.Win32;

namespace BlockLaunch;

public record VersionRow(string Id, string Label, string Type, DateTimeOffset? Published = null);

public partial class MainWindow : Window
{
    readonly string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory);
    HttpClient http = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };
    Preferences prefs = new();
    NetworkSettings runtimeNetwork = new();
    MinecraftLauncher launcher = null!;
    List<VersionRow> rows = new();
    string? downloadVersion;
    MSession? online;
    Process? game;
    bool busy;
    bool closeAfterTask;
    SettingsStore settings = null!;
    OperationCoordinator operations = null!;
    GameSession? gameSession;
    string? lastGameDirectory;

    CancellationToken CurrentToken => operations?.Current?.Token ?? CancellationToken.None;

    DateTime lastProgress;

    public MainWindow() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory))
    {
    }

    public MainWindow(string configurationDirectory)
    {
        settingsDir = Path.GetFullPath(configurationDirectory);
        InitializeComponent();
        Directory.CreateDirectory(settingsDir);
        settings = new SettingsStore(settingsDir);
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
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MCUGLauncher/0.20.1 (Windows-desktop-launcher)");
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
        BuildWindowChrome();
        BuildKeyboardNavigation();
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

    void SetupLauncher()
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

    async Task Run(Func<Task> action, string? name = null)
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

    void Log(string text)
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

    async Task Refresh()
    {
        Status.Text = "读取版本列表…";
        var local = LocalVersions();
        var known = ProfileKeys();
        try
        {
            var all = await launcher.GetAllVersionsAsync(CurrentToken);
            rows = all.Select(v => new VersionRow(v.Name, VersionLabel(v.Name, local), v.Type?.ToString() ?? "local", v.ReleaseTime == default ? null : v.ReleaseTime)).ToList();
            foreach (var id in known.Where(id => !rows.Any(r => r.Id == id)))
                rows.Insert(0, new(id, VersionLabel(id, local), "local"));
            Log($"读取到 {rows.Count} 个版本，{local.Count} 个本地版本。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            rows = known.Select(id => new VersionRow(id, VersionLabel(id, local), "local")).ToList();
            Log("在线列表不可用，已加载本地版本：" + ex.Message);
        }

        Filter();
        RenderVanillaDownloads();
        UpdateTargets();
        if (sketchHome != null)
            RefreshInstanceViews();
    }

    HashSet<string> LocalVersions()
    {
        var dir = Path.Combine(prefs.Root, "versions");
        return Directory.Exists(dir) ? Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".json")) && VersionInstallation.IsReady(prefs.Root, Path.GetFileName(d))).Select(Path.GetFileName).OfType<string>().ToHashSet() : new();
    }

    HashSet<string> ProfileKeys()
    {
        var dir = Path.Combine(prefs.Root, "versions");
        return Directory.Exists(dir) ? Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".json"))).Select(Path.GetFileName).OfType<string>().ToHashSet() : new();
    }

    void Filter()
    {
        var previous = Versions.SelectedValue as string;
        var local = LocalVersions();
        Versions.ItemsSource = rows.Where(r => local.Contains(r.Id)).ToList();
        if (previous != null)
            Versions.SelectedValue = previous;
        else if (rows.Any(r => r.Id == prefs.LastVersion))
            Versions.SelectedValue = prefs.LastVersion;
        else if (rows.Any(r => r.Id == "1.21.1"))
            Versions.SelectedValue = "1.21.1";
        if (Versions.SelectedIndex < 0 && Versions.Items.Count > 0)
            Versions.SelectedIndex = 0;
        InstalledVersionsHint.Text = local.Count == 0 ? "尚未安装游戏，请到「游戏安装」选择版本并下载。" : "这里只显示已安装版本；其他版本请到「游戏安装」下载。";
        LaunchGameButton.IsEnabled = !busy && Versions.SelectedItem != null;
    }

    string Selected() => Versions.SelectedValue as string ?? throw new InvalidOperationException("请先选择一个游戏版本。");

    string Instance()
    {
        var catalog = new InstanceCatalog(prefs.Root);
        var descriptor = catalog.GetOrCreate(Selected());
        return (descriptor.Launch ?? prefs.Launch).Isolation == "共享根目录" ? prefs.Root : catalog.DirectoryFor(Selected());
    }

    void SavePreferences()
    {
        if (!int.TryParse(Memory.Text, out var ram) || ram < 512 || ram > 131072)
            throw new Exception("内存必须在 512–131072 MB 之间。");
        if (!int.TryParse(GameWidth.Text, out var w) || !int.TryParse(GameHeight.Text, out var h) || w < 320 || h < 240 || w > 16384 || h > 16384)
            throw new Exception("请输入有效的窗口尺寸。");
        var root = GameDefaults.ResolveRoot(GameRoot.Text);
        GameRoot.Text = root;
        var java = JavaPath.Text.Trim().Trim('"');
        if (java.Length > 0 && !File.Exists(java))
            throw new Exception("指定的 Java 文件不存在。");
        prefs.Root = root;
        prefs.Java = java;
        prefs.Player = PlayerName.Text.Trim();
        prefs.Ram = ram;
        prefs.Width = w;
        prefs.Height = h;
        prefs.LastVersion = Versions.SelectedValue as string ?? "";
        prefs.PreferMicrosoft = AccountMode.SelectedIndex == 1;
        prefs.PreferMua = AccountMode.SelectedIndex == 2;
        prefs.Launch.RamMb = ram;
        prefs.Launch.Java = java;
        prefs.Launch.Width = w;
        prefs.Launch.Height = h;
        if (!prefs.GameRoots.Contains(root, StringComparer.OrdinalIgnoreCase))
            prefs.GameRoots.Add(root);
        settings.Save(prefs);
    }

    Task Install()
    {
        var id = downloadVersion ?? SelectedLaunchVersion();
        EnqueueDownload("下载 / 修复游戏 · " + id, id, new()
        {
            Version = id
        });
        return Task.CompletedTask;
    }

    async Task Login()
    {
        await MicrosoftLogin(true);
    }

    async Task Launch()
    {
        if (game is
            {
                HasExited: false
            })
            throw new Exception("游戏正在运行，请先关闭当前游戏。");
        var rootBefore = prefs.Root;
        SavePreferences();
        if (!string.Equals(rootBefore, prefs.Root, StringComparison.OrdinalIgnoreCase))
        {
            SetupLauncher();
            await Refresh();
            throw new Exception("目录已切换，请重新选择版本后启动。");
        }

        MSession session;
        if (AccountMode.SelectedIndex == 1)
        {
            await EnsureMicrosoftLogin();
            session = online!;
        }
        else if (AccountMode.SelectedIndex == 2)
        {
            await EnsureMuaLogin();
            session = MuaAccountService.Session(muaAccount!);
        }
        else
        {
            if (!Regex.IsMatch(prefs.Player, "^[A-Za-z0-9_]{3,16}$"))
                throw new Exception("离线名称必须为 3–16 位英文字母、数字或下划线。");
            session = MSession.CreateOfflineSession(prefs.Player);
        }

        var id = Selected();
        if (!LocalVersions().Contains(id))
            throw new IOException("所选版本尚未安装完成，请到「游戏安装」下载 / 修复后再启动。");
        Status.Text = "构建启动命令…";
        var directory = Instance();
        var descriptor = new InstanceCatalog(prefs.Root).GetOrCreate(id);
        GameDefaults.EnsureChinese(directory, Target().GameVersion);
        PrepareOfflineSkin(directory, Target().GameVersion);
        var launchSettings = descriptor.Launch ?? prefs.Launch;
        if (nextServerAddress != null)
        {
            launchSettings = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(launchSettings))!;
            launchSettings.Server = nextServerAddress;
        }

        var requirements = RequirementsFor(descriptor.LaunchProfileId);
        var preflight = await Task.Run(() => ModPreflight.Scan(directory, requirements.Minecraft, requirements.Loader, JavaDiscovery.RequiredMajor(prefs.Root, descriptor.LaunchProfileId), requirements.LoaderVersion, CurrentToken), CurrentToken);
        if (preflight.Findings.Count > 0 && !ConfirmModScan(preflight, "启动前检查"))
            return;
        AuthlibLaunch? external = null;
        if (AccountMode.SelectedIndex == 2)
        {
            var metadata = await muaAccounts.Metadata(CurrentToken);
            operations.Current?.Report("准备 MUA 认证组件…");
            var jar = await AuthlibLaunch.Prepare(Path.Combine(settingsDir, "authlib-injector"), http, CurrentToken);
            external = new(jar, metadata.Json);
        }

        var process = await LaunchService.Build(new(prefs.Root, directory, SelectedLaunchVersion(), launchSettings.Java, launchSettings.AutoMemory ? MemoryPolicy.Recommend() : launchSettings.RamMb, launchSettings.Width, launchSettings.Height, session, launchSettings, external), CurrentToken, http);
        CurrentToken.ThrowIfCancellationRequested();
        var access = typeof(MSession).GetProperty("AccessToken")?.GetValue(session)?.ToString() ?? "";
        using (OperationExecution.BeginCommit())
        {
            var currentSession = new GameSession(process, Path.Combine(settingsDir, "process-logs", descriptor.InstanceId + ".log"), QueueGameLine, new[] { access, AccountMode.SelectedIndex == 2 ? muaAccount?.ClientToken ?? "" : "" });
            var diagnosisContext = new DiagnosticContext(directory, descriptor.LaunchProfileId, Path.Combine(settingsDir, "process-logs", descriptor.InstanceId + ".log"), DateTime.UtcNow, Secrets: new[] { access, AccountMode.SelectedIndex == 2 ? muaAccount?.ClientToken ?? "" : "" });
            currentSession.Start();
            game = process;
            gameSession = currentSession;
            lastGameDirectory = directory;
            recentDiagnosticContext = diagnosisContext;
            _ = ObserveGame(currentSession, diagnosisContext);
            ApplyLaunchBehavior(process, launchSettings, currentSession);
        }

        Log("已启动 " + id + "，实例：" + directory);
    }

    async Task InstallFabric()
    {
        var id = downloadVersion ?? Selected();
        if (!rows.Any(r => r.Id == id && (r.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || r.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            throw new Exception("请选择一个官方原版版本。");
        Status.Text = "读取 Fabric 加载器…";
        using var list = JsonDocument.Parse(await http.GetStringAsync("https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(id), CurrentToken));
        var item = list.RootElement.EnumerateArray().FirstOrDefault(e => e.GetProperty("loader").GetProperty("stable").GetBoolean());
        if (item.ValueKind == JsonValueKind.Undefined)
            throw new Exception("该版本暂时没有稳定 Fabric 加载器。");
        var loader = item.GetProperty("loader").GetProperty("version").GetString()!;
        EnqueueDownload("Fabric · " + id, id, new()
        {
            Version = id,
            Loader = "fabric",
            LoaderVersion = loader
        });
    }

    void OpenFolder(string dir)
    {
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }

    async Task Import(string child, string filter)
    {
        if (busy)
            return;
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = true
        };
        if (dialog.ShowDialog() != true)
            return;
        await Run(async () =>
        {
            RequireStopped();
            if (!LocalVersions().Contains(Selected()))
                throw new IOException("请先完成游戏安装。");
            if (child == "mods" && Target().Loader == "vanilla")
                throw new IOException("请先选择已安装加载器的实例。");
            var count = await new ContentService(http, CurrentToken).ImportFiles(Instance(), child, dialog.FileNames);
            Log($"已导入 {count} 个文件；同名文件已跳过。");
            RefreshLocalContent();
        }, "导入本地内容");
    }

    async void RefreshClick(object s, RoutedEventArgs e) => await Run(Refresh);
    async void LoginClick(object s, RoutedEventArgs e) => await Run(Login);

    void InstallClick(object s, RoutedEventArgs e)
    {
        try
        {
            var id = downloadVersion ?? SelectedLaunchVersion();
            EnqueueDownload("下载 / 修复游戏 · " + id, id, new()
            {
                Version = id
            });
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    async void LaunchClick(object s, RoutedEventArgs e) => await Run(Launch);
    async void FabricClick(object s, RoutedEventArgs e) => await Run(InstallFabric);
    async void SaveClick(object s, RoutedEventArgs e) => await Run(async () =>
    {
        if (game is
            {
                HasExited: false
            })
            throw new Exception("请先关闭游戏再更改设置。");
        SavePreferences();
        SetupLauncher();
        await StartupRecovery();
        Log("设置已保存。");
    });

    void FilterChanged(object s, RoutedEventArgs e)
    {
        if (Versions != null)
            Filter();
    }

    async void ImportModClick(object s, RoutedEventArgs e) => await Import("mods", "Java 模组|*.jar");
    async void ImportResourceClick(object s, RoutedEventArgs e) => await Import("resourcepacks", "资源包|*.zip");

    void FolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            OpenFolder(Instance());
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    void ModsFolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            OpenFolder(Path.Combine(Instance(), "mods"));
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    void StopClick(object s, RoutedEventArgs e)
    {
        try
        {
            if (gameSession != null && !gameSession.RequestStop())
                Log("未能发送关闭请求，请在游戏内正常退出。");
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }
}
