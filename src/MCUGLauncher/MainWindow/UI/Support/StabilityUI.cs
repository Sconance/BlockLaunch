using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly ConcurrentQueue<string> pendingGameLines = new();
    DispatcherTimer? outputTimer;

    static string OperationName(string method) => method switch
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

    void ApplyOperation(OperationRecord record)
    {
        var active = operations.Current;
        if (active == null || active.Record.Id != record.Id)
            return;
        if (record.Download != null)
            TaskStatusBarControl.Visibility = Visibility.Collapsed;
        OperationStatusLabel.Text = record.Committing ? "正在提交安装结果，此阶段暂不可取消…" : $"{record.Name} · {record.Message}";
        TaskProgressBarControl.IsIndeterminate = record.Percent == null && record.State is OperationState.Running or OperationState.Cancelling or OperationState.Pausing;
        if (record.Percent is
            {
            } percent)
            TaskProgressBarControl.Value = Math.Clamp(percent, 0, 100);
        CancelTaskButtonControl.IsEnabled = record.State == OperationState.Running && !record.Committing;
    }

    async Task StartupRecovery()
    {
        var messages = await Task.Run(() =>
        {
            var result = InstallationRecovery.Recover(prefs.Root);
            result.AddRange(new InstanceCatalog(prefs.Root).MigrateLegacy());
            return result;
        }, CurrentToken);
        foreach (var message in messages)
            Log(message);
        var interrupted = operations.History().Count(r => r.State == OperationState.Interrupted);
        if (interrupted > 0)
            Log($"识别到 {interrupted} 个上次中断的任务，可在「任务与恢复」中查看。");
        CurrentToken.ThrowIfCancellationRequested();
        await Refresh();
        RefreshOperations();
    }

    void EnsureOutputTimer()
    {
        if (outputTimer != null)
            return;
        outputTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => DrainGameLines(), Dispatcher);
        outputTimer.Start();
        Closed += (_, _) => outputTimer.Stop();
    }

    async Task InstallTracked(string version)
    {
        await VersionInstallation.Run(prefs.Root, version, token => launcher.InstallAsync(version, token).AsTask(), CurrentToken);
    }

    void CancelTaskClick(object s, RoutedEventArgs e)
    {
        if (!operations.Cancel())
            OperationStatusLabel.Text = "当前正在提交文件，完成后可以继续操作。";
    }

    void RefreshOperations()
    {
        if (TaskHistoryListControl == null)
            return;
        var id = (TaskHistoryListControl.SelectedItem as OperationRecord)?.Id;
        var records = operations.History().OrderBy(r => r.Id == operations.Current?.Record.Id ? 0 : r.State == OperationState.Queued ? 1 : 2).ThenBy(r => r.State == OperationState.Queued ? (r.QueueOrder == 0 ? r.Started.UtcTicks : r.QueueOrder) : 0).ThenByDescending(r => r.Started).ToList();
        TaskHistoryListControl.ItemsSource = records;
        TaskHistoryListControl.SelectedItem = records.FirstOrDefault(r => r.Id == id);
    }

    void RefreshOperationsClick(object s, RoutedEventArgs e) => RefreshOperations();

    void LocateOperationClick(object s, RoutedEventArgs e)
    {
        if (busy)
            return;
        if (TaskHistoryListControl.SelectedItem is not OperationRecord record)
            return;
        if (record.Download is
            {
            } request)
        {
            InlineMessageBox.Show($"任务：{record.Name}\n状态：{record.StateLabel}\n游戏目录：{record.Root}\n目标：{request.Destination}\n\n选择任务后点击「继续 / 失败重试」。恢复始终使用任务保存的目录和版本。取消保留已校验缓存，不会移除已安装内容。", "下载任务");
            return;
        }

        if (!Path.IsPathFullyQualified(record.Root) || !Path.GetFullPath(record.Root).Equals(Path.GetFullPath(prefs.Root), StringComparison.OrdinalIgnoreCase))
        {
            InlineMessageBox.Show("此任务属于其他游戏根目录，请先在设置页切换目录。");
            return;
        }

        if (rows.Any(r => r.Id == record.Target))
        {
            VersionSelectorControl.SelectedValue = record.Target;
            HomeNavigation.IsChecked = true;
        }

        InlineMessageBox.Show("游戏下载任务：选择原版本后再次下载 / 修复。\n整合包任务：重新导入原文件或再次安装项目。\n内容任务：再次安装原项目；启动时会检查并回滚可恢复的未提交事务。\n下载任务可在此页暂停、继续或失败重试；支持续传的资源会保留部分文件，游戏安装重新执行时会校验已有文件。", "恢复说明");
    }

    async void NewInstanceClick(object s, RoutedEventArgs e) => await Run(async () =>
    {
        RequireStopped();
        var target = Target();
        if (!LocalVersions().Contains(Selected()))
            throw new IOException("请先完成所选版本安装。");
        var key = new InstanceCatalog(prefs.Root).Create(target.GameVersion + " 独立实例 " + DateTime.Now.ToString("HH:mm:ss"), SelectedLaunchVersion(), target.GameVersion);
        await Refresh();
        VersionSelectorControl.SelectedValue = key;
        Log("已建立新实例，配置、模组与存档均独立；未复制原实例内容。");
    }, "新建独立实例");

    void QueueGameLine(string line)
    {
        pendingGameLines.Enqueue(line);
        while (pendingGameLines.Count > 3000)
            pendingGameLines.TryDequeue(out _);
    }

    void DrainGameLines()
    {
        var lines = new List<string>();
        for (var i = 0; i < 150 && pendingGameLines.TryDequeue(out var line); i++)
            lines.Add(line);
        if (lines.Count > 0)
            Log(string.Join(Environment.NewLine, lines));
    }

    async Task ObserveGame(GameSession session, DiagnosticContext context)
    {
        try
        {
            var result = await session.Completion;
            if (gameSession == session)
                StopGameButtonControl.Visibility = Visibility.Collapsed;
            var completedContext = context with
            {
                Exit = result
            };
            if (gameSession == session)
                recentDiagnosticContext = completedContext;
            Log(result.Requested ? $"游戏已按关闭请求退出（代码 {result.Code}）。" : result.Code == 0 ? "游戏正常退出（代码 0）。" : $"游戏异常退出（代码 {result.Code}），正在生成本地诊断。");
            if (!result.Requested && result.Code != 0)
            {
                var snapshot = await Task.Run(() => Diagnostics.Capture(completedContext));
                if (gameSession != session)
                    return;
                ShowDiagnosticSnapshot(snapshot);
                OperationStatusLabel.Text = "游戏异常退出，可在「诊断与反馈」查看日志证据和建议。";
                Log($"诊断已完成：命中 {snapshot.Findings.Count} 条可能原因，请在「诊断与反馈」查看证据。未上传任何日志。");
            }
        }
        catch (Exception ex)
        {
            Log("游戏输出观察失败：" + ex.Message);
        }
    }

    async void ReadGameLogClick(object s, RoutedEventArgs e)
    {
        try
        {
            var directory = lastGameDirectory ?? Instance();
            GameLogViewControl.Text = await Task.Run(() => SharedLogs.ReadTail(Path.Combine(directory, "logs", "latest.log")));
        }
        catch (IOException ex)
        {
            GameLogViewControl.Text = "日志暂时不可读，可以稍后刷新：" + ex.Message;
        }
    }

    void LogsFolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            OpenFolder(Path.Combine(lastGameDirectory ?? Instance(), "logs"));
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }
}
