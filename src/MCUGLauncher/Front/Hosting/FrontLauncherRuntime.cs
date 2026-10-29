using System.IO;
using System.Net.Http;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public sealed class FrontLauncherRuntime : IFrontLauncher, IFrontSettings, IFrontPreferences, IFrontVersions, IFrontManagement, IGameLaunchInteraction, IDisposable
{
    readonly SettingsStore store;
    readonly Preferences prefs;
    readonly string configurationDirectory;
    readonly FileStream lease;
    readonly HttpClient http;
    readonly MuaAccountService mua;
    readonly AccountSessionService accounts;
    readonly VersionManagementService versions;
    readonly Dictionary<string, PackUpgradePreview> packUpgrades = new();
    readonly ResourceManagementService resources;
    readonly OperationCoordinator operations;
    readonly DownloadQueueService queue;
    readonly DownloadQueueRunner runner;
    readonly GameLaunchController launch;
    readonly LauncherSettingsService settings;
    readonly List<string> logs = [];
    readonly object logGate = new();
    readonly CancellationTokenSource lifetime = new();
    Task? activeAction;
    Task? shutdown;
    GameSession? game;
    string selectedVersion = "";
    Func<string, bool>? confirmPreflight;
    bool actionBusy;
    bool closing;
    bool disposed;
    bool ready;
    Task? gameObservation;
    CancellationToken actionToken;
    VersionListSnapshot? profileSnapshot;
    readonly Dictionary<(string Root, string Minecraft, string Loader), IReadOnlyList<FrontLoader>> loaderSnapshots = new();
    string status = "正在启动…";

    public IFrontResourceBrowser ResourceBrowser { get; }
    public IFrontDownloadTasks Downloads { get; }
    public string ConfigurationDirectory => configurationDirectory;
    public string DownloadDirectory => prefs.ResourceDownloadDirectory;
    public bool IsBusy => actionBusy || runner.IsRunning;
    public bool IsGameRunning => game != null && !game.Completion.IsCompleted;
    public string Status { get { lock (logGate) return status; } }
    public IReadOnlyList<string> Logs { get { lock (logGate) return logs.ToArray(); } }

    public FrontLauncherRuntime(string? directory = null, XboxAuthNet.OAuth.CodeFlow.IWebUI? microsoftBrowser = null)
    {
        configurationDirectory = Path.GetFullPath(directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory));
        Directory.CreateDirectory(configurationDirectory);
        try
        {
            lease = new FileStream(Path.Combine(configurationDirectory, "download-queue.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException exception)
        {
            throw new IOException("此配置目录已有启动器运行。请先关闭旧窗口或其他 Front 窗口。", exception);
        }
        HttpClient? pendingHttp = null;
        MuaAccountService? pendingMua = null;
        try
        {
            store = new SettingsStore(configurationDirectory);
            prefs = store.Load();
            settings = new LauncherSettingsService(store, () => IsGameRunning || IsBusy || closing);
            pendingHttp = NetworkPolicyHandler.Create(prefs.Network ?? new());
            http = pendingHttp;
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MCUGLauncher/0.21.0 (Front)");
            pendingMua = new MuaAccountService(configurationDirectory);
            mua = pendingMua;
            accounts = new AccountSessionService(new MicrosoftAccountService(configurationDirectory, http, microsoftBrowser), mua);
            versions = new VersionManagementService(http);
            resources = new ResourceManagementService(http);
            ResourceBrowser = new FrontResourceBrowserAdapter(resources, new ResourceBookmarkStore(configurationDirectory));
            operations = new OperationCoordinator(Path.Combine(configurationDirectory, "tasks"));
            queue = new DownloadQueueService(operations, new DownloadPauseStore(store, () => prefs));
            Downloads = new DownloadFrontendService(queue);
            var executor = new DownloadTaskExecutor(new(http, prefs.Network ?? new(), ProtectWorlds,
                () => prefs.Launch, root => SamePath(root, prefs.Root),
                (_, installed) => { Report(installed == null ? "内容已更新。" : "安装完成：" + installed); return Task.CompletedTask; }, Report));
            runner = new DownloadQueueRunner(queue, executor.Execute,
                () => ready && !closing && !actionBusy && !IsGameRunning && operations.Current == null, Report);
            launch = new GameLaunchController(this);
            if (store.Notice != null)
                Report(store.Notice);
            Report("Front 已连接正式配置；等待执行队列。账号、设置和实例与旧前端共用。");
        }
        catch
        {
            pendingMua?.Dispose();
            pendingHttp?.Dispose();
            lease.Dispose();
            throw;
        }
    }

    public FrontAccounts ReadAccounts() => new(prefs.PreferMua ? "mua" : prefs.PreferMicrosoft ? "microsoft" : "offline",
        prefs.Player, accounts.ReadMicrosoft()?.Username ?? "", accounts.ReadMua()?.Profile.Name ?? "");

    public Task Initialize(CancellationToken token) => Exclusive(async cancellation =>
    {
        var messages = await Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            var recovered = InstallationRecovery.Recover(prefs.Root);
            recovered.AddRange(new InstanceCatalog(prefs.Root).MigrateLegacy());
            return recovered;
        }, cancellation);
        foreach (var message in messages)
            Report(message);
        cancellation.ThrowIfCancellationRequested();
        ready = true;
        Report("启动恢复完成，Front 已就绪。");
    }, token);

    public Task LoginMicrosoft(CancellationToken token) => Exclusive(async cancellation =>
    {
        var session = await accounts.LoginMicrosoft(true, cancellation);
        SaveIdentity("microsoft", session.Username ?? throw new AccountLoginException("登录结果缺少玩家名称。"));
        Report("微软账号登录成功。");
    }, token);

    public Task LoginMua(string account, string password, Func<IReadOnlyList<FrontIdentity>, Task<FrontIdentity?>> select, CancellationToken token) => Exclusive(async cancellation =>
    {
        var credential = await accounts.LoginMua(account, password, async profiles =>
        {
            var choice = await select(profiles.Select(profile => new FrontIdentity(profile.Id, profile.Name)).ToArray());
            return choice == null ? null : profiles.FirstOrDefault(profile => profile.Id == choice.Id);
        }, cancellation);
        SaveIdentity("mua", credential.Profile.Name);
        Report("MUA 账号登录成功。");
    }, token);

    public void SelectAccount(string kind, string offlineName)
    {
        RequireIdle();
        var name = kind switch
        {
            "offline" => accounts.Offline(offlineName).Session.Username ?? throw new ArgumentException("离线玩家名称无效。"),
            "microsoft" => accounts.ReadMicrosoft()?.Username ?? throw new InvalidOperationException("请先登录微软账号。"),
            "mua" => accounts.ReadMua()?.Profile.Name ?? throw new InvalidOperationException("请先登录 MUA 账号。"),
            _ => throw new ArgumentException("未知账号类型。")
        };
        SaveIdentity(kind, name);
        Report("已选择账号：" + kind);
    }

    public Task ForgetAccount(string kind, CancellationToken token) => Exclusive(async cancellation =>
    {
        if (kind == "microsoft")
            accounts.ForgetMicrosoft();
        else if (kind == "mua")
            await accounts.ForgetMua(cancellation);
        else
            throw new ArgumentException("未知账号类型。");
        if (kind == "microsoft" && prefs.PreferMicrosoft || kind == "mua" && prefs.PreferMua)
            SaveIdentity("offline", "Player");
        Report("已清除本地账号登录。");
    }, token);

    void SaveIdentity(string kind, string name)
    {
        var pending = store.Load();
        pending.Player = name;
        pending.PreferMicrosoft = kind == "microsoft";
        pending.PreferMua = kind == "mua";
        store.Save(pending);
        prefs.Player = name;
        prefs.PreferMicrosoft = pending.PreferMicrosoft;
        prefs.PreferMua = pending.PreferMua;
    }

    public async Task<FrontProfiles> ReadProfiles(CancellationToken token)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        var snapshot = await versions.Read(prefs.Root, cancellation.Token);
        profileSnapshot = snapshot;
        return new(snapshot.Root, snapshot.Versions.Select(row => new FrontProfile(row.Id, row.Label, snapshot.LocalVersions.Contains(row.Id)) { Type = row.Type }).ToArray(), snapshot.OnlineError ?? "");
    }

    public async Task<IReadOnlyList<FrontLoader>> ReadLoaders(string version, string loader, CancellationToken token)
    {
        RequireLoader(loader);
        if (loader == "vanilla")
            return [];
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        var root = prefs.Root;
        var releases = (await versions.Loaders(version, loader, cancellation.Token)).Select(row => new FrontLoader(row.Version, row.Label)).ToArray();
        cancellation.Token.ThrowIfCancellationRequested();
        loaderSnapshots[(root, version, loader)] = releases;
        return releases;
    }

    public void EnqueueGame(string version, string loader, string loaderVersion)
    {
        RequireOpen();
        RequireLoader(loader);
        if (profileSnapshot == null || !SamePath(profileSnapshot.Root, prefs.Root)
            || !profileSnapshot.Versions.Any(row => row.Id == version &&
                (row.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || row.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("请刷新版本列表并选择官方原版版本。");
        if (string.IsNullOrWhiteSpace(version) || loader != "vanilla" && string.IsNullOrWhiteSpace(loaderVersion))
            throw new ArgumentException("请选择游戏版本和加载器版本。");
        if (loader != "vanilla" && (!loaderSnapshots.TryGetValue((prefs.Root, version, loader), out var releases)
            || !releases.Any(release => release.Id == loaderVersion)))
            throw new ArgumentException("请重新读取所选游戏版本的兼容加载器列表。");
        queue.Enqueue("安装游戏 · " + version + " · " + loader, prefs.Root, version,
            new() { Version = version, Loader = loader, LoaderVersion = loaderVersion, Java = prefs.Java });
        Report("已加入游戏安装队列。");
    }

    public Task ImportPack(string file, CancellationToken token) => Exclusive(async cancellation =>
    {
        var overview = await Task.Run(() => PackService.ReadOverview(file), cancellation);
        var hash = await Task.Run(() => ContentTransaction.Hash(file), cancellation);
        cancellation.ThrowIfCancellationRequested();
        queue.Enqueue("导入整合包 · " + Path.GetFileName(file), prefs.Root, file, new()
        {
            Kind = "import", File = Path.GetFullPath(file), SourceHash = hash, Java = prefs.Java,
            Version = overview.Requirements.Minecraft, Optional = false
        });
        Report("整合包已加入导入队列。");
    }, token);

    public Task EnqueueResource(string project, string version, FrontResourceQuery query, string destination, CancellationToken token) => Exclusive(async cancellation =>
    {
        var result = await resources.Versions(project, new(query.Text, query.GameVersion, query.Loader, query.Kind, query.Offset, query.Sort), cancellation);
        var content = result.SingleOrDefault(item => item.Id == version) ?? throw new IOException("资源版本已变化，请重新读取。");
        var target = string.IsNullOrWhiteSpace(destination) ? prefs.ResourceDownloadDirectory : Path.GetFullPath(destination);
        var request = resources.PrepareDownload(prefs.Root, target, query.Kind, content);
        request.Java = prefs.Java;
        cancellation.ThrowIfCancellationRequested();
        queue.Enqueue("下载资源 · " + content.Name, prefs.Root, content.Id, request);
        Report("资源已加入下载队列。文件保存到：" + target);
    }, token);

    public Task Launch(string version, Func<string, bool> confirm, CancellationToken token) => Exclusive(async cancellation =>
    {
        selectedVersion = version;
        confirmPreflight = confirm;
        try
        {
            await operations.Run("启动游戏", prefs.Root, version, async context =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, context.Token);
                await launch.Launch(linked.Token);
            });
        }
        finally
        {
            confirmPreflight = null;
        }
    }, token);

    public void StopGame() => launch.Stop();
    GameSession? IGameLaunchInteraction.CurrentSession => game;
    Task IGameLaunchInteraction.SaveSettings() => Task.CompletedTask;
    async Task<AccountSession> IGameLaunchInteraction.Authenticate() => prefs.PreferMua
        ? accounts.Mua(await accounts.EnsureMua(false, actionToken))
        : prefs.PreferMicrosoft ? accounts.Microsoft(await accounts.EnsureMicrosoft(actionToken, Report)) : accounts.Offline(prefs.Player);

    GameLaunchSelection IGameLaunchInteraction.CaptureSelection()
    {
        RequireProfile(selectedVersion);
        var pending = store.Load();
        pending.LastVersion = selectedVersion;
        store.Save(pending);
        prefs.LastVersion = selectedVersion;
        var requirements = versions.Requirements(prefs.Root, versions.LaunchProfile(prefs.Root, selectedVersion));
        return new(prefs.Root, selectedVersion, VersionInstallation.IsReady(prefs.Root, selectedVersion),
            versions.InstanceDirectory(prefs.Root, selectedVersion, prefs.Launch), requirements.Minecraft,
            configurationDirectory, prefs.OfflineSkin, prefs.Launch, null, http);
    }

    bool IGameLaunchInteraction.ConfirmPreflight(ModScan scan) => confirmPreflight?.Invoke(scan.Summary + "\n\n" +
        string.Join("\n", scan.Findings.Take(100).Select(finding => $"{finding.Severity} · {finding.File} · {finding.Message}"))) == true;
    void IGameLaunchInteraction.Report(string message) => Report(message);
    void IGameLaunchInteraction.QueueOutput(string line) => Report(line);
    void IGameLaunchInteraction.Log(string message) => Report(message);
    void IGameLaunchInteraction.Started(GameLaunchResult result)
    {
        game = result.Session;
        gameObservation = ObserveGame(result);
    }

    async Task ObserveGame(GameLaunchResult result)
    {
        try
        {
            var observed = await new GameSessionMonitor().Observe(result.Session, result.Diagnostics);
            Report("游戏已退出。" + (observed.Diagnostic == null ? "" : "诊断：" + observed.Diagnostic.Summary));
        }
        catch (Exception exception)
        {
            Report("游戏会话监控失败：" + exception.Message);
        }
    }

    public IReadOnlyList<FrontLocalContent> ReadContent(string version)
    {
        RequireOpen();
        RequireProfile(version);
        return resources.ListLocal(versions.InstanceDirectory(prefs.Root, version, prefs.Launch))
            .Select(item => new FrontLocalContent(item.Path, item.Name, item.Detail, item.State)).ToArray();
    }

    public void ToggleContent(string version, string path)
    {
        RequireIdle();
        RequireProfile(version);
        var directory = versions.InstanceDirectory(prefs.Root, version, prefs.Launch);
        var item = resources.ListLocal(directory).SingleOrDefault(item => SamePath(item.Path, path))
            ?? throw new IOException("本地资源已变化，请刷新列表。");
        resources.Toggle(item, IsGameRunning);
        Report("已切换本地资源启用状态。");
    }

    public FrontInstance ReadInstance(string key)
    {
        RequireOpen();
        RequireProfile(key);
        var descriptor = versions.Descriptor(prefs.Root, key);
        var launchSettings = System.Text.Json.JsonSerializer.Deserialize<LaunchSettings>(
            System.Text.Json.JsonSerializer.Serialize(descriptor.Launch ?? prefs.Launch))!;
        var requirements = versions.Requirements(prefs.Root, descriptor.LaunchProfileId);
        return new(key, descriptor.DisplayName, descriptor.Description, descriptor.Category, descriptor.Favorite,
            versions.InstanceDirectory(prefs.Root, key, prefs.Launch), launchSettings, descriptor.Launch == null)
            { Minecraft = requirements.Minecraft, Loader = requirements.Loader };
    }

    public void SaveInstanceIdentity(string key, string name, string description, string category, bool favorite)
    {
        RequireIdle();
        RequireProfile(key);
        versions.SaveIdentity(prefs.Root, key, name, description, category, favorite);
        Report("版本信息已保存。");
    }

    public void SaveInstanceLaunch(string key, LaunchSettings? launchSettings)
    {
        RequireIdle();
        RequireProfile(key);
        versions.SaveLaunch(prefs.Root, key, launchSettings);
        Report(launchSettings == null ? "版本已恢复跟随全局设置。" : "版本启动设置已保存。");
    }

    public void EnqueueInstanceLoader(string key, string loader, string loaderVersion)
    {
        RequireIdle();
        RequireLoader(loader);
        var instance = ReadInstance(key);
        if (loader == "vanilla" || !loaderSnapshots.TryGetValue((prefs.Root, instance.Minecraft, loader), out var releases)
            || !releases.Any(release => release.Id == loaderVersion))
            throw new ArgumentException("请重新读取当前游戏的兼容加载器版本。");
        queue.Enqueue("安装加载器 · " + instance.Minecraft + " · " + loader, prefs.Root, instance.Minecraft,
            new() { Version = instance.Minecraft, Loader = loader, LoaderVersion = loaderVersion, Java = instance.Launch.Java, BackupInstanceDirectory = instance.Directory });
        Report("已加入加载器安装队列；原实例与存档保留。" );
    }

    public void EnqueueInstanceRepair(string key)
    {
        RequireIdle();
        var instance = ReadInstance(key);
        queue.Enqueue("下载 / 修复 · " + instance.Name, prefs.Root, key,
            new() { Version = versions.LaunchProfile(prefs.Root, key), Java = instance.Launch.Java });
        Report("已加入实例修复队列。" );
    }

    public async Task<string> CloneInstance(string key, string name, CloneOptions options, CancellationToken token)
    {
        string? created = null;
        await Exclusive(async cancellation =>
        {
            RequireProfile(key);
            await operations.Run("复制实例", prefs.Root, key, async context =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, context.Token);
                created = await versions.Clone(prefs.Root, key, prefs.Launch, name, options, text => context.Report(text), linked.Token);
            });
            Report("独立实例已复制：" + name);
        }, token);
        return created ?? throw new IOException("实例复制未完成。");
    }

    public void ExportLaunchScript(string key, string target)
    {
        RequireIdle();
        RequireProfile(key);
        var executable = Environment.ProcessPath ?? throw new IOException("无法确定启动器路径。");
        if (!Path.GetFileName(executable).Equals("MCUGLauncher.Front.exe", StringComparison.OrdinalIgnoreCase))
            throw new IOException("请从 MCUGLauncher.Front.exe 导出，不能使用测试宿主或 dotnet 路径。");
        var account = prefs.PreferMua ? "mua" : prefs.PreferMicrosoft ? "microsoft" : "offline";
        LaunchScriptExporter.Export(executable, prefs.Root, key, account, target, configurationDirectory);
        Report("启动脚本已导出；脚本不保存令牌，运行时仍需在 Front 确认。");
    }

    public async Task LaunchFromScript(LaunchScriptRequest request, Func<string, bool> confirm, CancellationToken token)
    {
        RequireIdle();
        token.ThrowIfCancellationRequested();
        if (!SamePath(request.Root, prefs.Root))
            throw new IOException("脚本的游戏目录与当前配置不一致。请在 Front 中切换目录后重新导出脚本。");
        if (request.Account is not ("offline" or "microsoft" or "mua"))
            throw new ArgumentException("脚本账号类型无效。");
        RequireProfile(request.Instance);
        if (!VersionInstallation.IsReady(prefs.Root, request.Instance))
            throw new IOException("脚本指定的实例尚未安装完成。");
        if (!confirm("脚本请求启动实例：" + request.Instance + "\n游戏目录：" + prefs.Root + "\n账号类型：" + request.Account + "\n确认后可能弹出账号授权窗口。"))
        {
            Report("已取消脚本启动。");
            return;
        }
        token.ThrowIfCancellationRequested();
        SelectAccount(request.Account, request.Account == "offline" ? prefs.Player : "");
        await Launch(request.Instance, confirm, token);
    }

    public Task ExportInstance(string key, string target, ExportOptions options, CancellationToken token) => Exclusive(async cancellation =>
    {
        var instance = ReadInstance(key);
        await InstanceManagement.Export(instance.Directory, target, instance.Name,
            versions.Requirements(prefs.Root, versions.LaunchProfile(prefs.Root, key)), options, cancellation);
        Report("整合包已导出。");
    }, token);

    public Task ImportContent(string key, string kind, string[] files, CancellationToken token) => Exclusive(async cancellation =>
    {
        if (kind is not ("mods" or "resourcepacks" or "shaderpacks"))
            throw new ArgumentException("不支持的本地资源类型。");
        var instance = ReadInstance(key);
        var requirements = versions.Requirements(prefs.Root, versions.LaunchProfile(prefs.Root, key));
        await resources.Import(new(prefs.Root, key, instance.Directory, requirements.Minecraft, requirements.Loader), kind, files, IsGameRunning, cancellation);
        Report("本地资源已导入。");
    }, token);

    public IReadOnlyList<FrontWorld> ReadWorlds(string key)
    {
        var directory = Path.Combine(ReadInstance(key).Directory, "saves");
        return Directory.Exists(directory) ? Directory.GetDirectories(directory)
            .Where(path => File.Exists(Path.Combine(path, "level.dat")))
            .Select(path => new FrontWorld(Path.GetFileName(path), path)).ToArray() : [];
    }

    public string ReadGameLog(string key) => GameLogReader.ReadLatest(ReadInstance(key).Directory);

    public void OpenInstanceDirectory(string key, string child)
    {
        if (child is not ("" or "mods" or "resourcepacks" or "shaderpacks" or "saves"))
            throw new ArgumentException("实例子目录无效。");
        var directory = Path.Combine(ReadInstance(key).Directory, child);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("目录尚未创建，请先启动实例或导入资源。");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(directory) { UseShellExecute = true });
    }

    public async Task<UninstallPlan> PlanUninstall(string key, bool deleteSharedData, CancellationToken token)
    {
        RequireIdle();
        RequireProfile(key);
        token.ThrowIfCancellationRequested();
        var plan = await versions.PlanUninstall(prefs.Root, key, prefs.Launch, deleteSharedData);
        token.ThrowIfCancellationRequested();
        return plan;
    }

    public Task Uninstall(UninstallPlan plan, CancellationToken token) => Exclusive(async cancellation =>
    {
        if (!SamePath(plan.Root, prefs.Root))
            throw new IOException("删除计划不属于当前游戏目录，请重新读取。");
        RequireProfile(plan.Key);
        cancellation.ThrowIfCancellationRequested();
        var directory = versions.InstanceDirectory(prefs.Root, plan.Key, prefs.Launch);
        var related = operations.History().Where(record => record.Download != null && SamePath(record.Root, plan.Root)
            && (record.Target == plan.Key || record.Download.Destination.Length > 0 && SamePath(record.Download.Destination, directory)
                || record.Download.BackupInstanceDirectory.Length > 0 && SamePath(record.Download.BackupInstanceDirectory, directory)))
            .Where(record => record.State is OperationState.Queued or OperationState.Paused or OperationState.Failed or OperationState.Interrupted).ToArray();
        foreach (var record in related)
            if (!queue.Change(record.Id, OperationState.Cancelled).Changed)
                throw new IOException("无法取消关联下载任务，请先在下载列表处理：" + record.Name);
        await versions.Uninstall(plan, prefs.Launch);
        if (selectedVersion == plan.Key)
            selectedVersion = "";
        Report("实例已删除：" + plan.Name);
    }, token);

    public async Task<IReadOnlyList<FrontJava>> ScanJava(CancellationToken token)
    {
        RequireOpen();
        var values = await JavaDiscovery.Scan(prefs.Root, prefs.ExtraJavaPaths, token);
        return values.Select(value => new FrontJava(value.Path, value.Major, value.Architecture, value.Vendor, value.Error)).ToArray();
    }

    public async Task<IReadOnlyList<FrontContentUpdate>> CheckContentUpdates(string key, CancellationToken token)
    {
        RequireIdle();
        var instance = ReadInstance(key);
        var values = await resources.CheckUpdates(instance.Directory, instance.Minecraft, instance.Loader, IsGameRunning, token);
        return values.Select(value => new FrontContentUpdate(value.Current.ProjectId, value.Current.RelativePath, value.Current.Name,
            value.Current.VersionId, value.Latest?.VersionNumber ?? "", value.Error, value.Latest?.Id ?? "")).ToArray();
    }

    public Task EnqueueContentUpdates(string key, IReadOnlyList<string> projectIds, CancellationToken token) => Exclusive(async cancellation =>
    {
        if (projectIds.Count == 0)
            throw new ArgumentException("请选择至少一项内容更新。");
        var instance = ReadInstance(key);
        var managed = ContentService.ReadManaged(instance.Directory);
        var selected = new List<ContentVersion>();
        foreach (var projectId in projectIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var item = managed.FirstOrDefault(value => value.ProjectId == projectId) ?? throw new IOException("更新项目已变化，请重新检查。");
            var kind = ContentUpdates.Kind(item);
            var candidates = await resources.Versions(projectId, new("", instance.Minecraft, kind == "datapack" ? "datapack" : instance.Loader, kind), cancellation);
            var latest = candidates.Where(value => value.VersionType == "release").OrderByDescending(value => value.DatePublished).FirstOrDefault();
            if (latest != null && latest.Id != item.VersionId)
                selected.Add(latest);
        }
        var preview = await resources.PreviewUpdates(instance.Directory, selected, instance.Minecraft, instance.Loader, IsGameRunning, cancellation);
        var request = resources.PrepareUpdate(preview, instance.Directory, instance.Minecraft, instance.Loader);
        request.Java = prefs.Java;
        queue.Enqueue("更新实例内容 · " + instance.Name, prefs.Root, key, request);
        Report("内容更新已加入队列。");
    }, token);

    public Task RestoreLatestContent(string key, CancellationToken token) => Exclusive(async cancellation =>
    {
        var instance = ReadInstance(key);
        await resources.Restore(instance.Directory, IsGameRunning, cancellation);
        Report("已恢复最近一次内容事务。");
    }, token);

    public IReadOnlyList<FrontBackup> ReadBackups(string key)
    {
        var instance = ReadInstance(key);
        return SaveProtection.List(Path.Combine(configurationDirectory, "world-backups"), instance.Directory).Select(value =>
            new FrontBackup(value.Id, value.Reason, value.Created, value.Worlds.Select(world => world.World).ToArray())).ToArray();
    }

    public Task CreateBackup(string key, string reason, CancellationToken token) => Exclusive(async cancellation =>
    {
        var instance = ReadInstance(key);
        var backup = await Task.Run(() => SaveProtection.Create(instance.Directory, Path.Combine(configurationDirectory, "world-backups"), reason, prefs.SaveBackupCount, cancellation), cancellation);
        Report(backup == null ? "当前实例没有需要备份的存档。" : "存档备份已完成。");
    }, token);

    public Task RestoreBackup(string key, string backupId, string world, CancellationToken token) => Exclusive(async cancellation =>
    {
        var instance = ReadInstance(key);
        var root = Path.Combine(configurationDirectory, "world-backups");
        var backup = SaveProtection.List(root, instance.Directory).SingleOrDefault(value => value.Id == backupId)
            ?? throw new IOException("备份已变化，请刷新列表。");
        var item = backup.Worlds.SingleOrDefault(value => value.World == world) ?? throw new IOException("请选择备份内的世界。");
        var archive = SaveProtection.ArchivePath(root, instance.Directory, backup, item);
        var restored = await Task.Run(() => WorldArchives.Import(instance.Directory, archive, progress: Report, token: cancellation), cancellation);
        Report("存档已恢复为新世界：" + restored);
    }, token);

    public async Task SaveJava(string key, string path, bool global, CancellationToken token)
    {
        string? verifiedPath = null;
        await Exclusive(async cancellation =>
        {
            RequireProfile(key);
            var found = (await JavaDiscovery.Scan(prefs.Root, prefs.ExtraJavaPaths.Append(path), cancellation))
                .FirstOrDefault(value => SamePath(value.Path, path) && value.Error.Length == 0)
                ?? throw new IOException("此 Java 无法使用，请重新扫描。");
            var required = JavaDiscovery.RequiredMajor(prefs.Root, versions.LaunchProfile(prefs.Root, key));
            if (required > 0 && found.Major != required)
                throw new IOException($"当前实例需要 Java {required}，所选版本为 {found.Major}。");
            verifiedPath = found.Path;
        }, token);
        token.ThrowIfCancellationRequested();
        RequireIdle();
        var instance = ReadInstance(key);
        var value = global ? ReadPreferences().Launch : instance.Launch;
        value.Java = verifiedPath ?? throw new IOException("Java 校验未完成。");
        if (global)
            settings.SaveGlobalLaunch(prefs, value);
        else
            versions.SaveLaunch(prefs.Root, key, value);
        Report(global ? "全局 Java 已保存。" : "实例 Java 已保存。");
    }

    public async Task<FrontPackUpgrade> PreparePackUpgrade(string key, string archive, CancellationToken token)
    {
        FrontPackUpgrade? result = null;
        await Exclusive(async cancellation =>
        {
            var instance = ReadInstance(key);
            var preview = await new PackUpgradeService(http).Prepare(prefs.Root, key, instance.Directory, archive,
                instance.Launch.Java, prefs.Network ?? new(), Report, cancellation);
            var id = Guid.NewGuid().ToString("N");
            packUpgrades.Add(id, preview);
            result = new(id, preview.Files.Select(file => new FrontUpgradeFile(file.Relative, file.Reason, file.Conflict, file.KeepOld)).ToArray(), preview.HasBaseline);
            Report("升级预览已准备；原实例尚未修改。");
        }, token);
        return result ?? throw new IOException("整合包升级预览未完成。");
    }

    public async Task<string> CommitPackUpgrade(string key, string previewId, IReadOnlyList<string> keepOld, CancellationToken token)
    {
        string? created = null;
        await Exclusive(async cancellation =>
        {
            var instance = ReadInstance(key);
            if (!packUpgrades.TryGetValue(previewId, out var preview) || preview.OriginalKey != key || !SamePath(preview.Root, prefs.Root))
                throw new IOException("升级预览已失效，请重新准备。");
            created = await new PackUpgradeService(http).Commit(preview, keepOld, instance.Launch, Report, cancellation);
            try { DiscardPackUpgrade(previewId); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { Report("升级已提交，但清理暂存失败：" + exception.Message); }
            Report("升级副本已创建；旧实例仍保留。");
        }, token);
        return created ?? throw new IOException("整合包升级未完成。");
    }

    public void DiscardPackUpgrade(string previewId)
    {
        if (packUpgrades.TryGetValue(previewId, out var preview))
        {
            preview.Dispose();
            packUpgrades.Remove(previewId);
        }
    }

    public Task<FrontSettingsSnapshot> Read(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        RequireOpen();
        return Task.FromResult(new FrontSettingsSnapshot(prefs.Root, prefs.Java, prefs.Ram.ToString(), prefs.Width.ToString(), prefs.Height.ToString()));
    }

    public Task<FrontSettingsSnapshot> Save(FrontSettingsSnapshot input, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        RequireIdle();
        settings.Save(prefs, new(input.Root, input.Java, prefs.Player, input.Ram, input.Width, input.Height,
            prefs.LastVersion, prefs.PreferMicrosoft, prefs.PreferMua));
        Report("正式设置已保存。");
        return Read(token);
    }

    public Preferences ReadPreferences()
    {
        RequireOpen();
        return System.Text.Json.JsonSerializer.Deserialize<Preferences>(System.Text.Json.JsonSerializer.Serialize(prefs))!;
    }

    public void SaveNetwork(NetworkSettings network)
    {
        RequireIdle();
        settings.SaveNetwork(prefs, network);
        Report("网络配置已保存，重启后生效。");
    }

    public void SaveDownloadLimit(int kibPerSecond)
    {
        RequireIdle();
        settings.SaveDownloadLimit(prefs, kibPerSecond);
        Report("下载限速已保存。");
    }

    public void SaveAppearance(FrontAppearance appearance)
    {
        RequireIdle();
        settings.SaveAppearance(prefs, appearance.DarkMode, appearance.Accent, appearance.BackgroundPath,
            appearance.Opacity, appearance.Fit, appearance.Images);
        Report("外观已保存。");
    }

    public void SaveBackupPolicy(bool enabled, int count)
    {
        RequireIdle();
        settings.SaveBackupPolicy(prefs, enabled, count);
        Report("存档保护策略已保存。");
    }

    public void SaveGlobalLaunch(LaunchSettings launchSettings)
    {
        RequireIdle();
        settings.SaveGlobalLaunch(prefs, launchSettings);
        Report("全局启动设置已保存。");
    }

    async Task ProtectWorlds(string root, string directory, CancellationToken token)
    {
        if (!prefs.ProtectSaves)
            return;
        if (IsGameRunning)
            throw new IOException("请先关闭游戏再备份存档。");
        var catalog = new InstanceCatalog(root);
        var registered = versions.ProfileKeys(root).Any(key => SamePath(catalog.DirectoryFor(key), directory));
        if (!registered && !InstanceUninstaller.Inside(directory, root) && !SamePath(directory, root))
            throw new IOException("存档保护目录未登记在游戏目录中。");
        var backup = await Task.Run(() => SaveProtection.Create(directory, Path.Combine(configurationDirectory, "world-backups"),
            "修改加载器", prefs.SaveBackupCount, token), token);
        if (backup != null)
            Report($"存档保护完成：{backup.Worlds.Count} 个世界。");
    }

    public Task Tick() => closing ? Task.CompletedTask : runner.Tick();

    Task Exclusive(Func<CancellationToken, Task> action, CancellationToken token)
    {
        RequireIdle();
        token.ThrowIfCancellationRequested();
        actionBusy = true;
        activeAction = RunAction(action, token);
        return activeAction;
    }

    async Task RunAction(Func<CancellationToken, Task> action, CancellationToken token)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        actionToken = cancellation.Token;
        try
        {
            await action(cancellation.Token);
        }
        finally
        {
            actionBusy = false;
        }
    }

    void RequireIdle()
    {
        RequireOpen();
        if (IsBusy || operations.Current != null || IsGameRunning)
            throw new InvalidOperationException("请先暂停下载并等待清理，或关闭游戏，再执行此操作。");
    }

    void RequireOpen()
    {
        if (closing || disposed)
            throw new InvalidOperationException("启动器正在关闭。");
    }

    void RequireProfile(string version)
    {
        if (!versions.ProfileKeys(prefs.Root).Contains(version))
            throw new IOException("所选实例不存在，请刷新版本列表。");
    }

    static void RequireLoader(string loader)
    {
        if (loader is not ("vanilla" or "fabric" or "forge" or "neoforge" or "quilt"))
            throw new ArgumentException("未知加载器。");
    }

    static bool SamePath(string first, string second) => Path.GetFullPath(first).Equals(Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);

    void Report(string message)
    {
        lock (logGate)
        {
            status = LogPrivacy.Redact(message);
            logs.Add(DateTime.Now.ToString("HH:mm:ss ") + status);
            if (logs.Count > 500)
                logs.RemoveRange(0, logs.Count - 500);
        }
    }

    public Task Shutdown()
    {
        if (shutdown == null || shutdown.IsFaulted || shutdown.IsCanceled)
            shutdown = ShutdownCore();
        return shutdown;
    }

    async Task ShutdownCore()
    {
        closing = true;
        lifetime.Cancel();
        var downloadsStopped = runner.Stop();
        if (operations.Current?.Record.Download == null)
            operations.Cancel();
        await downloadsStopped;
        if (activeAction != null)
            try { await activeAction; } catch (Exception exception) { Report("操作退出：" + exception.Message); }
        if (IsGameRunning)
        {
            launch.Stop();
            await game!.Completion;
        }
        if (gameObservation != null)
            await gameObservation;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        if (IsBusy || IsGameRunning)
            throw new InvalidOperationException("请先等待 Shutdown 完成，再释放 Front 宿主。");
        disposed = true;
        closing = true;
        lifetime.Cancel();
        foreach (var id in packUpgrades.Keys.ToArray())
            try { DiscardPackUpgrade(id); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { Report("清理升级预览失败：" + exception.Message); }
        mua.Dispose();
        http.Dispose();
        lease.Dispose();
        lifetime.Dispose();
    }
}
