using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace BlockLaunch;
internal sealed class DownloadTaskController
{
    readonly LauncherWorkspace workspace;
    internal DownloadTaskController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal bool DownloadInProgress => workspace.LaunchCoordinator.operations?.Current?.Record.Download != null;

    internal InlineDialog? downloadTasksPage;
    internal StackPanel? downloadTaskRows;
    internal ScrollViewer? downloadTaskScroll;
    internal DateTime downloadTaskRefresh;
    internal void OpenDownloadTasks()
    {
        if (downloadTasksPage != null)
        {
            downloadTasksPage.Activate();
            return;
        }

        var body = new DockPanel();
        var heading = new StackPanel();
        DockPanel.SetDock(heading, Dock.Top);
        heading.Children.Add(FeatureUiElements.Hint("任务保存各自的安装目录。暂停保留进度；取消停止任务并保留已校验缓存。提交安装结果时请等待完成。"));
        heading.Children.Add(FeatureUiElements.ActionButton("刷新任务", (_, _) => RefreshDownloadTasks(true)));
        body.Children.Add(heading);
        downloadTaskRows = new StackPanel();
        downloadTaskScroll = new ScrollViewer
        {
            Content = downloadTaskRows,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        body.Children.Add(downloadTaskScroll);
        downloadTasksPage = new InlineDialog
        {
            Owner = workspace.Window,
            Title = "下载任务",
            Tag = "DownloadTasks",
            Content = body
        };
        downloadTasksPage.Closed += (_, _) =>
        {
            downloadTasksPage = null;
            downloadTaskRows = null;
            downloadTaskScroll = null;
        };
        RefreshDownloadTasks(true);
        downloadTasksPage.Show();
    }

    internal void RefreshDownloadTasks(bool force = false)
    {
        if (downloadTaskRows == null || !force && (DateTime.UtcNow - downloadTaskRefresh).TotalSeconds < 1)
            return;
        downloadTaskRefresh = DateTime.UtcNow;
        var offset = downloadTaskScroll?.VerticalOffset ?? 0;
        downloadTaskRows.Children.Clear();
        // Persisted history is throttled; use the live record for current transfer progress.
        var activeRecord = workspace.LaunchCoordinator.operations.Current?.Record;
        var records = workspace.LaunchCoordinator.operations.History().Where(record => record.Download != null).Select(record => record.Id == activeRecord?.Id ? activeRecord : record).OrderBy(record => DownloadTaskPriority(record.State)).ThenByDescending(record => record.Updated).Take(100).ToList();
        foreach (var record in records)
        {
            downloadTaskRows.Children.Add(BuildDownloadTaskCard(record));
        }

        if (records.Count == 0)
            downloadTaskRows.Children.Add(FeatureUiElements.Hint("暂无下载任务。"));
        downloadTaskScroll?.ScrollToVerticalOffset(offset);
    }

    internal Border BuildDownloadTaskCard(OperationRecord record)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = record.Name, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(FeatureUiElements.Hint(DownloadTaskStatus(record)));
        panel.Children.Add(new TextBlock { Text = record.Message, TextWrapping = TextWrapping.Wrap, FontSize = 11, ToolTip = record.Root });
        var actions = new WrapPanel();
        void Add(string name, OperationState state, bool enabled)
        {
            var b = FeatureUiElements.ActionButton(name, (_, _) => ChangeDownloadById(record.Id, state));
            b.IsEnabled = enabled && !record.Committing;
            actions.Children.Add(b);
        }

        Add("暂停", OperationState.Paused, record.State is OperationState.Running or OperationState.Queued);
        Add(record.State == OperationState.Failed ? "重试" : "继续", OperationState.Queued, record.State is OperationState.Paused or OperationState.Failed or OperationState.Interrupted || record.State == OperationState.Queued && workspace.LaunchCoordinator.prefs.DownloadsPaused);
        Add("取消", OperationState.Cancelled, record.State is OperationState.Running or OperationState.Queued or OperationState.Paused or OperationState.Failed or OperationState.Interrupted);
        panel.Children.Add(actions);
        var card = workspace.InstanceWorkspaceController.FeatureCard(panel);
        card.Padding = new Thickness(12);
        card.Margin = new Thickness(0, 0, 0, 8);
        return card;
    }

    internal static int DownloadTaskPriority(OperationState state) => state switch
    {
        OperationState.Running or OperationState.Pausing or OperationState.Cancelling => 0,
        OperationState.Queued or OperationState.Paused => 1,
        _ => 2
    };
    internal string DownloadTaskStatus(OperationRecord record)
    {
        var progress = record.Percent is { } percent ? $"{percent:0}%" : record.State == OperationState.Running ? "准备 / 校验中" : "—";
        var speed = workspace.LaunchCoordinator.operations.Current?.Record.Id == record.Id ? " · " + workspace.DownloadMiniBarController.miniDownloadSpeed.Text : "";
        return $"{record.StateLabel} · {progress}{speed}";
    }

    internal void ChangeDownloadById(string id, OperationState state)
    {
        var record = workspace.LaunchCoordinator.operations.History().FirstOrDefault(r => r.Id == id);
        if (record?.Download == null)
            return;
        var active = workspace.LaunchCoordinator.operations.Current?.Record.Id == id;
        bool changed;
        if (active)
        {
            // Active work owns its cancellation token; queued records are updated in history.
            changed = state != OperationState.Queued && workspace.LaunchCoordinator.operations.Cancel(state == OperationState.Paused);
        }
        else if (state == OperationState.Queued && record.State == OperationState.Queued)
        {
            changed = true;
        }
        else
        {
            changed = workspace.LaunchCoordinator.operations.ChangePending(id, state);
        }

        if (changed && state == OperationState.Queued)
        {
            workspace.LaunchCoordinator.prefs.DownloadsPaused = false;
            workspace.LaunchCoordinator.prefs.PausedDownloadId = "";
            workspace.DownloadMiniBarController.SaveDownloadPause();
        }

        if (changed && state == OperationState.Paused)
        {
            workspace.LaunchCoordinator.prefs.DownloadsPaused = true;
            workspace.LaunchCoordinator.prefs.PausedDownloadId = id;
            workspace.DownloadMiniBarController.SaveDownloadPause();
        }

        if (changed && state == OperationState.Cancelled && workspace.LaunchCoordinator.prefs.PausedDownloadId == id)
        {
            workspace.LaunchCoordinator.prefs.PausedDownloadId = "";
            workspace.DownloadMiniBarController.SaveDownloadPause();
        }

        if (!changed)
            workspace.Window.Status.Text = "任务正在提交或状态已变化，请刷新后重试。";
        workspace.DownloadMiniBarController.RefreshMiniDownloads(true);
        RefreshDownloadTasks(true);
        workspace.TaskRecoveryController.RefreshOperations();
    }

    internal void CancelMiniDownload()
    {
        if (workspace.DownloadMiniBarController.MiniDownloadRecord() is { } record)
            ChangeDownloadById(record.Id, OperationState.Cancelled);
    }

    internal void UpdateTaskbarDownload(OperationRecord? record)
    {
        workspace.Window.TaskbarItemInfo ??= new TaskbarItemInfo();
        workspace.Window.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        workspace.Window.TaskbarItemInfo.ProgressValue = 0;
    }

    internal async Task RunAccountAction(Func<Task> action, string name)
    {
        if (DownloadInProgress)
        {
            if (InlineMessageBox.Show(workspace.Window, "账号操作需要先暂停当前下载，是否暂停？之后可在下载任务中继续。", name, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            var record = workspace.LaunchCoordinator.operations.Current!.Record;
            if (!workspace.LaunchCoordinator.operations.Cancel(pause: true))
            {
                InlineMessageBox.Show("正在提交安装结果，请稍后重试。");
                return;
            }

            workspace.LaunchCoordinator.prefs.DownloadsPaused = true;
            workspace.LaunchCoordinator.prefs.PausedDownloadId = record.Id;
            workspace.DownloadMiniBarController.SaveDownloadPause();
            while (workspace.LaunchCoordinator.busy)
                await Task.Delay(50);
        }

        await workspace.LaunchCoordinator.Run(action, name);
    }
}
