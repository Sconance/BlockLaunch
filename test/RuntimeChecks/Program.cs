using BlockLaunch;
using System.Diagnostics;
using System.IO;
using System.Windows;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.FirstOrDefault() == "--game-fixture")
        {
            Console.WriteLine(args[1] + " stdout access_token=runtime-fixture-secret");
            Console.Error.WriteLine(args[1] + " stderr");
            var app = new Application();
            var window = new Window { Title = args[1], Width = 320, Height = 180 };
            window.Content = new System.Windows.Controls.TextBlock { Text = "Isolated game-process fixture", Margin = new Thickness(20) };
            app.Run(window);
            Console.WriteLine(args[1] + " closed");
            return;
        }
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
        void Wait(Task task)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            task.ContinueWith(_ => application.Dispatcher.BeginInvoke(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
        }
        try
        {
            Wait(CheckSessions());
            Wait(CheckCatalog());
        }
        finally { application.Shutdown(); }
        Console.WriteLine("ALL_RUNTIME_CHECKS_PASSED (fixture processes, not Minecraft gameplay)");
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    static Process Child(string label)
    {
        var info = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            info.ArgumentList.Add(typeof(Program).Assembly.Location);
        info.ArgumentList.Add("--game-fixture");
        info.ArgumentList.Add(label);
        return new Process { StartInfo = info };
    }

    static async Task CheckSessions()
    {
        var root = Path.Combine(Path.GetTempPath(), "MCUG-runtime-" + Guid.NewGuid().ToString("N"));
        var entry = new System.Windows.Controls.Button();
        UIElement? displayed = null;
        var controller = new RunningGamesController(entry, () => false, _ => { },
            (_, page) => displayed = page, _ => { }, _ => { });
        var registry = controller.Registry;
        using var first = Child("fixture-A");
        using var second = Child("fixture-B");
        try
        {
            var a = registry.Start(root, Path.Combine(root, "A"), "version-A", Path.Combine(root, "logs"), first, _ => { }, ["runtime-fixture-secret"]);
            var b = registry.Start(root, Path.Combine(root, "B"), "version-B", Path.Combine(root, "logs"), second, _ => { }, ["runtime-fixture-secret"]);
            var observeA = controller.ObserveGame(a, new(a.Directory, a.Version, a.LogFile));
            var observeB = controller.ObserveGame(b, new(b.Directory, b.Version, b.LogFile));
            controller.ShowRunningGames();
            Check(entry.Visibility == Visibility.Visible && entry.Content!.ToString()!.Contains("2") &&
                Buttons(displayed!).Count(button => Equals(button.Content, "请求关闭此游戏")) == 2,
                "running-game UI exposes both sessions and targeted controls");
            Check(registry.Active.Count == 2 && first.Id != second.Id && a.LogFile != b.LogFile, "two independent sessions and unique logs");
            try
            {
                registry.RequireAvailable(Path.Combine(root, "A") + Path.DirectorySeparatorChar);
                throw new Exception("Duplicate instance accepted");
            }
            catch (IOException) { }
            Check(registry.UsesRoot(root) && registry.UsesDirectory(Path.Combine(root, "A")), "same-directory guard and root tracking");
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && (first.MainWindowHandle == 0 || second.MainWindowHandle == 0))
            {
                await Task.Delay(50);
                first.Refresh();
                second.Refresh();
            }
            Buttons(displayed!).First(button => Equals(button.Content, "请求关闭此游戏"))
                .RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(a.Session.StopRequested, "targeted UI close reaches first window");
            GameExit exit;
            try
            {
                exit = await a.Session.Completion.WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
            {
                throw new Exception($"First session did not complete: first exited={first.HasExited}, second exited={second.HasExited}, log={File.ReadAllText(a.LogFile)}");
            }
            await observeA;
            Check(entry.Content!.ToString()!.Contains("1"), "running-game entry updates after first exit");
            Check(exit.Requested && registry.Active.Count == 1 && !second.HasExited, "closing first session leaves second running");
            registry.RequireAvailable(Path.Combine(root, "A"));
            Check(b.Session.RequestStop(), "second session closes independently");
            await b.Session.Completion.WaitAsync(TimeSpan.FromSeconds(15));
            await observeB;
            Check(entry.Visibility == Visibility.Collapsed, "running-game entry hides after last exit");
            Check(registry.Active.Count == 0 && !File.ReadAllText(a.LogFile).Contains("runtime-fixture-secret") &&
                File.ReadAllText(a.LogFile).Contains("fixture-A") && !File.ReadAllText(a.LogFile).Contains("fixture-B"), "session cleanup and isolated redacted output");
            using var missing = new Process { StartInfo = new ProcessStartInfo(Path.Combine(root, "missing.exe")) { UseShellExecute = false } };
            try
            {
                registry.Start(root, root, "bad", Path.Combine(root, "logs"), missing, _ => { }, []);
            }
            catch (System.ComponentModel.Win32Exception) { }
            Check(registry.Active.Count == 0, "failed process start leaves no running entry");
        }
        finally
        {
            // Only terminate fixture processes started by this test if an assertion failed.
            foreach (var process in new[] { first, second })
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        await process.WaitForExitAsync();
                    }
                }
                catch (InvalidOperationException) { }
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    static IEnumerable<System.Windows.Controls.Button> Buttons(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is System.Windows.Controls.Button button)
                yield return button;
            foreach (var nested in Buttons(child))
                yield return nested;
        }
    }

    static async Task CheckCatalog()
    {
        var root = "A";
        var calls = 0;
        var requests = new List<TaskCompletionSource<List<VersionRow>>>();
        var applied = new List<string>();
        using var catalog = new VersionCatalogController(() => root, _ =>
        {
            calls++;
            var request = new TaskCompletionSource<List<VersionRow>>(TaskCreationOptions.RunContinuationsAsynchronously);
            requests.Add(request);
            return request.Task;
        }, rows => applied.Add(rows.Single().Id), () => { }, _ => { });
        Check(calls == 0, "catalog construction makes no network requests");
        var old = catalog.EnsureVersionCatalog();
        var same = catalog.EnsureVersionCatalog();
        Check(calls == 1 && ReferenceEquals(old, same), "concurrent catalog requests coalesce");
        root = "B";
        var current = catalog.EnsureVersionCatalog();
        requests[1].SetResult([new("current", "", "release")]);
        await current;
        requests[0].SetResult([new("stale", "", "release")]);
        await old;
        await catalog.EnsureVersionCatalog();
        Check(calls == 2 && applied.SequenceEqual(new[] { "current" }), "stale-root response discarded and successful catalog cached");
        var failure = catalog.EnsureVersionCatalog(true);
        requests[2].SetException(new IOException("fixture unavailable"));
        await failure;
        Check(catalog.CatalogError != null && !catalog.CatalogLoading, "catalog failure exposes retry state without blocking local UI");
        var retry = catalog.EnsureVersionCatalog(true);
        requests[3].SetResult([new("retry", "", "release")]);
        await retry;
        Check(catalog.CatalogError == null && applied.Last() == "retry", "catalog retry succeeds");
        using var stalled = new VersionCatalogController(() => root,
            _ => new TaskCompletionSource<List<VersionRow>>().Task,
            _ => throw new Exception("Timed-out rows applied"), () => { }, _ => { }, TimeSpan.FromMilliseconds(30));
        await stalled.EnsureVersionCatalog().WaitAsync(TimeSpan.FromSeconds(3));
        Check(stalled.CatalogError != null && !stalled.CatalogLoading, "catalog timeout bounds a transport that ignores cancellation");
    }
}
