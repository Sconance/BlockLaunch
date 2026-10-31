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
            window.Loaded += (_, _) => Console.WriteLine("fixture-window-ready");
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
            CheckLaunchAndGroupViews();
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

    static void CheckLaunchAndGroupViews()
    {
        var button = new System.Windows.Controls.Button();
        var view = new HomeLaunchButtonView(button, new System.Windows.Controls.TextBlock { Text = "fixture" });
        var body = (System.Windows.Controls.StackPanel)button.Content;
        var progress = body.Children.OfType<System.Windows.Controls.ProgressBar>().Single();
        view.Begin();
        Check(view.IsLaunching && progress.Visibility == Visibility.Visible && progress.IsIndeterminate,
            "launch feedback begins inside the existing button without a global loading page");
        view.Report(42);
        Check(!progress.IsIndeterminate && progress.Value == 42, "button shows known progress without inventing percentages");
        view.End();
        Check(!view.IsLaunching && progress.Visibility == Visibility.Collapsed && !progress.IsIndeterminate,
            "launch feedback clears animation and restores the button");
        var group = new System.Windows.Controls.Expander
        {
            Style = InstanceGroupExpanderStyle.Create(),
            Header = "实例分类",
            IsExpanded = true,
            Content = new System.Windows.Controls.TextBlock { Text = "fixture" }
        };
        group.Measure(new Size(400, 100));
        group.Arrange(new Rect(0, 0, 400, 100));
        group.UpdateLayout();
        IEnumerable<DependencyObject> Visuals(DependencyObject node)
        {
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
                yield return child;
                foreach (var nested in Visuals(child))
                    yield return nested;
            }
        }
        var toggle = Visuals(group).OfType<System.Windows.Controls.Primitives.ToggleButton>().Single();
        Check(toggle.ActualWidth >= 390, "instance group header hit area spans the complete row");
        toggle.IsChecked = false;
        Check(!group.IsExpanded, "full-row group toggle updates expansion state");
        var card = new System.Windows.Controls.Border
        {
            Child = group,
            Padding = new Thickness(12),
            BorderThickness = new Thickness(1),
            BorderBrush = System.Windows.Media.Brushes.SlateGray,
            Background = System.Windows.Media.Brushes.White
        };
        InstanceGroupCardBehavior.Attach(card, group);
        card.Width = 400;
        card.Height = 100;
        card.HorizontalAlignment = HorizontalAlignment.Left;
        card.VerticalAlignment = VerticalAlignment.Top;
        var cardWindow = new Window { Content = card, Width = 440, Height = 160, ShowInTaskbar = false };
        cardWindow.Show();
        card.Measure(new Size(400, 100));
        card.Arrange(new Rect(0, 0, 400, 100));
        card.UpdateLayout();
        void PressRelease(object source)
        {
            foreach (var routed in new[] { UIElement.PreviewMouseLeftButtonDownEvent, UIElement.PreviewMouseLeftButtonUpEvent })
                card.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,
                    Environment.TickCount, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = routed,
                    Source = source
                });
        }
        foreach (var point in new[] { new Point(1, 50), new Point(399, 50), new Point(200, 1), new Point(200, 99) })
        {
            cardWindow.UpdateLayout();
            card.UpdateLayout();
            var hit = card.InputHitTest(point) ?? throw new Exception("Card border/padding has no hit target");
            var expanded = group.IsExpanded;
            PressRelease(hit);
            Check(group.IsExpanded != expanded, "outer rectangle border/padding toggles group: " + point);
        }
        group.IsExpanded = true;
        PressRelease(group.Content);
        Check(group.IsExpanded, "instance content clicks do not collapse the group");
        cardWindow.Close();
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
        var closeApproved = false;
        var view = new RunningGamesView(entry, (_, page) => displayed = page, _ => Task.FromResult(closeApproved));
        using var controller = new RunningGamesController(new RunningGameRegistry(), view,
            new(() => false, _ => { }, _ => { }, context => Task.FromResult(Diagnostics.Capture(context)),
                _ => { }, path => Task.FromResult(SharedLogs.ReadTail(path))));
        var registry = controller.Registry;
        using var first = Child("fixture-A");
        using var second = Child("fixture-B");
        try
        {
            var readyA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var readyB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var a = registry.Start(root, Path.Combine(root, "A"), "version-A", Path.Combine(root, "logs"), first,
                line => { if (line == "fixture-window-ready") readyA.TrySetResult(); }, ["runtime-fixture-secret"], "PlayerA", true);
            var b = registry.Start(root, Path.Combine(root, "A"), "version-B", Path.Combine(root, "logs"), second,
                line => { if (line == "fixture-window-ready") readyB.TrySetResult(); }, ["runtime-fixture-secret"], "PlayerB", true);
            await Task.WhenAll(readyA.Task, readyB.Task).WaitAsync(TimeSpan.FromSeconds(15));
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
            try
            {
                registry.RequireAvailable(Path.Combine(root, "A"), "playera", true);
                throw new Exception("Duplicate offline identity accepted");
            }
            catch (IOException) { }
            registry.RequireAvailable(Path.Combine(root, "A"), "PlayerC", true);
            Check(registry.Active.Select(game => game.PlayerName).SequenceEqual(new[] { "PlayerA", "PlayerB" }),
                "different offline identities share one instance, duplicate identity is rejected");
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && (first.MainWindowHandle == 0 || second.MainWindowHandle == 0))
            {
                await Task.Delay(50);
                first.Refresh();
                second.Refresh();
            }
            Buttons(displayed!).First(button => Equals(button.Content, "请求关闭此游戏"))
                .RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(!a.Session.StopRequested && registry.Active.Count == 2, "cancelled confirmation leaves both games running");
            closeApproved = true;
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
                throw new Exception($"First session did not complete: first exited={first.HasExited}, second exited={second.HasExited}, log={SharedLogs.ReadTail(a.LogFile)}");
            }
            await observeA;
            Check(entry.Content!.ToString()!.Contains("1"), "running-game entry updates after first exit");
            Check(exit.Requested && registry.Active.Count == 1 && !second.HasExited, "closing first session leaves second running");
            registry.RequireAvailable(Path.Combine(root, "A"), "PlayerA", true);
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
