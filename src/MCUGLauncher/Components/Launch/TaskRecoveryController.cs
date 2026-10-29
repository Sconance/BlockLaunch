using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace BlockLaunch;
internal sealed class TaskRecoveryController
{
    readonly LauncherWorkspace workspace;
    internal TaskRecoveryController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal readonly ConcurrentQueue<string> pendingGameLines = new();
    internal DispatcherTimer? outputTimer;
    internal static string OperationName(string method) => method switch
    {
        "Refresh" => "刷新版本",
        "Install" => "安装 / 修复游戏",
        "Launch" => "启动游戏",
        "Login" => "微软登录",
        "InstallFabric" => "安装 Fabric",
        "SearchContent" => "搜索内容",
        "StartupRecovery" => "启动恢复",
        _ when method.Contains("Quilt") => "安装 Quilt",
        _ when method.Contains("ProjectChanged") => "读取内容版本",
        _ when method.Contains("InstallContent") => "安装所选内容",
        _ when method.Contains("ImportPack") => "导入整合包",
        _ when method.Contains("SaveClick") => "保存设置与迁移",
        _ when method.Contains("UpdateContent") => "更新内容",
        _ => "内容与设置操作"
    };
    internal void ApplyOperation(OperationRecord record)
    {
        var active = workspace.LaunchCoordinator.operations.Current;
        if (active == null || active.Record.Id != record.Id)
            return;
        if (record.Download != null)
            workspace.Window.TaskStatusBar.Visibility = Visibility.Collapsed;
        workspace.Window.Status.Text = record.Committing ? "正在提交安装结果，此阶段暂不可取消…" : $"{record.Name} · {record.Message}";
        workspace.Window.Progress.IsIndeterminate = record.Percent == null && record.State is OperationState.Running or OperationState.Cancelling or OperationState.Pausing;
        if (record.Percent is { } percent)
            workspace.Window.Progress.Value = Math.Clamp(percent, 0, 100);
        workspace.Window.CancelTaskButton.IsEnabled = record.State == OperationState.Running && !record.Committing;
    }

    internal async Task StartupRecovery()
    {
        var messages = await Task.Run(() =>
        {
            var result = InstallationRecovery.Recover(workspace.LaunchCoordinator.prefs.Root);
            result.AddRange(new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).MigrateLegacy());
            return result;
        }, workspace.LaunchCoordinator.CurrentToken);
        foreach (var message in messages)
            workspace.LaunchCoordinator.Log(message);
        var interrupted = workspace.LaunchCoordinator.operations.History().Count(r => r.State == OperationState.Interrupted);
        if (interrupted > 0)
            workspace.LaunchCoordinator.Log($"识别到 {interrupted} 个上次中断的任务，可在「任务与恢复」中查看。");
        workspace.LaunchCoordinator.CurrentToken.ThrowIfCancellationRequested();
        await workspace.LaunchCoordinator.Refresh();
        RefreshOperations();
    }

    internal void EnsureOutputTimer()
    {
        if (outputTimer != null)
            return;
        outputTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => DrainGameLines(), workspace.Window.Dispatcher);
        outputTimer.Start();
        workspace.Window.Closed += (_, _) => outputTimer.Stop();
    }

    internal async Task InstallTracked(string version)
    {
        await VersionInstallation.Run(workspace.LaunchCoordinator.prefs.Root, version, token => workspace.LaunchCoordinator.launcher.InstallAsync(version, token).AsTask(), workspace.LaunchCoordinator.CurrentToken);
    }

    internal void CancelTaskClick(object s, RoutedEventArgs e)
    {
        if (!workspace.LaunchCoordinator.operations.Cancel())
            workspace.Window.Status.Text = "当前正在提交文件，完成后可以继续操作。";
    }

    internal void RefreshOperations()
    {
        if (workspace.Controls.TaskHistoryList == null)
            return;
        var id = (workspace.Controls.TaskHistoryList.SelectedItem as OperationRecord)?.Id;
        var records = workspace.LaunchCoordinator.operations.History().OrderBy(r => r.Id == workspace.LaunchCoordinator.operations.Current?.Record.Id ? 0 : r.State == OperationState.Queued ? 1 : 2).ThenBy(r => r.State == OperationState.Queued ? (r.QueueOrder == 0 ? r.Started.UtcTicks : r.QueueOrder) : 0).ThenByDescending(r => r.Started).ToList();
        workspace.Controls.TaskHistoryList.ItemsSource = records;
        workspace.Controls.TaskHistoryList.SelectedItem = records.FirstOrDefault(r => r.Id == id);
    }

    internal void RefreshOperationsClick(object s, RoutedEventArgs e) => RefreshOperations();
    internal void LocateOperationClick(object s, RoutedEventArgs e)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        if (workspace.Controls.TaskHistoryList.SelectedItem is not OperationRecord record)
            return;
        if (record.Download is { } request)
        {
            InlineMessageBox.Show($"任务：{record.Name}\n状态：{record.StateLabel}\n游戏目录：{record.Root}\n目标：{request.Destination}\n\n选择任务后点击「继续 / 失败重试」。恢复始终使用任务保存的目录和版本。取消保留已校验缓存，不会移除已安装内容。", "下载任务");
            return;
        }

        if (!Path.IsPathFullyQualified(record.Root) || !Path.GetFullPath(record.Root).Equals(Path.GetFullPath(workspace.LaunchCoordinator.prefs.Root), StringComparison.OrdinalIgnoreCase))
        {
            InlineMessageBox.Show("此任务属于其他游戏根目录，请先在设置页切换目录。");
            return;
        }

        if (workspace.LaunchCoordinator.rows.Any(r => r.Id == record.Target))
        {
            workspace.Controls.Versions.SelectedValue = record.Target;
            workspace.Controls.HomeNav.IsChecked = true;
        }

        InlineMessageBox.Show("游戏下载任务：选择原版本后再次下载 / 修复。\n整合包任务：重新导入原文件或再次安装项目。\n内容任务：再次安装原项目；启动时会检查并回滚可恢复的未提交事务。\n下载任务可在此页暂停、继续或失败重试；支持续传的资源会保留部分文件，游戏安装重新执行时会校验已有文件。", "恢复说明");
    }

    internal async void NewInstanceClick(object s, RoutedEventArgs e) => await workspace.LaunchCoordinator.Run(async () =>
    {
        workspace.ResourceBrowserController.RequireStopped();
        var target = workspace.ResourceBrowserController.Target();
        if (!workspace.LaunchCoordinator.LocalVersions().Contains(workspace.LaunchCoordinator.Selected()))
            throw new IOException("请先完成所选版本安装。");
        var key = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).Create(target.GameVersion + " 独立实例 " + DateTime.Now.ToString("HH:mm:ss"), workspace.ModpackController.SelectedLaunchVersion(), target.GameVersion);
        await workspace.LaunchCoordinator.Refresh();
        workspace.Controls.Versions.SelectedValue = key;
        workspace.LaunchCoordinator.Log("已建立新实例，配置、模组与存档均独立；未复制原实例内容。");
    }, "新建独立实例");
    internal void QueueGameLine(string line)
    {
        pendingGameLines.Enqueue(line);
        while (pendingGameLines.Count > 3000)
            pendingGameLines.TryDequeue(out _);
    }

    internal void DrainGameLines()
    {
        var lines = new List<string>();
        for (var i = 0; i < 150 && pendingGameLines.TryDequeue(out var line); i++)
            lines.Add(line);
        if (lines.Count > 0)
            workspace.LaunchCoordinator.Log(string.Join(Environment.NewLine, lines));
    }

    internal async Task ObserveGame(GameSession session, DiagnosticContext context)
    {
        try
        {
            var result = await session.Completion;
            if (workspace.LaunchCoordinator.gameSession == session)
                workspace.Controls.StopGameButton.Visibility = Visibility.Collapsed;
            var completedContext = context with
            {
                Exit = result
            };
            if (workspace.LaunchCoordinator.gameSession == session)
                workspace.DiagnosticsController.recentDiagnosticContext = completedContext;
            workspace.LaunchCoordinator.Log(result.Requested ? $"游戏已按关闭请求退出（代码 {result.Code}）。" : result.Code == 0 ? "游戏正常退出（代码 0）。" : $"游戏异常退出（代码 {result.Code}），正在生成本地诊断。");
            if (!result.Requested && result.Code != 0)
            {
                var snapshot = await Task.Run(() => Diagnostics.Capture(completedContext));
                if (workspace.LaunchCoordinator.gameSession != session)
                    return;
                workspace.DiagnosticsController.ShowDiagnosticSnapshot(snapshot);
                workspace.Window.Status.Text = "游戏异常退出，可在「诊断与反馈」查看日志证据和建议。";
                workspace.LaunchCoordinator.Log($"诊断已完成：命中 {snapshot.Findings.Count} 条可能原因，请在「诊断与反馈」查看证据。未上传任何日志。");
            }
        }
        catch (Exception ex)
        {
            workspace.LaunchCoordinator.Log("游戏输出观察失败：" + ex.Message);
        }
    }

    internal async void ReadGameLogClick(object s, RoutedEventArgs e)
    {
        try
        {
            var directory = workspace.LaunchCoordinator.lastGameDirectory ?? workspace.LaunchCoordinator.Instance();
            workspace.Controls.GameLogView.Text = await Task.Run(() => SharedLogs.ReadTail(Path.Combine(directory, "logs", "latest.log")));
        }
        catch (IOException ex)
        {
            workspace.Controls.GameLogView.Text = "日志暂时不可读，可以稍后刷新：" + ex.Message;
        }
    }

    internal void LogsFolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            workspace.LaunchCoordinator.OpenFolder(Path.Combine(workspace.LaunchCoordinator.lastGameDirectory ?? workspace.LaunchCoordinator.Instance(), "logs"));
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }
}
