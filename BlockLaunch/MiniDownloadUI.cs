using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlockLaunch;
public partial class MainWindow
{
    Border? miniDownload;
    readonly TextBlock miniDownloadLabel = new() { FontSize = 11 };
    readonly TextBlock miniDownloadSpeed = new() { FontSize = 11, Foreground = Brushes.SlateGray, HorizontalAlignment = HorizontalAlignment.Right };
    readonly ProgressBar miniDownloadProgress = new() { Height = 4, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 5, 0, 0) };
    readonly Button miniDownloadToggle = new() { Padding = new Thickness(7, 1, 7, 1), Margin = new Thickness(6, 0, 0, 0), FontSize = 11, Opacity = 0 };
    List<OperationRecord> miniRecords = new();
    DispatcherTimer? miniDownloadTimer;
    string sampledDownloadId = "";
    long sampledBytes;
    long sampledTick;
    double sampledSpeed;

    void BuildMiniDownloads()
    {
        var root = (Grid)Content;
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var content = new Grid(); content.ColumnDefinitions.Add(new()); content.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var text = new Grid(); text.ColumnDefinitions.Add(new()); text.ColumnDefinitions.Add(new());
        text.Children.Add(miniDownloadLabel); Grid.SetColumn(miniDownloadSpeed, 1); text.Children.Add(miniDownloadSpeed);
        var stack = new StackPanel(); stack.Children.Add(text); stack.Children.Add(miniDownloadProgress); content.Children.Add(stack);
        Grid.SetColumn(miniDownloadToggle, 1); content.Children.Add(miniDownloadToggle);
        miniDownload = new Border { Width = 280, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 16, 8), CornerRadius = new CornerRadius(6), Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Right, Visibility = Visibility.Collapsed, Child = content, Cursor = Cursors.Hand };
        Grid.SetRow(miniDownload, root.RowDefinitions.Count - 1); root.Children.Add(miniDownload);
        AutomationProperties.SetName(miniDownload, "下载进度");
        miniDownload.MouseEnter += (_, _) => miniDownloadToggle.Opacity = 1;
        miniDownload.MouseLeave += (_, _) => { if (!miniDownloadToggle.IsKeyboardFocusWithin) miniDownloadToggle.Opacity = 0; };
        miniDownloadToggle.GotKeyboardFocus += (_, _) => miniDownloadToggle.Opacity = 1;
        miniDownloadToggle.LostKeyboardFocus += (_, _) => { if (!miniDownload.IsMouseOver) miniDownloadToggle.Opacity = 0; };
        miniDownloadToggle.Click += (_, e) => { e.Handled = true; ToggleMiniDownload(); };
        miniDownload.MouseLeftButtonUp += (_, e) => { if (miniDownloadToggle.IsMouseOver) return; e.Handled = true; ToggleMiniDownload(); };
        miniDownloadTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => RefreshMiniDownloads(), Dispatcher);
        miniDownloadTimer.Start(); Closed += (_, _) => miniDownloadTimer.Stop(); RefreshMiniDownloads(true);
    }

    OperationRecord? MiniDownloadRecord()
    {
        if(operations.Current?.Record is {Download:not null} active)return active;
        var pending=miniRecords.FirstOrDefault(r=>r.Id==prefs.PausedDownloadId && r.State is OperationState.Paused or OperationState.Queued) ?? miniRecords.Where(r=>r.Download!=null && r.State is OperationState.Queued or OperationState.Paused).OrderBy(r=>r.QueueOrder==0?r.Started.UtcTicks:r.QueueOrder).FirstOrDefault();
        if(pending!=null)return pending;
        var recent=miniRecords.Where(r=>r.Download!=null&&r.Root.Equals(prefs.Root,StringComparison.OrdinalIgnoreCase)).OrderByDescending(r=>r.Updated).FirstOrDefault();return recent?.State==OperationState.Failed?recent:null;
    }

    void RefreshMiniDownloads(bool reload = false)
    {
        if (miniDownload == null) return;
        if (reload) miniRecords = operations.History().ToList();
        var record = MiniDownloadRecord();
        miniDownload.Visibility = record == null ? Visibility.Collapsed : Visibility.Visible;
        if (record == null) { sampledDownloadId = ""; return; }
        if(record.State==OperationState.Failed)
        {
            miniDownloadLabel.Text="下载失败 · 可重试";miniDownloadSpeed.Text="—";miniDownloadProgress.IsIndeterminate=false;miniDownloadProgress.Value=0;
            miniDownloadToggle.Content="重试";miniDownloadToggle.IsEnabled=true;miniDownloadToggle.Opacity=1;
            miniDownload.ToolTip=record.Name+"\n"+record.Message+"\n点击重试；任务与恢复页可查看记录。";
            AutomationProperties.SetName(miniDownloadToggle,"重试失败下载");return;
        }
        miniDownloadToggle.Opacity=miniDownload.IsMouseOver||miniDownloadToggle.IsKeyboardFocusWithin?1:0;
        var active = operations.Current;
        var tick = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = active?.Record.Id == record.Id ? active.TransferredBytes : 0;
        if (sampledDownloadId != record.Id || bytes < sampledBytes) { sampledDownloadId = record.Id; sampledBytes = bytes; sampledTick = tick; sampledSpeed = 0; }
        var elapsed = (tick - sampledTick) / (double)System.Diagnostics.Stopwatch.Frequency;
        if (elapsed >= .2) { sampledSpeed = (bytes - sampledBytes) / elapsed; sampledBytes = bytes; sampledTick = tick; }
        var paused = prefs.DownloadsPaused || record.State == OperationState.Paused;
        var pausing = record.State == OperationState.Pausing;
        var running = active?.Record.Id == record.Id && record.State == OperationState.Running;
        var percent = record.Percent is { } p ? Math.Clamp(p, 0, 100) : (double?)null;
        miniDownloadLabel.Text = (pausing ? "正在暂停" : paused ? "下载已暂停" : running ? "下载" : "等待下载") + (percent.HasValue ? $" {percent:0}%" : "");
        miniDownloadSpeed.Text = running && !paused ? FormatDownloadSpeed(sampledSpeed) : "—";
        miniDownloadProgress.IsIndeterminate = running && !paused && percent == null;
        miniDownloadProgress.Value = percent ?? 0;
        miniDownloadToggle.Content = paused ? "继续" : "暂停";
        miniDownloadToggle.IsEnabled = !pausing && !record.Committing && record.State is OperationState.Running or OperationState.Queued or OperationState.Paused;
        miniDownload.ToolTip = record.Committing ? "正在完成安装，请稍候" : paused ? "点击继续下载" : "点击暂停下载";
        AutomationProperties.SetName(miniDownloadToggle, paused ? "继续下载" : "暂停下载");
    }
    static string FormatDownloadSpeed(double bytes) => bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024):0.0} MB/s" : bytes >= 1024 ? $"{bytes / 1024:0.0} KB/s" : $"{bytes:0} B/s";

    void ToggleMiniDownload()
    {
        var record = MiniDownloadRecord(); if (record == null || !miniDownloadToggle.IsEnabled) return;
        if(record.State==OperationState.Failed)
        {
            if(operations.ChangePending(record.Id,OperationState.Queued)){prefs.DownloadsPaused=false;prefs.PausedDownloadId="";SaveDownloadPause();RefreshMiniDownloads(true);RefreshOperations();}
            return;
        }
        var resume = prefs.DownloadsPaused || record.State == OperationState.Paused;
        bool changed;
        if (resume) changed = record.State == OperationState.Queued || operations.ChangePending(record.Id, OperationState.Queued);
        else changed = operations.Current?.Record.Id == record.Id ? operations.Cancel(pause: true) : operations.ChangePending(record.Id, OperationState.Paused);
        if (!changed) return;
        prefs.DownloadsPaused = !resume; prefs.PausedDownloadId = resume ? "" : record.Id;
        SaveDownloadPause(); RefreshMiniDownloads(true);
    }
    void SaveDownloadPause()
    {
        try { settings.Save(prefs); } catch (Exception ex) { Log("下载暂停状态暂未保存：" + ex.Message); }
    }
}

