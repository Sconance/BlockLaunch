using BlockLaunch;
using CmlLib.Core.Auth;

internal static class BusinessServiceChecks
{
    public static async Task Run()
    {
        var settingsDirectory = Path.Combine(Path.GetTempPath(), "MCUG-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(settingsDirectory);
        try
        {
            var store = new SettingsStore(settingsDirectory);
            var preferences = new Preferences();
            var service = new LauncherSettingsService(store);
            service.Save(preferences, new(preferences.Root, "", "Tester", "4096", "1280", "720", "1.21.1", false, false));
            Check(preferences.Ram == 4096 && preferences.Width == 1280 && preferences.LastVersion == "1.21.1", "settings service applies validated snapshot");
            Check(store.Load().LastVersion == "1.21.1" && preferences.Launch.RamMb == preferences.Ram, "settings persist and synchronize launch settings");
            try
            {
                service.Save(preferences, new(preferences.Root, "", "Tester", "128", "1280", "720", "", false, false));
                throw new Exception("invalid memory accepted");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("PASS settings service rejects invalid memory");
            }

            Check(preferences.Ram == 4096 && store.Load().Ram == 4096, "invalid settings do not mutate or persist preferences");
            service.SaveNetwork(preferences, new() { ProxyMode = "直连", DownloadSource = "仅官方", LimitKibPerSecond = 128 });
            Check(preferences.Network.ProxyMode == "直连" && store.Load().Network.LimitKibPerSecond == 128, "shared network settings persist before updating memory");
            service.SaveAppearance(preferences, true, "青绿", "", 0.3, "完整显示", ["one", "one", "two"]);
            Check(preferences.DarkMode && store.Load().Accent == "青绿" && preferences.BackgroundImages.Count == 2, "shared appearance settings persist and deduplicate history");
            service.SaveBackupPolicy(preferences, false, 7);
            Check(!preferences.ProtectSaves && store.Load().SaveBackupCount == 7, "shared backup policy persists without overwriting other settings");
            var globalLaunch = new LaunchSettings { AutoMemory = false, RamMb = 3072, Width = 1024, Height = 768 };
            service.SaveGlobalLaunch(preferences, globalLaunch);
            globalLaunch.RamMb = 8192;
            Check(preferences.Ram == 3072 && preferences.Launch.RamMb == 3072 && store.Load().Width == 1024,
                "global launch settings detach input and synchronize legacy preference fields");
            var settingsFile = Path.Combine(settingsDirectory, "settings.json");
            var validBytes = File.ReadAllBytes(settingsFile);
            foreach (var invalid in new Action[]
            {
                () => service.SaveAppearance(preferences, false, "青绿", "", double.NaN, "完整显示", []),
                () => service.SaveAppearance(preferences, false, "unknown", "", 0.3, "完整显示", []),
                () => service.SaveAppearance(preferences, false, "蓝色", "", 0.3, "invalid", []),
                () => service.SaveBackupPolicy(preferences, true, 0),
                () => service.SaveDownloadLimit(preferences, -1)
            })
            {
                try { invalid(); throw new Exception("invalid advanced settings accepted"); }
                catch (ArgumentException) { }
            }
            Check(validBytes.SequenceEqual(File.ReadAllBytes(settingsFile)) && preferences.DarkMode && preferences.SaveBackupCount == 7,
                "invalid advanced settings leave file and memory unchanged");
            var blocked = new LauncherSettingsService(store, () => true);
            try { blocked.SaveBackupPolicy(preferences, true, 1); throw new Exception("busy settings save accepted"); }
            catch (InvalidOperationException) { }
            Check(validBytes.SequenceEqual(File.ReadAllBytes(settingsFile)), "busy advanced settings are rejected before disk writes");
            var blockedPath = Path.Combine(settingsDirectory, "not-a-directory");
            File.WriteAllText(blockedPath, "fixture");
            try
            {
                new LauncherSettingsService(new SettingsStore(blockedPath)).SaveBackupPolicy(preferences, true, 1);
                throw new Exception("failed persistence accepted");
            }
            catch (IOException) { }
            Check(!preferences.ProtectSaves && preferences.SaveBackupCount == 7, "failed persistence does not update in-memory backup policy");
            var microsoft = new FakeMicrosoft();
            var accounts = new AccountSessionService(microsoft, new FakeMua());
            Check(accounts.Offline("Tester").IsOffline, "account service creates offline session");
            try
            {
                accounts.Offline("bad name");
                throw new Exception("invalid offline name accepted");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("PASS account service rejects invalid offline name");
            }
            microsoft.RequireInteractive = true;
            await accounts.EnsureMicrosoft(CancellationToken.None);
            Check(microsoft.Attempts.SequenceEqual([false, true]), "Microsoft refresh requiring interaction falls back to login");
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var attemptCount = microsoft.Attempts.Count;
            try
            {
                await accounts.EnsureMicrosoft(cancelled.Token);
                throw new Exception("cancelled login accepted");
            }
            catch (OperationCanceledException)
            {
                Check(microsoft.Attempts.Count == attemptCount, "cancelled login never invokes provider");
            }
            var monitor = new GameSessionMonitor();
            var diagnosticDirectory = Path.Combine(settingsDirectory, "game");
            Directory.CreateDirectory(diagnosticDirectory);
            var context = new DiagnosticContext(diagnosticDirectory, "fixture");
            Check(GameLogReader.ReadLatest(diagnosticDirectory).Contains("尚无"), "missing latest.log returns an explicit empty state");
            var logDirectory = Path.Combine(diagnosticDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            File.WriteAllText(Path.Combine(logDirectory, "latest.log"), new string('x', 600 * 1024) + "\nfixture-log-tail\n");
            var tail = GameLogReader.ReadLatest(diagnosticDirectory);
            Check(tail.Contains("512 KiB") && tail.EndsWith("fixture-log-tail\n") && tail.Length < 512 * 1024,
                "latest.log reader bounds large files and retains complete trailing lines");
            File.Delete(Path.Combine(logDirectory, "latest.log"));
            var normal = await monitor.Observe(Task.FromResult(new GameExit(0, false)), context);
            Check(normal.Context.Exit?.Code == 0 && normal.Diagnostic == null, "normal exit does not generate crash diagnostics");
            var requested = await monitor.Observe(Task.FromResult(new GameExit(1, true)), context);
            Check(requested.Diagnostic == null, "requested nonzero exit does not generate crash diagnostics");
            File.WriteAllText(Path.Combine(diagnosticDirectory, "latest.log"), "fixture crash log");
            var exited = false;
            var crashed = await monitor.Observe(Task.FromResult(new GameExit(1, false)), context, _ => exited = true);
            Check(exited && crashed.Diagnostic != null && crashed.Context.Exit?.Code == 1, "unexpected exit notifies host and generates local diagnostics");
            try
            {
                await monitor.Observe(new TaskCompletionSource<GameExit>().Task, context, token: cancelled.Token);
                throw new Exception("cancelled monitor accepted");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("PASS game session monitoring can be cancelled without stopping game");
            }
        }
        finally
        {
            Directory.Delete(settingsDirectory, true);
        }
    }

    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    sealed class FakeMicrosoft : IMicrosoftAccountProvider
    {
        public List<bool> Attempts { get; } = [];
        public bool RequireInteractive { get; set; }
        public SavedMicrosoftAccount? ReadSavedAccount() => null;
        public Task<MSession> Authenticate(bool interactive, CancellationToken token)
        {
            Attempts.Add(interactive);
            return RequireInteractive && !interactive
                ? Task.FromException<MSession>(new AccountLoginException("fixture", true))
                : Task.FromResult(MSession.CreateOfflineSession("Tester"));
        }
        public void Forget() { }
    }

    sealed class FakeMua : IMuaAccountProvider
    {
        public MuaCredential? Read() => null;
        public Task<MuaCredential> Login(string account, string password, Func<IReadOnlyList<MuaProfile>, Task<MuaProfile?>> select, CancellationToken token) => throw new NotSupportedException();
        public Task<MuaCredential> Ensure(bool forceRefresh, CancellationToken token) => throw new NotSupportedException();
        public Task<bool> Forget(CancellationToken token) => Task.FromResult(true);
        public Task<MuaMetadata> Metadata(CancellationToken token) => throw new NotSupportedException();
        public MSession CreateSession(MuaCredential credential) => MSession.CreateOfflineSession(credential.Profile.Name);
    }
}
