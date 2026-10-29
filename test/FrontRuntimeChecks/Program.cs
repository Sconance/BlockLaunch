using System.IO;
using BlockLaunch;
using BlockLaunch.Contracts;
using BlockLaunch.Front;

internal static class Program
{
    [STAThread]
    static void Main(string[] args) => Run(args).GetAwaiter().GetResult();

    static async Task Run(string[] args)
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCUG-front-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new SettingsStore(directory);
        var gameRoot = Path.Combine(directory, "minecraft");
        store.Save(new Preferences { Root = gameRoot, Player = "Fixture", DownloadsPaused = true });
        var coordinator = new OperationCoordinator(Path.Combine(directory, "tasks"));
        var task = coordinator.Enqueue("control fixture", gameRoot, "fixture", new() { Kind = "unsupported-fixture" });
        try
        {
            using (var runtime = new FrontLauncherRuntime(directory))
            {
                if (args.Contains("--ui-structure"))
                {
                    FrontUiChecks.Run(runtime);
                    return;
                }
                try
                {
                    using var other = new FrontLauncherRuntime(directory);
                    throw new Exception("duplicate runtime acquired configuration lock");
                }
                catch (IOException)
                {
                    Console.WriteLine("PASS configuration is exclusive across hosts");
                }
                await runtime.Initialize(CancellationToken.None);
                Check((await runtime.Read(CancellationToken.None)).Root == gameRoot, "runtime reads supplied real configuration");
                runtime.SelectAccount("offline", "Fixture_2");
                Check(runtime.ReadAccounts().OfflineName == "Fixture_2" && store.Load().Player == "Fixture_2", "offline account uses shared account module and persists selection");
                var before = File.ReadAllBytes(Path.Combine(directory, "settings.json"));
                try
                {
                    await runtime.Save(new(gameRoot, "", "invalid", "1280", "720"), CancellationToken.None);
                    throw new Exception("invalid settings were accepted");
                }
                catch (ArgumentException)
                {
                    Check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(directory, "settings.json"))), "invalid settings do not write configuration");
                }
                await runtime.Save(new(gameRoot, "", "2048", "1024", "768"), CancellationToken.None);
                Check(store.Load().Ram == 2048 && store.Load().DownloadsPaused, "settings save preserves queue pause");
                await runtime.Tick();
                Check(coordinator.History().Single().State == OperationState.Queued, "paused persisted task does not execute");
                Check(runtime.Downloads.Change(task.Id, FrontDownloadAction.Cancel).Changed, "Front controls persisted task through shared backend");
                Check(coordinator.History().Single().State == OperationState.Cancelled, "task change persists in configuration queue");
                AtomicFile.WriteJson(Path.Combine(gameRoot, "versions", "fixture", "fixture.json"), new { id = "fixture", type = "release" });
                var instance = runtime.ReadInstance("fixture");
                runtime.SaveInstanceIdentity("fixture", "UI Fixture", "Description", "常用版本", true);
                runtime.SaveInstanceLaunch("fixture", new() { AutoMemory = false, RamMb = 3072 });
                Check(runtime.ReadInstance("fixture").Name == "UI Fixture" && runtime.ReadInstance("fixture").Launch.RamMb == 3072,
                    "runtime version manager persists through shared version service");
                runtime.SaveInstanceLaunch("fixture", null);
                Check(runtime.ReadInstance("fixture").UsesGlobalSettings, "runtime version manager restores global settings");
                var world = Path.Combine(instance.Directory, "saves", "FixtureWorld");
                Directory.CreateDirectory(world);
                File.WriteAllText(Path.Combine(world, "level.dat"), "fixture");
                Check(runtime.ReadWorlds("fixture").Single().Name == "FixtureWorld", "runtime enumerates only valid world directories");
                runtime.SaveAppearance(new(true, "紫罗兰", "", 0.3, "完整显示", []));
                runtime.SaveBackupPolicy(true, 3);
                Check(store.Load().DarkMode && store.Load().SaveBackupCount == 3, "runtime advanced preferences persist through shared settings service");
                var detached = runtime.ReadPreferences();
                detached.Launch.RamMb = 9999;
                Check(runtime.ReadPreferences().Launch.RamMb != 9999, "runtime preferences snapshot cannot mutate live settings");
                await runtime.Shutdown();
            }
            using (var reopened = new FrontLauncherRuntime(directory))
            {
                Check(reopened.ReadAccounts().OfflineName == "Fixture_2", "new host restores account selection");
                await reopened.Shutdown();
            }
            Console.WriteLine("ALL_FRONT_RUNTIME_CHECKS_PASSED");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }
}
