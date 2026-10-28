using BlockLaunch;
using CmlLib.Core.Auth;

internal static class GameLaunchControllerChecks
{
    public static async Task Run()
    {
        var running = new Interaction { IsGameRunning = true };
        await Fails<InvalidOperationException>(() => new GameLaunchController(running).Launch(default));
        Check(running.Calls.Count == 0, "running game blocks launch before settings or authentication");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = new Interaction();
        await Fails<OperationCanceledException>(() => new GameLaunchController(cancelled).Launch(cancellation.Token));
        Check(cancelled.Calls.Count == 0, "cancelled launch does not touch settings or accounts");

        var changedRoot = new Interaction { SettingsError = new InvalidOperationException("directory changed") };
        await Fails<InvalidOperationException>(() => new GameLaunchController(changedRoot).Launch(default));
        Check(changedRoot.Calls.SequenceEqual(new[] { "settings" }), "settings failure stops authentication and launch");

        var failedAccount = new Interaction { AuthenticationError = new IOException("authentication failed") };
        await Fails<IOException>(() => new GameLaunchController(failedAccount).Launch(default));
        Check(failedAccount.Calls.SequenceEqual(new[] { "settings", "authenticate" }), "authentication failure stops instance access");

        using var http = new HttpClient(new RejectNetwork());
        var root = Path.Combine(Path.GetTempPath(), "MCUG-launch-checks-" + Guid.NewGuid().ToString("N"));
        var instance = Path.Combine(root, "instances", "fixture");
        var settings = new LaunchSettings { Server = "original.example" };
        var selection = new GameLaunchSelection(root, "fixture", false, instance, "1.21.1", root, "", settings, "temporary.example", http);
        var missing = new Interaction { Selection = selection };
        await Fails<IOException>(() => new GameLaunchController(missing).Launch(default));
        Check(!Directory.Exists(root) && missing.Calls.SequenceEqual(new[] { "settings", "authenticate", "selection" }), "uninstalled version cannot create instance files");

        try
        {
            AtomicFile.WriteJson(Path.Combine(root, "versions", "fixture", "fixture.json"), new { id = "fixture", type = "release", libraries = Array.Empty<object>() });
            Directory.CreateDirectory(Path.Combine(instance, "mods"));
            File.WriteAllText(Path.Combine(instance, "mods", "broken.jar"), "synthetic invalid mod archive");
            var rejected = new Interaction { Selection = selection with { IsInstalled = true } };
            await new GameLaunchController(rejected).Launch(default);
            Check(rejected.Calls.Contains("confirm") && !rejected.Calls.Contains("started"), "declined mod preflight never starts a game");
            Check(settings.Server == "original.example", "temporary server override does not change global settings");

            var accepted = new Interaction
            {
                Selection = selection with { IsInstalled = true },
                AcceptPreflight = true,
                Identity = new(MSession.CreateOfflineSession("Fixture"), false, "synthetic-client-token",
                    _ => throw new IOException("synthetic metadata failure"))
            };
            await Fails<IOException>(() => new GameLaunchController(accepted).Launch(default));
            Check(accepted.Calls.Contains("confirm") && !accepted.Calls.Contains("started"), "external authentication failure blocks process creation");

            var stop = new Interaction();
            new GameLaunchController(stop).Stop();
            Check(stop.Calls.Count == 0, "stop without a game session is harmless");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    static async Task Fails<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException("Expected " + typeof(TException).Name);
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    sealed class Interaction : IGameLaunchInteraction
    {
        public bool IsGameRunning { get; init; }
        public GameSession? CurrentSession => null;
        public List<string> Calls { get; } = new();
        public Exception? SettingsError { get; init; }
        public Exception? AuthenticationError { get; init; }
        public GameLaunchSelection Selection { get; init; } = null!;
        public AccountSession Identity { get; init; } = new(MSession.CreateOfflineSession("Fixture"), true);
        public bool AcceptPreflight { get; init; }

        public Task SaveSettings()
        {
            Calls.Add("settings");
            return SettingsError == null ? Task.CompletedTask : Task.FromException(SettingsError);
        }

        public Task<AccountSession> Authenticate()
        {
            Calls.Add("authenticate");
            return AuthenticationError == null ? Task.FromResult(Identity) : Task.FromException<AccountSession>(AuthenticationError);
        }

        public GameLaunchSelection CaptureSelection()
        {
            Calls.Add("selection");
            return Selection;
        }

        public bool ConfirmPreflight(ModScan scan)
        {
            Calls.Add("confirm");
            return AcceptPreflight;
        }

        public void Report(string message) => Calls.Add("report");
        public void QueueOutput(string line) => throw new InvalidOperationException("Unexpected game output");
        public void Started(GameLaunchResult result) => Calls.Add("started");
        public void Log(string message) => Calls.Add("log");
    }

    sealed class RejectNetwork : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            throw new InvalidOperationException("Launch checks must not access the network");
    }
}
