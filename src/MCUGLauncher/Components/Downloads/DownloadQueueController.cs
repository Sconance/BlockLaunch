using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class DownloadQueueController
{
    readonly LauncherWorkspace workspace;
    internal DownloadQueueController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal DispatcherTimer? downloadTimer;
    internal FileStream? queueLease;
    internal bool queueReady;
    internal void StartDownloadQueue()
    {
        queueReady = true;
        downloadTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, async (_, _) => await PumpDownloads(), workspace.Window.Dispatcher);
        downloadTimer.Start();
        workspace.Window.Closed += (_, _) =>
        {
            downloadTimer.Stop();
            queueLease?.Dispose();
        };
    }

    internal void EnqueueDownload(string name, string target, DownloadRequest request)
    {
        request.Java = workspace.LaunchCoordinator.prefs.Java;
        workspace.LaunchCoordinator.operations.Enqueue(name, workspace.LaunchCoordinator.prefs.Root, target, request);
        workspace.TaskRecoveryController.RefreshOperations();
        workspace.Window.Status.Text = "已加入下载队列：" + name;
    }

    internal async Task QueuePackImport(string file)
    {
        var root = workspace.LaunchCoordinator.prefs.Root;
        var java = workspace.LaunchCoordinator.prefs.Java;
        var optional = workspace.Controls.PackOptional.IsChecked == true;
        var overview = await Task.Run(() => PackService.ReadOverview(file));
        var displayName = workspace.DropFeedbackController.ConfirmPackImport(overview, root);
        if (displayName == null)
            return;
        var hash = await Task.Run(() => ContentTransaction.Hash(file));
        workspace.LaunchCoordinator.operations.Enqueue("导入整合包 · " + displayName + " · " + overview.Requirements.Minecraft + " / " + overview.Requirements.Loader, root, file, new()
        {
            Kind = "import",
            File = file,
            SourceHash = hash,
            Java = java,
            Optional = optional,
            PackDisplayName = displayName
        });
        workspace.TaskRecoveryController.RefreshOperations();
        workspace.Window.Status.Text = overview.Format + " 整合包已识别并加入下载队列";
    }

    internal async Task PumpDownloads()
    {
        if (!queueReady || workspace.LaunchCoordinator.prefs.DownloadsPaused || workspace.LaunchCoordinator.busy || workspace.LaunchCoordinator.closeAfterTask || workspace.UninstallController.uninstallPreviewOpen || workspace.RunningGamesController.HasRunningGames)
            return;
        var record = workspace.LaunchCoordinator.operations.History().Where(r => r.State == OperationState.Queued && r.Download != null).OrderBy(r => r.QueueOrder == 0 ? r.Started.UtcTicks : r.QueueOrder).ThenBy(r => r.Started).FirstOrDefault();
        if (record == null)
            return;
        workspace.Window.TaskStatusBar.Visibility = Visibility.Collapsed;
        workspace.LaunchCoordinator.busy = true;
        try
        {
            var result = await workspace.LaunchCoordinator.operations.Run(record.Name, record.Root, record.Target, context =>
            {
                workspace.LaunchCoordinator.SetInputs(false);
                return ExecuteDownload(record, context);
            }, record);
            workspace.Window.Status.Text = result.Name + " · " + result.StateLabel;
        }
        catch (Exception ex)
        {
            workspace.LaunchCoordinator.Log("下载任务失败，可在任务页重试：" + ex.Message);
            workspace.Window.Status.Text = "下载失败，可在任务与恢复中重试";
        }
        finally
        {
            workspace.Window.TaskStatusBar.Visibility = Visibility.Collapsed;
            workspace.LaunchCoordinator.busy = false;
            workspace.LaunchCoordinator.SetInputs(true);
            workspace.Window.Progress.IsIndeterminate = false;
            workspace.Window.Progress.Value = 0;
            workspace.Window.CancelTaskButton.IsEnabled = false;
            workspace.TaskRecoveryController.RefreshOperations();
            if (workspace.LaunchCoordinator.closeAfterTask)
                workspace.Window.Close();
        }
    }

    internal async Task ExecuteDownload(OperationRecord record, OperationContext context)
    {
        var request = record.Download ?? throw new IOException("任务缺少下载参数。");
        var root = Path.GetFullPath(record.Root);
        var token = context.Token;
        if (request.BackupInstanceDirectory.Length > 0)
        {
            if (!Path.GetFullPath(root).Equals(Path.GetFullPath(workspace.LaunchCoordinator.prefs.Root), StringComparison.OrdinalIgnoreCase))
                throw new IOException("备份任务属于其他游戏目录，请切换到原目录后继续。");
            await workspace.ExperienceController.ProtectWorlds(request.BackupInstanceDirectory, "修改加载器", token);
        }

        context.Report("恢复安装记录并校验已有文件…");
        if (request.Kind != "resource")
            foreach (var message in await Task.Run(() => InstallationRecovery.Recover(root), token))
                workspace.LaunchCoordinator.Log(message);
        string? installed = null;
        async Task<string> InstallGame(PackRequirements requirements) => await new GameInstaller(workspace.LaunchCoordinator.http, root, request.Java, text => context.Report(text), token, workspace.LaunchCoordinator.runtimeNetwork).Install(requirements);
        var packs = new PackService(workspace.LaunchCoordinator.http, token);
        switch (request.Kind)
        {
            case "resource":
                if (InstanceUninstaller.Inside(request.Destination, root) || Path.GetFullPath(request.Destination).Equals(root, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("请选择游戏目录之外的独立下载目录。");
                var downloaded = await ResourceDownloads.Download(workspace.LaunchCoordinator.http, request.Destination, request.ResourceKind, request.Content ?? throw new IOException("资源任务缺少版本。"), text => context.Report(text), token);
                context.Report("已保存：" + downloaded);
                workspace.LaunchCoordinator.Log("资源已保存：" + downloaded);
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
                var service = new ContentService(workspace.LaunchCoordinator.http, token);
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
                var directory = (descriptor.Launch ?? workspace.LaunchCoordinator.prefs.Launch).Isolation == "共享根目录" ? root : catalog.DirectoryFor(installed);
                GameDefaults.EnsureChinese(directory, request.Version, request.Kind is "pack" or "import");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                workspace.LaunchCoordinator.Log("游戏已安装，默认语言暂未写入，启动时会重试：" + ex.Message);
            }
        }

        // Refresh failures must not turn a committed installation into a retryable duplicate pack.
        if (request.Kind != "resource" && Path.GetFullPath(workspace.LaunchCoordinator.prefs.Root).Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                workspace.LaunchCoordinator.SetupLauncher();
                await workspace.LaunchCoordinator.Refresh();
                if (installed != null)
                    workspace.Controls.Versions.SelectedValue = installed;
                workspace.ResourceBrowserController.RefreshLocalContent();
                workspace.InstanceWorkspaceController.RefreshInstanceViews();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                workspace.LaunchCoordinator.Log("安装完成，列表刷新暂不可用：" + ex.Message);
            }
        }

        workspace.LaunchCoordinator.Log("下载任务完成：" + record.Name);
    }

    internal void PauseDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Paused);
    internal void ResumeDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Queued);
    internal void CancelDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Cancelled);
    internal void ChangeDownload(OperationState state)
    {
        if (workspace.Controls.TaskHistoryList.SelectedItem is not OperationRecord record || record.Download == null)
        {
            workspace.Window.Status.Text = "请选择一个下载任务。";
            return;
        }

        bool changed;
        if (workspace.LaunchCoordinator.operations.Current?.Record.Id == record.Id)
            changed = state != OperationState.Queued && workspace.LaunchCoordinator.operations.Cancel(state == OperationState.Paused);
        else
            changed = workspace.LaunchCoordinator.operations.ChangePending(record.Id, state);
        if (changed && state == OperationState.Queued)
        {
            workspace.LaunchCoordinator.prefs.DownloadsPaused = false;
            workspace.LaunchCoordinator.prefs.PausedDownloadId = "";
            workspace.DownloadMiniBarController.SaveDownloadPause();
        }

        if (!changed)
            workspace.Window.Status.Text = "当前状态不能执行此操作；提交文件阶段请等待完成。";
        workspace.TaskRecoveryController.RefreshOperations();
    }

    internal void QueueContent(ContentVersion version)
    {
        try
        {
            if (InstanceUninstaller.Inside(workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory, workspace.LaunchCoordinator.prefs.Root) || Path.GetFullPath(workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory).Equals(Path.GetFullPath(workspace.LaunchCoordinator.prefs.Root), StringComparison.OrdinalIgnoreCase))
                throw new IOException("请选择游戏目录之外的独立下载目录。");
            ResourceDownloads.PathFor(workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory, workspace.ModpackController.ContentKind, version);
            EnqueueDownload("下载资源 · " + version.Name, version.Id, new()
            {
                Kind = "resource",
                ResourceKind = workspace.ModpackController.ContentKind,
                Content = version,
                Destination = workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory
            });
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "无法加入队列");
        }
    }
}
