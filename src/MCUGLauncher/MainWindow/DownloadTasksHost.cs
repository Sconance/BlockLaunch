using System.Windows;
using System.Windows.Shell;

namespace BlockLaunch;

public partial class MainWindow
{
    internal bool DownloadInProgress => operations?.Current?.Record.Download != null;

    InlineDialog? downloadTasksPage;
    BlockLaunch.SharedUI.DownloadTasksView? downloadTasksView;
    DateTime downloadTaskRefresh;

    void OpenDownloadTasks()
    {
        if (downloadTasksPage != null)
        {
            downloadTasksPage.Activate();
            return;
        }

        var model = new BlockLaunch.SharedUI.DownloadTasksViewModel(downloadFrontend);
        model.CommandCompleted += result => ApplyDownloadChange(new(result.Changed, result.Warning));
        var view = new BlockLaunch.SharedUI.DownloadTasksView(model);
        downloadTasksView = view;
        downloadTasksPage = new InlineDialog
        {
            Owner = this,
            Title = "下载任务",
            Tag = "DownloadTasks",
            Content = view
        };
        downloadTasksPage.Closed += (_, _) =>
        {
            view.Dispose();
            downloadTasksPage = null;
            downloadTasksView = null;
        };
        RefreshDownloadTasks(true);
        downloadTasksPage.Show();
    }

    void RefreshDownloadTasks(bool force = false)
    {
        if (downloadTasksView == null || !force && (DateTime.UtcNow - downloadTaskRefresh).TotalSeconds < 1)
            return;
        downloadTaskRefresh = DateTime.UtcNow;
        downloadTasksView.Refresh();
    }

    internal void ChangeDownloadById(string id, OperationState state)
    {
        ApplyDownloadChange(downloadQueue.Change(id, state));
    }

    bool ApplyDownloadChange(DownloadCommandResult result)
    {
        if (!result.Changed)
            OperationStatusLabel.Text = "任务正在提交或状态已变化，请刷新后重试。";
        if (result.Warning.Length > 0)
        {
            Log(result.Warning);
            OperationStatusLabel.Text = result.Warning;
        }
        RefreshMiniDownloads(true);
        RefreshDownloadTasks(true);
        RefreshOperations();
        return result.Changed;
    }

    void CancelMiniDownload()
    {
        if (MiniDownloadRecord() is
            {
            } record)
            ChangeDownloadById(record.Id, OperationState.Cancelled);
    }

    void UpdateTaskbarDownload(OperationRecord? record)
    {
        TaskbarItemInfo ??= new TaskbarItemInfo();
        TaskbarItemInfo.ProgressState = record?.State switch
        {
            null => TaskbarItemProgressState.None,
            OperationState.Failed => TaskbarItemProgressState.Error,
            OperationState.Running when record.Percent == null => TaskbarItemProgressState.Indeterminate,
            OperationState.Running => TaskbarItemProgressState.Normal,
            _ => TaskbarItemProgressState.Paused
        };
        TaskbarItemInfo.ProgressValue = Math.Clamp((record?.Percent ?? 0) / 100, 0, 1);
    }

    internal async Task RunAccountAction(Func<Task> action, string name)
    {
        if (DownloadInProgress)
        {
            if (InlineMessageBox.Show(this, "账号操作需要先暂停当前下载，是否暂停？之后可在下载任务中继续。", name, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            var record = operations.Current!.Record;
            if (!ApplyDownloadChange(downloadQueue.Change(record.Id, OperationState.Paused)))
            {
                InlineMessageBox.Show("正在提交安装结果，请稍后重试。");
                return;
            }

            while (busy)
                await Task.Delay(50);
        }

        await Run(action, name);
    }
}
