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
internal sealed class LaunchCoordinator
{
    readonly LauncherWorkspace workspace;
    internal LaunchCoordinator(LauncherWorkspace workspace, string configurationDirectory)
    {
        this.workspace = workspace;
        settingsDir = global::System.IO.Path.GetFullPath(configurationDirectory);
    }

    internal readonly string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory);
    internal HttpClient http = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };
    internal Preferences prefs = new();
    internal NetworkSettings runtimeNetwork = new();
    internal MinecraftLauncher launcher = null!;
    internal List<VersionRow> rows = new();
    internal string? downloadVersion;
    internal MSession? online;
    internal bool busy;
    internal bool closeAfterTask;
    internal SettingsStore settings = null!;
    internal OperationCoordinator operations = null!;
    internal CancellationToken CurrentToken => operations?.Current?.Token ?? CancellationToken.None;

    internal DateTime lastProgress;
    internal void SetupLauncher()
    {
        downloadVersion = null;
        Directory.CreateDirectory(prefs.Root);
        launcher = new MinecraftLauncher(MinecraftLauncherParameters.CreateDefault(new MinecraftPath(prefs.Root), http));
        launcher.FileProgressChanged += (_, a) =>
        {
            var owner = operations.Current;
            workspace.Window.Dispatcher.BeginInvoke(() =>
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

        workspace.Window.TaskStatusBar.Visibility = Visibility.Collapsed;
        busy = true;
        workspace.Window.Progress.IsIndeterminate = false;
        SetInputs(false);
        try
        {
            var result = await operations.Run(name ?? TaskRecoveryController.OperationName(action.Method.Name), prefs.Root, workspace.Controls.Versions.SelectedValue as string ?? "", async context =>
            {
                context.Report("正在执行…");
                await action();
            });
            workspace.Window.Status.Text = result.State == OperationState.Cancelled ? "已取消，可以重新执行原操作。" : "就绪";
        }
        catch (Exception ex)
        {
            workspace.Window.Status.Text = "操作失败，详情见运行日志";
            Log(ex.GetType().Name + ": " + ex.Message);
            InlineMessageBox.Show(ex.Message, "操作失败");
        }
        finally
        {
            workspace.Window.TaskStatusBar.Visibility = Visibility.Collapsed;
            busy = false;
            workspace.Window.Progress.IsIndeterminate = false;
            workspace.Window.Progress.Value = 0;
            SetInputs(true);
            workspace.Window.CancelTaskButton.IsEnabled = false;
            workspace.TaskRecoveryController.RefreshOperations();
            if (closeAfterTask)
                workspace.Window.Close();
        }
    }

    internal void SetInputs(bool enabled)
    {
        workspace.Controls.LaunchGameButton.IsEnabled = enabled && workspace.Controls.Versions.SelectedItem != null;
        workspace.RunningGamesController.RefreshRunningGames();
        workspace.Controls.LaunchGameButton.ToolTip = workspace.DownloadTaskController.DownloadInProgress ? "请先暂停下载，再启动游戏，避免安装文件与游戏进程同时使用。" : "启动当前选择的已安装版本";
        System.Windows.Controls.ToolTipService.SetShowOnDisabled(workspace.Controls.LaunchGameButton, true);
        workspace.Controls.Versions.IsEnabled = enabled || workspace.DownloadTaskController.DownloadInProgress;
        workspace.Controls.Snapshots.IsEnabled = enabled;
        workspace.Controls.AccountMode.IsEnabled = enabled || workspace.DownloadTaskController.DownloadInProgress;
        workspace.Controls.PlayerName.IsEnabled = enabled || workspace.DownloadTaskController.DownloadInProgress;
        workspace.Controls.GameRoot.IsEnabled = enabled;
        workspace.Controls.JavaPath.IsEnabled = enabled;
        workspace.Controls.Memory.IsEnabled = enabled;
        workspace.Controls.GameWidth.IsEnabled = enabled;
        workspace.Controls.GameHeight.IsEnabled = enabled;
        var browse = enabled || operations.Current?.Record.Download != null;
        workspace.Controls.SearchQuery.IsEnabled = browse;
        workspace.Controls.ContentType.IsEnabled = browse;
        workspace.Controls.SearchResults.IsEnabled = browse;
        workspace.Controls.ProjectVersions.IsEnabled = browse;
        workspace.Controls.InstalledContent.IsEnabled = enabled;
        workspace.Controls.PackOptional.IsEnabled = enabled;
    }

    internal void Log(string text)
    {
        // Never write command lines or authentication payloads: they can contain access tokens.
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}\n";
        void Append()
        {
            workspace.Controls.LogBox.AppendText(LogPrivacy.Redact(line));
            if (workspace.Controls.LogBox.Text.Length > 150000)
                workspace.Controls.LogBox.Text = workspace.Controls.LogBox.Text[^100000..];
            workspace.Controls.LogBox.ScrollToEnd();
        }

        if (workspace.Window.Dispatcher.CheckAccess())
            Append();
        else
            workspace.Window.Dispatcher.BeginInvoke(Append);
    }

    internal Task Refresh() => RefreshLocalVersions();

    internal Task RefreshLocalVersions()
    {
        ApplyVersionRows(rows.Where(r => r.Type != "local").ToList());
        return Task.CompletedTask;
    }

    internal async Task<List<VersionRow>> FetchOnlineVersions(CancellationToken token)
    {
        // Capture the launcher before awaiting: a directory switch may replace it.
        var source = launcher;
        var all = await source.GetAllVersionsAsync(token);
        return all.Select(v => new VersionRow(v.Name, "", v.Type?.ToString() ?? "local",
            v.ReleaseTime == default ? null : v.ReleaseTime)).ToList();
    }

    internal void ApplyVersionRows(List<VersionRow> source)
    {
        var local = LocalVersions();
        rows = source.Select(r => r with { Label = workspace.ModpackController.VersionLabel(r.Id, local) }).ToList();
        foreach (var id in ProfileKeys().Where(id => !rows.Any(r => r.Id == id)))
            rows.Insert(0, new(id, workspace.ModpackController.VersionLabel(id, local), "local"));
        Filter();
        workspace.DownloadPageController.RenderVanillaDownloads();
        workspace.ResourceBrowserController.UpdateTargets();
        if (workspace.DownloadPageController.downloadCategory == "模组加载器")
            workspace.LoaderOverviewController.PrepareLoaderConfiguration();
        if (workspace.InstanceContentController.sketchHome != null)
            workspace.InstanceWorkspaceController.RefreshInstanceViews();
    }

    internal HashSet<string> LocalVersions()
    {
        var dir = Path.Combine(prefs.Root, "versions");
        return Directory.Exists(dir) ? Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".json")) && VersionInstallation.IsReady(prefs.Root, Path.GetFileName(d))).Select(Path.GetFileName).OfType<string>().ToHashSet() : new();
    }

    internal HashSet<string> ProfileKeys()
    {
        var dir = Path.Combine(prefs.Root, "versions");
        return Directory.Exists(dir) ? Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".json"))).Select(Path.GetFileName).OfType<string>().ToHashSet() : new();
    }

    internal void Filter()
    {
        var previous = workspace.Controls.Versions.SelectedValue as string;
        var local = LocalVersions();
        workspace.Controls.Versions.ItemsSource = rows.Where(r => local.Contains(r.Id)).ToList();
        if (previous != null)
            workspace.Controls.Versions.SelectedValue = previous;
        else if (rows.Any(r => r.Id == prefs.LastVersion))
            workspace.Controls.Versions.SelectedValue = prefs.LastVersion;
        else if (rows.Any(r => r.Id == "1.21.1"))
            workspace.Controls.Versions.SelectedValue = "1.21.1";
        if (workspace.Controls.Versions.SelectedIndex < 0 && workspace.Controls.Versions.Items.Count > 0)
            workspace.Controls.Versions.SelectedIndex = 0;
        workspace.Controls.InstalledVersionsHint.Text = local.Count == 0 ? "尚未安装游戏，请到「游戏安装」选择版本并下载。" : "这里只显示已安装版本；其他版本请到「游戏安装」下载。";
        workspace.Controls.LaunchGameButton.IsEnabled = !busy && workspace.Controls.Versions.SelectedItem != null;
    }

    internal string Selected() => workspace.Controls.Versions.SelectedValue as string ?? throw new InvalidOperationException("请先选择一个游戏版本。");
    internal string Instance()
    {
        var catalog = new InstanceCatalog(prefs.Root);
        var descriptor = catalog.GetOrCreate(Selected());
        return (descriptor.Launch ?? prefs.Launch).Isolation == "共享根目录" ? prefs.Root : catalog.DirectoryFor(Selected());
    }

    internal void SavePreferences()
    {
        if (!int.TryParse(workspace.Controls.Memory.Text, out var ram) || ram < 512 || ram > 131072)
            throw new Exception("内存必须在 512–131072 MB 之间。");
        if (!int.TryParse(workspace.Controls.GameWidth.Text, out var w) || !int.TryParse(workspace.Controls.GameHeight.Text, out var h) || w < 320 || h < 240 || w > 16384 || h > 16384)
            throw new Exception("请输入有效的窗口尺寸。");
        var root = GameDefaults.ResolveRoot(workspace.Controls.GameRoot.Text);
        workspace.Controls.GameRoot.Text = root;
        var java = workspace.Controls.JavaPath.Text.Trim().Trim('"');
        if (java.Length > 0 && !File.Exists(java))
            throw new Exception("指定的 Java 文件不存在。");
        prefs.Root = root;
        prefs.Java = java;
        prefs.Player = workspace.Controls.PlayerName.Text.Trim();
        prefs.Ram = ram;
        prefs.Width = w;
        prefs.Height = h;
        prefs.LastVersion = workspace.Controls.Versions.SelectedValue as string ?? "";
        prefs.PreferMicrosoft = workspace.Controls.AccountMode.SelectedIndex == 1;
        prefs.PreferMua = workspace.Controls.AccountMode.SelectedIndex == 2;
        prefs.Launch.RamMb = ram;
        prefs.Launch.Java = java;
        prefs.Launch.Width = w;
        prefs.Launch.Height = h;
        if (!prefs.GameRoots.Contains(root, StringComparer.OrdinalIgnoreCase))
            prefs.GameRoots.Add(root);
        settings.Save(prefs);
    }

    internal Task Install()
    {
        var id = downloadVersion ?? workspace.ModpackController.SelectedLaunchVersion();
        workspace.DownloadQueueController.EnqueueDownload("下载 / 修复游戏 · " + id, id, new()
        {
            Version = id
        });
        return Task.CompletedTask;
    }

    internal async Task Login()
    {
        await workspace.AccountController.MicrosoftLogin(true);
    }

    internal async Task Launch()
    {
        var rootBefore = prefs.Root;
        SavePreferences();
        if (!string.Equals(rootBefore, prefs.Root, StringComparison.OrdinalIgnoreCase))
        {
            SetupLauncher();
            await Refresh();
            throw new Exception("目录已切换，请重新选择版本后启动。");
        }

        MSession session;
        if (workspace.Controls.AccountMode.SelectedIndex == 1)
        {
            await workspace.AccountController.EnsureMicrosoftLogin();
            session = online!;
        }
        else if (workspace.Controls.AccountMode.SelectedIndex == 2)
        {
            await workspace.MuaAccountController.EnsureMuaLogin();
            session = MuaAccountService.Session(workspace.MuaAccountController.muaAccount!);
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
        workspace.Window.Status.Text = "构建启动命令…";
        var directory = Instance();
        var offline = workspace.Controls.AccountMode.SelectedIndex == 0;
        var playerName = session.Username ?? prefs.Player;
        workspace.RunningGamesController.Registry.RequireAvailable(directory, playerName, offline);
        var alreadyRunning = workspace.RunningGamesController.Registry.UsesDirectory(directory);
        var descriptor = new InstanceCatalog(prefs.Root).GetOrCreate(id);
        if (!alreadyRunning)
        {
            GameDefaults.EnsureChinese(directory, workspace.ResourceBrowserController.Target().GameVersion);
            OfflineSkinPackBuilder.Prepare(prefs.Root, directory, prefs.OfflineSkin, workspace.ResourceBrowserController.Target().GameVersion, offline);
        }
        var launchSettings = descriptor.Launch ?? prefs.Launch;
        if (workspace.ExperienceController.nextServerAddress != null)
        {
            launchSettings = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(launchSettings))!;
            launchSettings.Server = workspace.ExperienceController.nextServerAddress;
        }

        var requirements = workspace.InstanceWorkspaceController.RequirementsFor(descriptor.LaunchProfileId);
        var preflight = await Task.Run(() => ModPreflight.Scan(directory, requirements.Minecraft, requirements.Loader, JavaDiscovery.RequiredMajor(prefs.Root, descriptor.LaunchProfileId), requirements.LoaderVersion, CurrentToken), CurrentToken);
        if (preflight.Findings.Count > 0 && !workspace.ExperienceController.ConfirmModScan(preflight, "启动前检查"))
            return;
        AuthlibLaunch? external = null;
        if (workspace.Controls.AccountMode.SelectedIndex == 2)
        {
            var metadata = await workspace.MuaAccountController.muaAccounts.Metadata(CurrentToken);
            operations.Current?.Report("准备 MUA 认证组件…");
            var jar = await AuthlibLaunch.Prepare(Path.Combine(settingsDir, "authlib-injector"), http, CurrentToken);
            external = new(jar, metadata.Json);
        }

        var process = await LaunchService.Build(new(prefs.Root, directory, workspace.ModpackController.SelectedLaunchVersion(), launchSettings.Java, launchSettings.AutoMemory ? MemoryPolicy.Recommend() : launchSettings.RamMb, launchSettings.Width, launchSettings.Height, session, launchSettings, external), CurrentToken, http);
        CurrentToken.ThrowIfCancellationRequested();
        var access = typeof(MSession).GetProperty("AccessToken")?.GetValue(session)?.ToString() ?? "";
        using (OperationExecution.BeginCommit())
        {
            var secrets = new[] { access, workspace.Controls.AccountMode.SelectedIndex == 2 ? workspace.MuaAccountController.muaAccount?.ClientToken ?? "" : "" };
            var running = workspace.RunningGamesController.Registry.Start(prefs.Root, directory, id,
                Path.Combine(settingsDir, "process-logs"), process,
                line => workspace.TaskRecoveryController.QueueGameLine("[" + id + "/" + playerName + "] " + line), secrets, playerName, offline);
            var diagnosisContext = new DiagnosticContext(directory, descriptor.LaunchProfileId, running.LogFile,
                running.StartedUtc, Secrets: secrets);
            _ = workspace.RunningGamesController.ObserveGame(running, diagnosisContext);
            _ = new GameWindowBehaviorController(workspace.Window, Log).Apply(process, launchSettings, running.Session);
        }

        Log("已启动 " + id + "，实例：" + directory);
    }

    internal async Task InstallFabric()
    {
        var id = downloadVersion ?? Selected();
        if (!rows.Any(r => r.Id == id && (r.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || r.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            throw new Exception("请选择一个官方原版版本。");
        workspace.Window.Status.Text = "读取 Fabric 加载器…";
        using var list = JsonDocument.Parse(await http.GetStringAsync("https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(id), CurrentToken));
        var item = list.RootElement.EnumerateArray().FirstOrDefault(e => e.GetProperty("loader").GetProperty("stable").GetBoolean());
        if (item.ValueKind == JsonValueKind.Undefined)
            throw new Exception("该版本暂时没有稳定 Fabric 加载器。");
        var loader = item.GetProperty("loader").GetProperty("version").GetString()!;
        workspace.DownloadQueueController.EnqueueDownload("Fabric · " + id, id, new()
        {
            Version = id,
            Loader = "fabric",
            LoaderVersion = loader
        });
    }

    internal void OpenFolder(string dir)
    {
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }

    internal async Task Import(string child, string filter)
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
            workspace.ResourceBrowserController.RequireStopped();
            if (!LocalVersions().Contains(Selected()))
                throw new IOException("请先完成游戏安装。");
            if (child == "mods" && workspace.ResourceBrowserController.Target().Loader == "vanilla")
                throw new IOException("请先选择已安装加载器的实例。");
            var count = await new ContentService(http, CurrentToken).ImportFiles(Instance(), child, dialog.FileNames);
            Log($"已导入 {count} 个文件；同名文件已跳过。");
            workspace.ResourceBrowserController.RefreshLocalContent();
        }, "导入本地内容");
    }

    internal async void RefreshClick(object s, RoutedEventArgs e) => await Run(RefreshLocalVersions, "刷新本地版本");
    internal async void LoginClick(object s, RoutedEventArgs e) => await Run(Login);
    internal void InstallClick(object s, RoutedEventArgs e)
    {
        try
        {
            var id = downloadVersion ?? workspace.ModpackController.SelectedLaunchVersion();
            workspace.DownloadQueueController.EnqueueDownload("下载 / 修复游戏 · " + id, id, new()
            {
                Version = id
            });
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    internal async void LaunchClick(object s, RoutedEventArgs e) => await Run(Launch);
    internal async void FabricClick(object s, RoutedEventArgs e) => await Run(InstallFabric);
    internal async void SaveClick(object s, RoutedEventArgs e) => await Run(async () =>
    {
        SavePreferences();
        SetupLauncher();
        await workspace.TaskRecoveryController.StartupRecovery();
        Log("设置已保存。");
    });
    internal void FilterChanged(object s, RoutedEventArgs e)
    {
        if (workspace.Controls.Versions != null)
            Filter();
    }

    internal async void ImportModClick(object s, RoutedEventArgs e) => await Import("mods", "Java 模组|*.jar");
    internal async void ImportResourceClick(object s, RoutedEventArgs e) => await Import("resourcepacks", "资源包|*.zip");
    internal void FolderClick(object s, RoutedEventArgs e)
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

    internal void ModsFolderClick(object s, RoutedEventArgs e)
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

    internal void StopClick(object s, RoutedEventArgs e) => workspace.RunningGamesController.ShowRunningGames();

    internal void Initialize()
    {
        Directory.CreateDirectory(settingsDir);
        settings = new SettingsStore(settingsDir);
        try
        {
            prefs = settings.Load();
        }
        catch (Exception ex)
        {
            workspace.InlineHostController.ShowStartupProblem(ex.Message);
            return;
        }

        try
        {
            workspace.DownloadQueueController.queueLease = new FileStream(Path.Combine(settingsDir, "download-queue.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            workspace.InlineHostController.ShowStartupProblem("此配置目录已有启动器运行，请使用原窗口。");
            return;
        }

        operations = new OperationCoordinator(Path.Combine(settingsDir, "tasks"));
        operations.Changed += record => workspace.Window.Dispatcher.BeginInvoke(() =>
        {
            workspace.TaskRecoveryController.ApplyOperation(record);
            workspace.DownloadMiniBarController.RefreshMiniDownloads(true);
            if (workspace.Window.MainTabs.SelectedIndex == 6)
                workspace.TaskRecoveryController.RefreshOperations();
        });
        workspace.Controls.GameRoot.Text = prefs.Root;
        workspace.Controls.JavaPath.Text = prefs.Java;
        workspace.Controls.PlayerName.Text = prefs.Player;
        workspace.Controls.Memory.Text = prefs.Ram.ToString();
        workspace.Controls.GameWidth.Text = prefs.Width.ToString();
        workspace.Controls.GameHeight.Text = prefs.Height.ToString();
        prefs.Network ??= new();
        runtimeNetwork = JsonSerializer.Deserialize<NetworkSettings>(JsonSerializer.Serialize(prefs.Network))!;
        http.Dispose();
        http = NetworkPolicyHandler.Create(runtimeNetwork);
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"MCUGLauncher/{ProductInfo.Version} (Windows-desktop-launcher)");
        SetupLauncher();
        workspace.InstanceWorkspaceController.BuildFeaturePages();
        workspace.TaskRecoveryController.AttachTaskCancellation();
        workspace.InstanceWorkspaceController.ApplyPersonalization();
        workspace.AccountController.BuildAccountUI();
        workspace.DiagnosticsController.BuildDiagnosticsUI();
        workspace.CompactShellController.BuildCompactShell();
        workspace.HomeAccountController.BuildHomeAccountPanel();
        workspace.DownloadMiniBarController.BuildMiniDownloads();
        workspace.ExperienceController.BuildExperienceUI();
        workspace.InlineHostController.BuildInlineHost();
        workspace.WindowChromeController.BuildWindowChrome();
        workspace.KeyboardNavigationController.BuildKeyboardNavigation();
        workspace.InstanceContentController.BuildSketchUI();
        workspace.ModernShellController.BuildModernUI();
        workspace.MenuExtensionsController.BuildMenuExtensionHosts();
        workspace.Window.Loaded += async (_, _) =>
        {
            workspace.TaskRecoveryController.EnsureOutputTimer();
            if (settings.Notice != null)
                Log(settings.Notice);
            var loginRequested = Environment.GetCommandLineArgs().Contains("--login");
            if (loginRequested)
                await Run(Login, "微软正版登录");
            if (closeAfterTask)
                return;
            var startup = System.Diagnostics.Stopwatch.StartNew();
            await Run(workspace.TaskRecoveryController.StartupRecovery, "读取本地游戏");
            Log($"本地启动准备完成：{startup.ElapsedMilliseconds} ms；在线版本按需加载。");
            if (!loginRequested)
                await workspace.InstanceWorkspaceController.HandleScriptLaunch();
            workspace.DownloadQueueController.StartDownloadQueue();
        };
        workspace.Window.Closing += (_, e) =>
        {
            if (busy)
            {
                e.Cancel = true;
                closeAfterTask = true;
                operations.Cancel(operations.Current?.Record.Download != null, operations.Current?.Record.Download != null);
                workspace.Window.Status.Text = "正在清理当前任务，完成后自动关闭…";
            }
        };
        workspace.Window.Closed += (_, _) => workspace.ResourceBrowserController.browseCancellation?.Cancel();
    }
}
