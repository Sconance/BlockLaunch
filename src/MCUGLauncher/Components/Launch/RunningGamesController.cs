using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Displays and observes sessions through scoped UI actions; never owns launcher settings.</summary>
internal sealed class RunningGamesController(
    Button entry, Func<bool> isClosed, Action<string> log, Action<string, UIElement> showPage,
    Action<DiagnosticContext> setDiagnosticContext, Action<DiagnosticSnapshot> showDiagnostic)
{
    internal RunningGameRegistry Registry { get; } = new();
    internal string? LastGameDirectory
    {
        get; private set;
    }
    internal bool HasRunningGames => Registry.Active.Count > 0;
    StackPanel? sessionList;

    internal void RefreshRunningGames()
    {
        if (isClosed())
            return;
        var active = Registry.Active;
        entry.Content = $"运行中的游戏（{active.Count}）";
        entry.ToolTip = "各实例独立运行；在此选择要查看或关闭的游戏";
        entry.Visibility = active.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (sessionList == null)
            return;
        sessionList.Children.Clear();
        if (active.Count == 0)
            sessionList.Children.Add(FeatureUiElements.Hint("当前没有运行中的游戏。可返回启动页选择其他实例。"));
        foreach (var game in active)
        {
            var body = new StackPanel();
            body.Children.Add(FeatureUiElements.Heading(game.Version));
            body.Children.Add(FeatureUiElements.Hint(game.Directory));
            var actions = new WrapPanel();
            actions.Children.Add(FeatureUiElements.ActionButton("请求关闭此游戏", (_, _) =>
            {
                try
                {
                    if (!game.Session.Completion.IsCompleted && !game.Session.RequestStop())
                        log("未能发送关闭请求，请在对应游戏窗口内正常退出。");
                }
                catch (InvalidOperationException) { RefreshRunningGames(); }
            }));
            actions.Children.Add(FeatureUiElements.ActionButton("查看本次日志", (_, _) =>
            {
                string content;
                try
                {
                    content = SharedLogs.ReadTail(game.LogFile);
                }
                catch (System.IO.IOException ex) { content = "日志暂不可读：" + ex.Message; }
                var text = new TextBox
                {
                    Text = content,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                showPage("运行日志 · " + game.Version, text);
            }));
            body.Children.Add(actions);
            sessionList.Children.Add(new Border
            {
                Child = body,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                BorderBrush = System.Windows.Media.Brushes.LightGray
            });
        }
    }

    internal void ShowRunningGames()
    {
        sessionList = new StackPanel();
        RefreshRunningGames();
        showPage("运行中的游戏", new ScrollViewer { Content = sessionList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    internal async Task ObserveGame(RunningGame game, DiagnosticContext context)
    {
        LastGameDirectory = game.Directory;
        setDiagnosticContext(context);
        RefreshRunningGames();
        try
        {
            var result = await game.Session.Completion;
            if (isClosed())
                return;
            var completed = context with
            {
                Exit = result
            };
            setDiagnosticContext(completed);
            log($"[{game.Version}] " + (result.Requested ? "游戏已按关闭请求退出。" : result.Code == 0 ? "游戏正常退出。" : $"游戏异常退出（代码 {result.Code}）。"));
            if (!result.Requested && result.Code != 0)
            {
                var snapshot = await Task.Run(() => Diagnostics.Capture(completed));
                if (!isClosed())
                    showDiagnostic(snapshot);
            }
        }
        catch (Exception ex) { log($"[{game.Version}] 游戏输出观察失败：" + ex.Message); }
        finally
        {
            Registry.Release(game.Id);
            RefreshRunningGames();
        }
    }
}
