using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BlockLaunch;

public partial class MainWindow
{
    DispatcherTimer? downloadTimer;
    FileStream? queueLease;
    bool queueReady;

    void StartDownloadQueue()
    {
        queueReady = true;
        downloadTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, async (_, _) => await PumpDownloads(), Dispatcher);
        downloadTimer.Start();
        Closed += (_, _) =>
        {
            downloadTimer.Stop();
            queueLease?.Dispose();
        };
    }

    internal void EnqueueDownload(string name, string target, DownloadRequest request)
    {
        request.Java = prefs.Java;
        operations.Enqueue(name, prefs.Root, target, request);
        RefreshOperations();
        Status.Text = "已加入下载队列：" + name;
    }

    async Task QueuePackImport(string file)
    {
        var root = prefs.Root;
        var java = prefs.Java;
        var optional = PackOptional.IsChecked == true;
        var overview = await Task.Run(() => PackService.ReadOverview(file));
        var displayName = ConfirmPackImport(overview, root);
        if (displayName == null)
            return;
        var hash = await Task.Run(() => ContentTransaction.Hash(file));
        operations.Enqueue("导入整合包 · " + displayName + " · " + overview.Requirements.Minecraft + " / " + overview.Requirements.Loader, root, file, new()
        {
            Kind = "import",
            File = file,
            SourceHash = hash,
            Java = java,
            Optional = optional,
            PackDisplayName = displayName
        });
        RefreshOperations();
        Status.Text = overview.Format + " 整合包已识别并加入下载队列";
    }

    async Task PumpDownloads()
    {
        if (!queueReady || prefs.DownloadsPaused || busy || closeAfterTask || uninstallPreviewOpen || game is
            {
                HasExited: false
            })
            return;
        var record = operations.History().Where(r => r.State == OperationState.Queued && r.Download != null).OrderBy(r => r.QueueOrder == 0 ? r.Started.UtcTicks : r.QueueOrder).ThenBy(r => r.Started).FirstOrDefault();
        if (record == null)
            return;
        TaskStatusBar.Visibility = Visibility.Collapsed;
        busy = true;
        try
        {
            var result = await operations.Run(record.Name, record.Root, record.Target, context =>
            {
                SetInputs(false);
                return ExecuteDownload(record, context);
            }, record);
            Status.Text = result.Name + " · " + result.StateLabel;
        }
        catch (Exception ex)
        {
            Log("下载任务失败，可在任务页重试：" + ex.Message);
            Status.Text = "下载失败，可在任务与恢复中重试";
        }
        finally
        {
            TaskStatusBar.Visibility = Visibility.Collapsed;
            busy = false;
            SetInputs(true);
            Progress.IsIndeterminate = false;
            Progress.Value = 0;
            CancelTaskButton.IsEnabled = false;
            RefreshOperations();
            if (closeAfterTask)
                Close();
        }
    }

    async Task ExecuteDownload(OperationRecord record, OperationContext context)
    {
        var request = record.Download ?? throw new IOException("任务缺少下载参数。");
        var root = Path.GetFullPath(record.Root);
        var token = context.Token;
        if (request.BackupInstanceDirectory.Length > 0)
        {
            if (!Path.GetFullPath(root).Equals(Path.GetFullPath(prefs.Root), StringComparison.OrdinalIgnoreCase))
                throw new IOException("备份任务属于其他游戏目录，请切换到原目录后继续。");
            await ProtectWorlds(request.BackupInstanceDirectory, "修改加载器", token);
        }

        context.Report("恢复安装记录并校验已有文件…");
        if (request.Kind != "resource")
            foreach (var message in await Task.Run(() => InstallationRecovery.Recover(root), token))
                Log(message);
        string? installed = null;
        async Task<string> InstallGame(PackRequirements requirements) => await new GameInstaller(http, root, request.Java, text => context.Report(text), token, runtimeNetwork).Install(requirements);
        var packs = new PackService(http, token);
        switch (request.Kind)
        {
            case "resource":
                if (InstanceUninstaller.Inside(request.Destination, root) || Path.GetFullPath(request.Destination).Equals(root, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("请选择游戏目录之外的独立下载目录。");
                var downloaded = await ResourceDownloads.Download(http, request.Destination, request.ResourceKind, request.Content ?? throw new IOException("资源任务缺少版本。"), text => context.Report(text), token);
                context.Report("已保存：" + downloaded);
                Log("资源已保存：" + downloaded);
                break;
            case "game":
                installed = await InstallGame(new(request.Version, request.Loader, request.LoaderVersion));
                break;
            case "pack":
            case "import":
                var file = request.File;
                if (request.Kind == "import" && !ContentTransaction.Hash(file).Equals(request.SourceHash, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("原整合包文件已改变，请重新加入任务。");
                if (request.Kind == "pack")
                {
                    var content = request.Content ?? throw new IOException("整合包任务缺少版本信息。");
                    var asset = content.Files.FirstOrDefault(f => f.Primary) ?? content.Files.FirstOrDefault() ?? throw new IOException("没有整合包文件。");
                    if (!asset.Filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase))
                        throw new IOException("此版本没有 .mrpack 文件。");
                    file = Path.Combine(root, ".blocklaunch-downloads", ContentService.SafeFile(asset.Filename));
                    await packs.Download(asset.Url, file, asset.Hashes.GetValueOrDefault("sha512") ?? "", text => context.Report(text));
                }

                var result = await packs.Import(file, root, InstallGame, request.Optional, text => context.Report(text), request.PackInstance);
                installed = result.InstanceId;
                var catalog = new InstanceCatalog(root);
                var descriptor = catalog.GetOrCreate(installed);
                if (request.PackDisplayName.Length > 0)
                {
                    descriptor.DisplayName = request.PackDisplayName;
                    catalog.Save(descriptor);
                }

                break;
            case "content":
                var service = new ContentService(http, token);
                var plan = request.PreparedPlan ?? await service.Resolve(request.Content ?? throw new IOException("任务缺少资源版本。"), request.GameVersion, request.Loader);
                await service.Install(plan, request.Destination, text => context.Report(text), request.ContentFingerprint);
                break;
            default:
                throw new IOException("不支持的下载任务类型。");
        }

        if (installed != null)
        {
            try
            {
                var catalog = new InstanceCatalog(root);
                var descriptor = catalog.GetOrCreate(installed);
                var directory = (descriptor.Launch ?? prefs.Launch).Isolation == "共享根目录" ? root : catalog.DirectoryFor(installed);
                GameDefaults.EnsureChinese(directory, request.Version, request.Kind is "pack" or "import");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log("游戏已安装，默认语言暂未写入，启动时会重试：" + ex.Message);
            }
        }
        // Refresh failures must not turn a committed installation into a retryable duplicate pack.
        if (request.Kind != "resource" && Path.GetFullPath(prefs.Root).Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                SetupLauncher();
                await Refresh();
                if (installed != null)
                    Versions.SelectedValue = installed;
                RefreshLocalContent();
                RefreshInstanceViews();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log("安装完成，列表刷新暂不可用：" + ex.Message);
            }
        }

        Log("下载任务完成：" + record.Name);
    }

    void PauseDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Paused);
    void ResumeDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Queued);
    void CancelDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Cancelled);

    void ChangeDownload(OperationState state)
    {
        if (TaskHistoryList.SelectedItem is not OperationRecord record || record.Download == null)
        {
            Status.Text = "请选择一个下载任务。";
            return;
        }

        bool changed;
        if (operations.Current?.Record.Id == record.Id)
            changed = state != OperationState.Queued && operations.Cancel(state == OperationState.Paused);
        else
            changed = operations.ChangePending(record.Id, state);
        if (changed && state == OperationState.Queued)
        {
            prefs.DownloadsPaused = false;
            prefs.PausedDownloadId = "";
            SaveDownloadPause();
        }

        if (!changed)
            Status.Text = "当前状态不能执行此操作；提交文件阶段请等待完成。";
        RefreshOperations();
    }

    void QueueContent(ContentVersion version)
    {
        try
        {
            if (InstanceUninstaller.Inside(prefs.ResourceDownloadDirectory, prefs.Root) || Path.GetFullPath(prefs.ResourceDownloadDirectory).Equals(Path.GetFullPath(prefs.Root), StringComparison.OrdinalIgnoreCase))
                throw new IOException("请选择游戏目录之外的独立下载目录。");
            ResourceDownloads.PathFor(prefs.ResourceDownloadDirectory, ContentKind, version);
            EnqueueDownload("下载资源 · " + version.Name, version.Id, new()
            {
                Kind = "resource",
                ResourceKind = ContentKind,
                Content = version,
                Destination = prefs.ResourceDownloadDirectory
            });
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "无法加入队列");
        }
    }
}
