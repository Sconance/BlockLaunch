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

    internal void EnqueueDownload(string name, string target, DownloadRequest request, string? root = null)
    {
        request.Java = prefs.Java;
        downloadQueue.Enqueue(name, root ?? prefs.Root, target, request);
        RefreshOperations();
        OperationStatusLabel.Text = "已加入下载队列：" + name;
    }

    async Task QueuePackImport(string file)
    {
        var root = prefs.Root;
        var java = prefs.Java;
        var optional = IncludeOptionalPackContent;
        var overview = await Task.Run(() => PackService.ReadOverview(file));
        var displayName = ConfirmPackImport(overview, root);
        if (displayName == null)
            return;
        var hash = await Task.Run(() => ContentTransaction.Hash(file));
        downloadQueue.Enqueue("导入整合包 · " + displayName + " · " + overview.Requirements.Minecraft + " / " + overview.Requirements.Loader, root, file, new()
        {
            Kind = "import",
            File = file,
            SourceHash = hash,
            Java = java,
            Optional = optional,
            PackDisplayName = displayName
        });
        RefreshOperations();
        OperationStatusLabel.Text = overview.Format + " 整合包已识别并加入下载队列";
    }

    async Task PumpDownloads()
    {
        if (!queueReady || downloadQueue.IsPaused || busy || closeAfterTask || instanceUninstallationController.PreviewOpen || game is
            {
                HasExited: false
            })
            return;
        var record = downloadQueue.NextQueued();
        if (record == null)
            return;
        TaskStatusBarControl.Visibility = Visibility.Collapsed;
        busy = true;
        try
        {
            var result = await downloadQueue.Execute(record, context =>
            {
                SetInputs(false);
                return downloadTaskExecutor.Execute(record, context);
            });
            OperationStatusLabel.Text = result.Name + " · " + result.StateLabel;
        }
        catch (Exception ex)
        {
            Log("下载任务失败，可在任务页重试：" + ex.Message);
            OperationStatusLabel.Text = "下载失败，可在任务与恢复中重试";
        }
        finally
        {
            TaskStatusBarControl.Visibility = Visibility.Collapsed;
            busy = false;
            SetInputs(true);
            TaskProgressBarControl.IsIndeterminate = false;
            TaskProgressBarControl.Value = 0;
            CancelTaskButtonControl.IsEnabled = false;
            RefreshOperations();
            if (closeAfterTask)
                Close();
        }
    }

    async Task ProtectDownloadWorlds(string root, string directory, CancellationToken token)
    {
        if (!Path.GetFullPath(root).Equals(Path.GetFullPath(prefs.Root), StringComparison.OrdinalIgnoreCase))
            throw new IOException("备份任务属于其他游戏目录，请切换到原目录后继续。");
        await ProtectWorlds(directory, "修改加载器", token);
    }

    async Task RefreshAfterDownload(string root, string? installed)
    {
        SetupLauncher();
        await Refresh();
        if (installed != null)
            VersionSelectorControl.SelectedValue = installed;
        RefreshLocalContent();
        RefreshInstanceViews();
    }

    void PauseDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Paused);
    void ResumeDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Queued);
    void CancelDownloadClick(object s, RoutedEventArgs e) => ChangeDownload(OperationState.Cancelled);

    void ChangeDownload(OperationState state)
    {
        if (TaskHistoryListControl.SelectedItem is not OperationRecord record || record.Download == null)
        {
            OperationStatusLabel.Text = "请选择一个下载任务。";
            return;
        }

        ChangeDownloadById(record.Id, state);
    }

    void QueueContent(ContentVersion version)
    {
        try
        {
            var request = resourceManagement.PrepareDownload(prefs.Root, prefs.ResourceDownloadDirectory, ContentKind, version);
            EnqueueDownload("下载资源 · " + version.Name, version.Id, request);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "无法加入队列");
        }
    }
}
