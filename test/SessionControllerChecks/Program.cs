using BlockLaunch;
using System.Diagnostics;

if (args.Contains("--fixture"))
{
    Console.WriteLine("fixture ready");
    Console.ReadLine();
    return;
}

void Check(bool condition, string label)
{
    if (!condition)
        throw new Exception(label);
    Console.WriteLine("PASS " + label);
}

var root = Path.Combine(Path.GetTempPath(), "MCUG-controller-" + Guid.NewGuid().ToString("N"));
var view = new FakeRunningView();
var logs = new List<string>();
DiagnosticContext? observed = null;
string? requestedLog = null;
var registry = new RunningGameRegistry();
using var controller = new RunningGamesController(registry, view, new(
    () => false, logs.Add, context => observed = context,
    _ => throw new Exception("Successful exit should not request diagnostics"),
    _ => throw new Exception("Successful exit should not display diagnostics"),
    path => { requestedLog = path; return Task.FromResult("injected log contents"); }));
var start = new ProcessStartInfo(Environment.ProcessPath!)
{
    UseShellExecute = false,
    CreateNoWindow = true,
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true
};
if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
    start.ArgumentList.Add(typeof(FakeRunningView).Assembly.Location);
start.ArgumentList.Add("--fixture");
using var child = new Process { StartInfo = start };
try
{
    var game = registry.Start(root, root, "fixture", Path.Combine(root, "logs"), child, _ => { }, []);
    var observe = controller.ObserveGame(game, new(root, "fixture", game.LogFile));
    controller.ShowRunningGames();
    Check(view.Rows.Single().Id == game.Id && view.ShowCount == 1, "controller renders value rows through a non-WPF port");
    view.RequestLog(game.Id);
    Check(requestedLog == game.LogFile && view.LastLog == "injected log contents", "log content comes from injected reader");
    view.RequestClose(Guid.NewGuid());
    Check(!child.HasExited && logs.Count == 0, "unknown commands do not affect other processes");
    await child.StandardInput.WriteLineAsync("exit");
    await observe.WaitAsync(TimeSpan.FromSeconds(10));
    Check(observed?.Exit?.Code == 0 && view.Rows.Count == 0 && !controller.HasRunningGames, "exit observation updates plain state and releases session");
    controller.Dispose();
    Check(view.CloseSubscribers == 0 && view.LogSubscribers == 0, "controller detaches view commands on disposal");
    Console.WriteLine("ALL_SESSION_CONTROLLER_CHECKS_PASSED (no WPF reference)");
}
finally
{
    try
    {
        if (!child.HasExited)
        {
            child.Kill(true);
            await child.WaitForExitAsync();
        }
    }
    catch (InvalidOperationException) { }
    if (Directory.Exists(root))
        Directory.Delete(root, true);
}

sealed class FakeRunningView : IRunningGamesView
{
    public event Action<Guid>? CloseRequested;
    public event Action<Guid>? LogRequested;
    public int CloseSubscribers => CloseRequested?.GetInvocationList().Length ?? 0;
    public int LogSubscribers => LogRequested?.GetInvocationList().Length ?? 0;
    public IReadOnlyList<RunningGameRow> Rows = [];
    public int ShowCount;
    public string? LastLog;
    public void Render(IReadOnlyList<RunningGameRow> rows) => Rows = rows;
    public void Show() => ShowCount++;
    public void ShowLog(string version, string content) => LastLog = content;
    public Task<bool> ConfirmClose(RunningGameRow game) => Task.FromResult(false);
    public void RequestClose(Guid id) => CloseRequested?.Invoke(id);
    public void RequestLog(Guid id) => LogRequested?.Invoke(id);
}
