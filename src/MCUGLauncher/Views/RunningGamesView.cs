using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

/// <summary>WPF adapter for the session presentation port. It never reads files or controls processes.</summary>
internal sealed class RunningGamesView(Button entry, Action<string, UIElement> showPage, Func<RunningGameRow, Task<bool>> confirmClose) : IRunningGamesView
{
    IReadOnlyList<RunningGameRow> rows = [];
    StackPanel? sessionList;
    public event Action<Guid>? CloseRequested;
    public event Action<Guid>? LogRequested;

    public void Render(IReadOnlyList<RunningGameRow> games)
    {
        rows = games;
        entry.Content = $"运行中的游戏（{games.Count}）";
        entry.ToolTip = "各实例独立运行；在此选择要查看或关闭的游戏";
        entry.Visibility = games.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        RenderRows();
    }

    public void Show()
    {
        sessionList = new StackPanel();
        RenderRows();
        showPage("运行中的游戏", new ScrollViewer { Content = sessionList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    void RenderRows()
    {
        if (sessionList == null)
            return;
        sessionList.Children.Clear();
        if (rows.Count == 0)
            sessionList.Children.Add(FeatureUiElements.Hint("当前没有运行中的游戏。可返回启动页选择其他实例。"));
        foreach (var game in rows)
        {
            var body = new StackPanel();
            body.Children.Add(FeatureUiElements.Heading(game.Version + (game.PlayerName.Length > 0 ? " · " + game.PlayerName : "")));
            body.Children.Add(FeatureUiElements.Hint(game.Directory));
            var actions = new WrapPanel();
            actions.Children.Add(FeatureUiElements.ActionButton("请求关闭此游戏", (_, _) => CloseRequested?.Invoke(game.Id)));
            actions.Children.Add(FeatureUiElements.ActionButton("查看本次日志", (_, _) => LogRequested?.Invoke(game.Id)));
            body.Children.Add(actions);
            sessionList.Children.Add(new Border
            {
                Child = body,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                BorderBrush = Brushes.LightGray
            });
        }
    }

    public Task<bool> ConfirmClose(RunningGameRow game) => confirmClose(game);

    public void ShowLog(string version, string content) => showPage("运行日志 · " + version, new TextBox
    {
        Text = content,
        IsReadOnly = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    });
}
