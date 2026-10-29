using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlockLaunch;
internal sealed class DownloadMiniBarController
{
    readonly LauncherWorkspace workspace;
    internal DownloadMiniBarController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal Border? miniDownload;
    internal readonly TextBlock miniDownloadLabel = new()
    {
        FontSize = 11
    };
    internal readonly TextBlock miniDownloadSpeed = new()
    {
        FontSize = 11,
        Foreground = Brushes.SlateGray,
        HorizontalAlignment = HorizontalAlignment.Right
    };
    internal readonly ProgressBar miniDownloadProgress = new()
    {
        Height = 4,
        Minimum = 0,
        Maximum = 100,
        Margin = new Thickness(0, 5, 0, 0)
    };
    internal readonly Button miniDownloadToggle = new()
    {
        Padding = new Thickness(7, 1, 7, 1),
        Margin = new Thickness(6, 0, 0, 0),
        FontSize = 11,
        Opacity = 0
    };
    internal List<OperationRecord> miniRecords = new();
    internal DispatcherTimer? miniDownloadTimer;
    internal string sampledDownloadId = "";
    internal long sampledBytes;
    internal long sampledTick;
    internal double sampledSpeed;
    internal void BuildMiniDownloads()
    {
        var root = (Grid)workspace.Window.Content;
        var content = new Grid();
        content.ColumnDefinitions.Add(new());
        content.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        content.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        content.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        var text = new Grid();
        text.ColumnDefinitions.Add(new());
        text.ColumnDefinitions.Add(new());
        text.Children.Add(miniDownloadLabel);
        Grid.SetColumn(miniDownloadSpeed, 1);
        text.Children.Add(miniDownloadSpeed);
        var stack = new StackPanel();
        stack.Children.Add(text);
        stack.Children.Add(miniDownloadProgress);
        content.Children.Add(stack);
        Grid.SetColumn(miniDownloadToggle, 1);
        content.Children.Add(miniDownloadToggle);
        miniDownload = new Border
        {
            Width = 260,
            Padding = new Thickness(5, 3, 5, 3),
            Margin = new Thickness(0, 3, 142, 3),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Right,
            Visibility = Visibility.Collapsed,
            Child = content,
            Cursor = Cursors.Hand
        };
        Grid.SetRow(miniDownload, 0);
        Panel.SetZIndex(miniDownload, 20);
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(miniDownload, true);
        root.Children.Add(miniDownload);
        void Fit()
        {
            var scale = Math.Clamp(Math.Min(workspace.Window.ActualWidth / 1060.0, workspace.Window.ActualHeight / 740.0), 1, 1.4);
            miniDownload.Width = Math.Clamp(workspace.Window.ActualWidth / scale - 720, 150, 260);
            miniDownload.Margin = new Thickness(0, 3, 136, 3);
            miniDownloadLabel.Visibility = miniDownload.Width < 220 ? Visibility.Collapsed : Visibility.Visible;
            text.ColumnDefinitions[0].Width = miniDownload.Width < 220 ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        }

        workspace.Window.SizeChanged += (_, _) => Fit();
        Fit();
        miniDownloadToggle.Padding = new Thickness(0);
        miniDownloadToggle.MinHeight = 24;
        miniDownloadToggle.FontSize = 10;
        miniDownloadToggle.Width = 30;
        miniDownloadToggle.Opacity = 1;
        var cancel = FeatureUiElements.ActionButton("×", (_, e) =>
        {
            e.Handled = true;
            workspace.DownloadTaskController.CancelMiniDownload();
        });
        cancel.Padding = new Thickness(0);
        cancel.MinHeight = 24;
        cancel.Width = 22;
        cancel.Margin = new Thickness(2, 0, 0, 0);
        cancel.ToolTip = "取消当前下载";
        Grid.SetColumn(cancel, 2);
        content.Children.Add(cancel);
        var tasks = FeatureUiElements.ActionButton("⌄", (_, e) =>
        {
            e.Handled = true;
            workspace.DownloadTaskController.OpenDownloadTasks();
        });
        tasks.Padding = new Thickness(0);
        tasks.MinHeight = 24;
        tasks.Width = 22;
        tasks.Margin = new Thickness(2, 0, 0, 0);
        tasks.ToolTip = "打开下载任务列表";
        Grid.SetColumn(tasks, 3);
        content.Children.Add(tasks);
        AutomationProperties.SetName(miniDownload, "下载进度");
        miniDownload.MouseEnter += (_, _) => miniDownloadToggle.Opacity = 1;
        miniDownload.MouseLeave += (_, _) =>
        {
            if (!miniDownloadToggle.IsKeyboardFocusWithin)
                miniDownloadToggle.Opacity = 1;
        };
        miniDownloadToggle.GotKeyboardFocus += (_, _) => miniDownloadToggle.Opacity = 1;
        miniDownloadToggle.LostKeyboardFocus += (_, _) =>
        {
            if (!miniDownload.IsMouseOver)
                miniDownloadToggle.Opacity = 1;
        };
        miniDownloadToggle.Click += (_, e) =>
        {
            e.Handled = true;
            ToggleMiniDownload();
        };
        miniDownload.MouseLeftButtonUp += (_, e) =>
        {
            if (miniDownloadToggle.IsMouseOver)
                return;
            e.Handled = true;
            workspace.DownloadTaskController.OpenDownloadTasks();
        };
        miniDownloadTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => RefreshMiniDownloads(), workspace.Window.Dispatcher);
        miniDownloadTimer.Start();
        workspace.Window.Closed += (_, _) => miniDownloadTimer.Stop();
        RefreshMiniDownloads(true);
    }

    internal OperationRecord? MiniDownloadRecord()
    {
        if (workspace.LaunchCoordinator.operations.Current?.Record is { Download: not null } active)
            return active;
        var pending = miniRecords.FirstOrDefault(r => r.Id == workspace.LaunchCoordinator.prefs.PausedDownloadId && r.State is OperationState.Paused or OperationState.Queued) ?? miniRecords.Where(r => r.Download != null && r.State is OperationState.Queued or OperationState.Paused).OrderBy(r => r.QueueOrder == 0 ? r.Started.UtcTicks : r.QueueOrder).FirstOrDefault();
        if (pending != null)
            return pending;
        var recent = miniRecords.Where(r => r.Download != null && r.Root.Equals(workspace.LaunchCoordinator.prefs.Root, StringComparison.OrdinalIgnoreCase)).OrderByDescending(r => r.Updated).FirstOrDefault();
        return recent?.State == OperationState.Failed ? recent : null;
    }

    internal void RefreshMiniDownloads(bool reload = false)
    {
        if (miniDownload == null)
            return;
        if (reload)
            miniRecords = workspace.LaunchCoordinator.operations.History().ToList();
        var record = MiniDownloadRecord();
        miniDownload.Visibility = record == null ? Visibility.Collapsed : Visibility.Visible;
        workspace.DownloadTaskController.UpdateTaskbarDownload(record);
        workspace.DownloadTaskController.RefreshDownloadTasks();
        if (record == null)
        {
            sampledDownloadId = "";
            return;
        }

        if (record.State == OperationState.Failed)
        {
            miniDownloadLabel.Text = "下载失败 · 可重试";
            miniDownloadSpeed.Text = "—";
            miniDownloadProgress.IsIndeterminate = false;
            miniDownloadProgress.Value = 0;
            miniDownloadToggle.Content = "重试";
            miniDownloadToggle.IsEnabled = true;
            miniDownloadToggle.Opacity = 1;
            miniDownload.ToolTip = record.Name + "\n" + record.Message + "\n点击重试；任务与恢复页可查看记录。";
            AutomationProperties.SetName(miniDownloadToggle, "重试失败下载");
            return;
        }

        miniDownloadToggle.Opacity = 1;
        var active = workspace.LaunchCoordinator.operations.Current;
        var tick = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = active?.Record.Id == record.Id ? active.TransferredBytes : 0;
        if (sampledDownloadId != record.Id || bytes < sampledBytes)
        {
            sampledDownloadId = record.Id;
            sampledBytes = bytes;
            sampledTick = tick;
            sampledSpeed = 0;
        }

        var elapsed = (tick - sampledTick) / (double)System.Diagnostics.Stopwatch.Frequency;
        if (elapsed >= .2)
        {
            sampledSpeed = (bytes - sampledBytes) / elapsed;
            sampledBytes = bytes;
            sampledTick = tick;
        }

        var paused = workspace.LaunchCoordinator.prefs.DownloadsPaused || record.State == OperationState.Paused;
        var pausing = record.State == OperationState.Pausing;
        var running = active?.Record.Id == record.Id && record.State == OperationState.Running;
        var percent = record.Percent is { } p ? Math.Clamp(p, 0, 100) : (double?)null;
        miniDownloadLabel.Text = (pausing ? "正在暂停" : paused ? "下载已暂停" : running ? "下载" : "等待下载") + (percent.HasValue ? $" {percent:0}%" : "");
        miniDownloadSpeed.Text = running && !paused ? sampledSpeed > 0 ? FormatDownloadSpeed(sampledSpeed) : "准备 / 校验" : "—";
        miniDownloadProgress.IsIndeterminate = running && !paused && percent == null;
        miniDownloadProgress.Value = percent ?? 0;
        miniDownloadToggle.Content = paused ? "继续" : "暂停";
        miniDownloadToggle.IsEnabled = !pausing && !record.Committing && record.State is OperationState.Running or OperationState.Queued or OperationState.Paused;
        miniDownload.ToolTip = record.Committing ? "正在完成安装，请稍候" : paused ? "点击继续下载" : "点击暂停下载";
        AutomationProperties.SetName(miniDownloadToggle, paused ? "继续下载" : "暂停下载");
    }

    internal static string FormatDownloadSpeed(double bytes) => bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024):0.0} MB/s" : bytes >= 1024 ? $"{bytes / 1024:0.0} KB/s" : $"{bytes:0} B/s";
    internal void ToggleMiniDownload()
    {
        var record = MiniDownloadRecord();
        if (record == null || !miniDownloadToggle.IsEnabled)
            return;
        if (record.State == OperationState.Failed)
        {
            if (workspace.LaunchCoordinator.operations.ChangePending(record.Id, OperationState.Queued))
            {
                workspace.LaunchCoordinator.prefs.DownloadsPaused = false;
                workspace.LaunchCoordinator.prefs.PausedDownloadId = "";
                SaveDownloadPause();
                RefreshMiniDownloads(true);
                workspace.TaskRecoveryController.RefreshOperations();
            }

            return;
        }

        var resume = workspace.LaunchCoordinator.prefs.DownloadsPaused || record.State == OperationState.Paused;
        bool changed;
        if (resume)
            changed = record.State == OperationState.Queued || workspace.LaunchCoordinator.operations.ChangePending(record.Id, OperationState.Queued);
        else
            changed = workspace.LaunchCoordinator.operations.Current?.Record.Id == record.Id ? workspace.LaunchCoordinator.operations.Cancel(pause: true) : workspace.LaunchCoordinator.operations.ChangePending(record.Id, OperationState.Paused);
        if (!changed)
            return;
        workspace.LaunchCoordinator.prefs.DownloadsPaused = !resume;
        workspace.LaunchCoordinator.prefs.PausedDownloadId = resume ? "" : record.Id;
        SaveDownloadPause();
        RefreshMiniDownloads(true);
    }

    internal void SaveDownloadPause()
    {
        try
        {
            workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
        }
        catch (Exception ex)
        {
            workspace.LaunchCoordinator.Log("下载暂停状态暂未保存：" + ex.Message);
        }
    }
}
