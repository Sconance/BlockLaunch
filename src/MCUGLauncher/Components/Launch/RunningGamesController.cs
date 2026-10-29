namespace BlockLaunch;

/// <summary>Coordinates session lifetime and commands through a presentation port.</summary>
internal sealed class RunningGamesController : IDisposable
{
    readonly IRunningGamesView view;
    readonly RunningGameActions actions;
    bool disposed;

    internal RunningGamesController(RunningGameRegistry registry, IRunningGamesView view, RunningGameActions actions)
    {
        Registry = registry;
        this.view = view;
        this.actions = actions;
        view.CloseRequested += CloseGame;
        view.LogRequested += ShowGameLog;
    }

    internal RunningGameRegistry Registry
    {
        get;
    }
    internal string? LastGameDirectory
    {
        get; private set;
    }
    internal bool HasRunningGames => Registry.Active.Count > 0;
    bool IsClosed => disposed || actions.IsClosed();

    internal void RefreshRunningGames()
    {
        if (!IsClosed)
            view.Render(Registry.Active.Select(game => new RunningGameRow(game.Id, game.Version, game.Directory)).ToArray());
    }

    internal void ShowRunningGames()
    {
        if (IsClosed)
            return;
        RefreshRunningGames();
        view.Show();
    }

    void CloseGame(Guid id)
    {
        if (IsClosed)
            return;
        var game = Registry.Active.FirstOrDefault(game => game.Id == id);
        if (game == null)
            return;
        try
        {
            if (!game.Session.Completion.IsCompleted && !game.Session.RequestStop())
                actions.Log("未能发送关闭请求，请在对应游戏窗口内正常退出。");
        }
        catch (InvalidOperationException) { RefreshRunningGames(); }
    }

    async void ShowGameLog(Guid id)
    {
        if (IsClosed)
            return;
        var game = Registry.Active.FirstOrDefault(game => game.Id == id);
        if (game == null)
            return;
        string content;
        try
        {
            content = await actions.ReadLog(game.LogFile);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            content = "日志暂不可读：" + ex.Message;
        }
        if (!IsClosed)
            view.ShowLog(game.Version, content);
    }

    internal async Task ObserveGame(RunningGame game, DiagnosticContext context)
    {
        LastGameDirectory = game.Directory;
        actions.SetDiagnosticContext(context);
        RefreshRunningGames();
        try
        {
            var result = await game.Session.Completion;
            if (IsClosed)
                return;
            var completed = context with
            {
                Exit = result
            };
            actions.SetDiagnosticContext(completed);
            actions.Log($"[{game.Version}] " + (result.Requested ? "游戏已按关闭请求退出。" : result.Code == 0 ? "游戏正常退出。" : $"游戏异常退出（代码 {result.Code}）。"));
            if (!result.Requested && result.Code != 0)
            {
                var snapshot = await actions.CaptureDiagnostic(completed);
                if (!IsClosed)
                    actions.ShowDiagnostic(snapshot);
            }
        }
        catch (Exception ex) { if (!IsClosed) actions.Log($"[{game.Version}] 游戏输出观察失败：" + ex.Message); }
        finally
        {
            Registry.Release(game.Id);
            RefreshRunningGames();
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        view.CloseRequested -= CloseGame;
        view.LogRequested -= ShowGameLog;
    }
}
