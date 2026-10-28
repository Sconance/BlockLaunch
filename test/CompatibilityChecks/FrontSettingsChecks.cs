using System.Text.Json;
using BlockLaunch;
using BlockLaunch.Contracts;
using BlockLaunch.Front;

internal static class FrontSettingsChecks
{
    public static async Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCUG-front-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await CheckPersistence(directory);
            await CheckViewModel();
            Console.WriteLine("ALL_FRONT_SETTINGS_CHECKS_PASSED");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static async Task CheckPersistence(string directory)
    {
        var store = new SettingsStore(directory);
        var prefs = new Preferences
        {
            Root = Path.Combine(directory, "original-game"),
            Player = "PreservedPlayer",
            LastVersion = "original-version",
            PreferMua = true,
            DownloadsPaused = true,
            PausedDownloadId = "original-task",
            DarkMode = true,
            Accent = "紫色",
            Launch = new() { Isolation = "独立实例", AutoMemory = false, Server = "example.invalid" }
        };
        store.Save(prefs);
        var running = false;
        var service = new LauncherSettingsService(store, () => running);
        var adapter = new FrontSettingsAdapter(store, service);
        var initial = await adapter.Read(CancellationToken.None);
        Check(initial.Root == prefs.Root && initial.Ram == "4096", "read returns an independent settings snapshot");
        var gameRoot = Path.Combine(directory, "game with spaces 中文");
        Directory.CreateDirectory(Path.Combine(gameRoot, "versions"));
        var input = initial with { Root = Path.Combine(gameRoot, "versions"), Ram = "8192", Width = "1600", Height = "900" };
        var saved = await adapter.Save(input, CancellationToken.None);
        var persisted = store.Load();
        Check(saved.Root == gameRoot && persisted.Root == gameRoot && persisted.Ram == 8192, "save normalizes root and persists editable settings");
        Check(persisted.Launch.RamMb == 8192 && persisted.Launch.Width == 1600 && persisted.Launch.Height == 900, "save synchronizes launch settings");
        Check(persisted.Player == prefs.Player && persisted.LastVersion == prefs.LastVersion && persisted.PreferMua
            && persisted.DownloadsPaused && persisted.PausedDownloadId == prefs.PausedDownloadId && persisted.DarkMode
            && persisted.Accent == prefs.Accent && persisted.Launch.Server == prefs.Launch.Server
            && persisted.Launch.Isolation == prefs.Launch.Isolation && !persisted.Launch.AutoMemory,
            "partial settings save preserves account, version, queue, appearance and advanced launch preferences");
        Check(persisted.GameRoots.Count(root => root.Equals(gameRoot, StringComparison.OrdinalIgnoreCase)) == 1,
            "normalized game root is registered once");
        var reopened = new FrontSettingsAdapter(new SettingsStore(directory), new LauncherSettingsService(new SettingsStore(directory)));
        Check(await reopened.Read(CancellationToken.None) == saved, "fresh adapter reads the same persisted snapshot");
        var latest = store.Load();
        latest.Player = "LatestPlayer";
        latest.Launch.Server = "latest.example.invalid";
        store.Save(latest);
        await adapter.Save(saved, CancellationToken.None);
        Check(store.Load().Player == "LatestPlayer" && store.Load().Launch.Server == "latest.example.invalid" && initial.Ram == "4096",
            "saving an old front snapshot preserves newer unrelated disk settings and leaves old snapshot immutable");
        var settingsFile = Path.Combine(directory, "settings.json");
        var before = File.ReadAllBytes(settingsFile);
        foreach (var invalid in new[]
        {
            saved with { Ram = "511" }, saved with { Ram = "131073" }, saved with { Ram = "not-a-number" },
            saved with { Width = "319" }, saved with { Width = "16385" }, saved with { Height = "239" },
            saved with { Height = "16385" }, saved with { Java = Path.Combine(directory, "missing-java.exe") },
            saved with { Root = "" }, saved with { Root = settingsFile }
        })
        {
            await Fails<Exception>(() => adapter.Save(invalid, CancellationToken.None));
            Check(File.ReadAllBytes(settingsFile).SequenceEqual(before), "invalid input leaves settings file unchanged");
        }
        var javaFile = Path.Combine(directory, "fake java.exe");
        File.WriteAllText(javaFile, "fixture only; never execute");
        var withJava = await adapter.Save(saved with { Java = "\"" + javaFile + "\"", Ram = "512", Width = "320", Height = "240" }, CancellationToken.None);
        Check(withJava.Java == javaFile && store.Load().Launch.Java == javaFile, "existing Java path is unquoted without executing the fixture");
        await adapter.Save(withJava with { Java = "", Ram = "131072", Width = "16384", Height = "16384" }, CancellationToken.None);
        Check(store.Load().Java == "", "empty Java and inclusive maximum bounds are accepted");
        before = File.ReadAllBytes(settingsFile);
        running = true;
        await Fails<InvalidOperationException>(() => adapter.Save(saved, CancellationToken.None));
        var guarded = store.Load();
        var guardedMemory = JsonSerializer.Serialize(guarded);
        await Fails<InvalidOperationException>(() => Task.Run(() => service.Save(guarded, new(
            saved.Root, "", "ChangedPlayer", "4096", "1280", "720", "new-version", false, false))));
        Check(JsonSerializer.Serialize(guarded) == guardedMemory, "direct service calls enforce the same game-running guard without mutating memory");
        Check(File.ReadAllBytes(settingsFile).SequenceEqual(before), "backend rejects saving while game is running");
        running = false;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Fails<OperationCanceledException>(() => adapter.Read(cancelled.Token));
        await Fails<OperationCanceledException>(() => adapter.Save(saved, cancelled.Token));
        Check(File.ReadAllBytes(settingsFile).SequenceEqual(before), "pre-cancelled operations do not persist settings");
        var memory = store.Load();
        var unchanged = JsonSerializer.Serialize(memory);
        var incompatible = store.Load();
        incompatible.SchemaVersion = 99;
        AtomicFile.WriteJson(settingsFile, incompatible);
        var incompatibleBytes = File.ReadAllBytes(settingsFile);
        await Fails<NotSupportedException>(() => Task.Run(() => service.Save(memory, new(
            saved.Root, "", "ChangedPlayer", "4096", "1280", "720", "new-version", false, false))));
        Check(JsonSerializer.Serialize(memory) == unchanged && File.ReadAllBytes(settingsFile).SequenceEqual(incompatibleBytes),
            "failed persistence leaves in-memory settings and newer-schema disk data unchanged");
        await Fails<NotSupportedException>(() => adapter.Read(CancellationToken.None));
    }

    static async Task CheckViewModel()
    {
        var fake = new FakeSettings();
        using var model = new SettingsViewModel(fake);
        await model.Save();
        Check(fake.Saves == 0 && !model.CanSave, "settings cannot save before a successful load");
        await model.Load();
        Check(model.Root == fake.Value.Root && model.CanSave, "view model loads through contract only");
        model.Ram = "8192";
        await model.Save();
        Check(fake.Value.Ram == "8192" && model.Status.Contains("已保存"), "view model submits edited snapshot");
        fake.Error = new IOException("fixture save failure");
        model.Ram = "9000";
        await model.Save();
        Check(model.Status.Contains("fixture save failure") && model.Ram == "9000" && model.CanSave && fake.Value.Ram == "8192",
            "save failure preserves edits and enables retry");
        fake.Error = null;
        await model.Load();
        Check(model.Ram == "8192", "reload discards unsaved edits");
        fake.Error = new IOException("fixture read failure");
        await model.Load();
        Check(!model.CanSave && model.CanReload && model.Status.Contains("读取失败"), "read failure blocks saving and permits reload");
        fake.Error = null;
        await model.Load();
        fake.Pending = new TaskCompletionSource<FrontSettingsSnapshot>();
        var loading = model.Load();
        var reads = fake.Reads;
        await model.Load();
        await model.Save();
        Check(model.IsBusy && !model.CanEdit && fake.Reads == reads, "busy view model prevents overlapping reads and writes");
        var status = model.Status;
        model.Dispose();
        fake.Pending.SetResult(fake.Value with { Root = "late-result" });
        await loading;
        Check(fake.LastToken.IsCancellationRequested && model.Root != "late-result" && model.Status == status && !model.CanReload,
            "closed view model cancels requests and ignores late completion");
    }

    static async Task Fails<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception("Expected " + typeof(T).Name);
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    sealed class FakeSettings : IFrontSettings
    {
        public FrontSettingsSnapshot Value = new("fixture-root", "", "4096", "1280", "720");
        public Exception? Error;
        public int Saves;
        public int Reads;
        public CancellationToken LastToken;
        public TaskCompletionSource<FrontSettingsSnapshot>? Pending;

        public Task<FrontSettingsSnapshot> Read(CancellationToken token)
        {
            Reads++;
            LastToken = token;
            return Pending?.Task ?? (Error == null ? Task.FromResult(Value) : Task.FromException<FrontSettingsSnapshot>(Error));
        }

        public Task<FrontSettingsSnapshot> Save(FrontSettingsSnapshot input, CancellationToken token)
        {
            Saves++;
            if (Error != null)
                return Task.FromException<FrontSettingsSnapshot>(Error);
            Value = input;
            return Task.FromResult(Value);
        }
    }
}
