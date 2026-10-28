namespace BlockLaunch;

public sealed class GameSessionMonitor
{
    public Task<GameSessionMonitorResult> Observe(GameSession session, DiagnosticContext context, Action<DiagnosticContext>? exited = null, CancellationToken token = default) =>
        Observe(session.Completion, context, exited, token);

    public async Task<GameSessionMonitorResult> Observe(Task<GameExit> completion, DiagnosticContext context, Action<DiagnosticContext>? exited = null, CancellationToken token = default)
    {
        var exit = await completion.WaitAsync(token);
        var completed = context with { Exit = exit };
        exited?.Invoke(completed);
        var diagnostic = exit.Requested || exit.Code == 0 ? null : await Task.Run(() => Diagnostics.Capture(completed, token), token);
        return new(completed, diagnostic);
    }
}

public sealed record GameSessionMonitorResult(DiagnosticContext Context, DiagnosticSnapshot? Diagnostic);
