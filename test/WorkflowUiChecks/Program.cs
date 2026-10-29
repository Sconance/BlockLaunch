using BlockLaunch;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var root = Path.GetFullPath("work/v015-component-ui-" + Guid.NewGuid().ToString("N"));
        var settings = Path.Combine(root, "settings");
        var game = Path.Combine(root, "minecraft");
        new SettingsStore(settings).Save(new()
        {
            Root = game
        });
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));
        var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
            Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString())
        };
        var window = new MainWindow(settings)
        {
            Title = "0.15 component tests"
        };
        using var startupHandler = new Handler();
        using var startupHttp = new HttpClient(startupHandler);
        LauncherTestAccess.Set(window, "http", startupHttp);
        LauncherTestAccess.Invoke(window, "SetupLauncher");
        var unexpectedStartupProgress = false;
        var taskStatusBar = (FrameworkElement)LauncherTestAccess.Get(window, "TaskStatusBar");
        taskStatusBar.IsVisibleChanged += (_, _) => unexpectedStartupProgress |= taskStatusBar.IsVisible;
        var startupClock = System.Diagnostics.Stopwatch.StartNew();
        window.Show();

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        var deadline = DateTime.UtcNow.AddMinutes(3);
        bool started = false;
        timer.Tick += async (_, _) =>
        {
            if (started)
                return;
            if ((bool)LauncherTestAccess.Get(window, "busy"))
            {
                if (DateTime.UtcNow > deadline)
                {
                    Environment.ExitCode = 1;
                    app.Shutdown();
                }

                return;
            }

            started = true;
            timer.Stop();
            try
            {
                if (unexpectedStartupProgress)
                    throw new Exception("Startup displayed the removed task progress bar");
                if (startupHandler.Calls != 0)
                    throw new Exception("Local startup unexpectedly requested online metadata");
                var cancelButton = (Button)LauncherTestAccess.Get(window, "CancelTaskButton");
                if (cancelButton.Parent == taskStatusBar || !Equals(cancelButton.Content, "取消当前操作"))
                    throw new Exception("Task cancellation was removed with the progress surface");
                await (Task)LauncherTestAccess.Invoke(window, "Run", [(Func<Task>)(async () =>
                {
                    if (taskStatusBar.Visibility == Visibility.Visible) throw new Exception("Launch operation displayed a progress bar");
                    await Task.Delay(10);
                }), "启动游戏"])!;
                Console.WriteLine("PASS startup/launch progress hidden and task cancellation retained");
                Console.WriteLine($"PASS local startup made zero network requests ({startupClock.ElapsedMilliseconds} ms in isolated fixture)");
                LauncherTestAccess.Set(window, "queueReady", false);
                ((DispatcherTimer?)LauncherTestAccess.Get(window, "downloadTimer"))?.Stop();
                using var http = new HttpClient(new Handler());
                LauncherTestAccess.Set(window, "http", http);
                var instance = Path.Combine(game, "test-instance");
                if (ContentService.ReadManaged(instance).Count == 0)
                    await new ContentService(http).Install(new([Handler.V("old")], new()
                    {
                        ["test"] = "mod"
                    }), instance);
                var stage = 0;
                var errors = new List<Exception>();
                var driver = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                driver.Tick += (_, _) =>
                {
                    try
                    {
                        var dialogs = ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).ToList();
                        if (stage == 0 && dialogs.FirstOrDefault(w => w.Title == "内容更新") is
                            {
                            } updates)
                        {
                            var controls = Descendants(updates).ToList();
                            if (!controls.OfType<CheckBox>().Any(c => c.IsEnabled && c.IsChecked == true))
                                throw new Exception("No selectable update");
                            stage = 1;
                            controls.OfType<Button>().Single(b => Equals(b.Content, "预览所选更新")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        else if (stage == 1 && dialogs.FirstOrDefault(w => w.Title == "更新预览") is
                        {
                        } preview)
                        {
                            var text = string.Join("\n", Descendants(preview).OfType<TextBlock>().Select(t => t.Text));
                            if (!text.Contains("old") || !text.Contains("new") || !text.Contains("mods/test.jar"))
                                throw new Exception("Preview missing old/new/path");
                            Save(preview, "v015-update-preview.png");
                            stage = 2;
                            Descendants(preview).OfType<Button>().Single(b => Equals(b.Content, "确认并加入下载队列")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                        foreach (var dialog in ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).Reverse().ToList())
                            dialog.Close();
                    }
                };
                driver.Start();
                await (Task)LauncherTestAccess.Invoke(window, "OpenContentUpdates", [instance, "fixture", "1.21.1", "fabric", null, null])!;
                driver.Stop();
                if (errors.Count > 0)
                    throw errors[0];
                if (stage != 2)
                    throw new Exception("Workflow incomplete");
                var queue = (OperationCoordinator)LauncherTestAccess.Get(window, "operations");
                var job = queue.History().First(r => r.Download?.PreparedPlan != null);
                if (job.Download!.PreparedPlan!.Versions.Single().Id != "new" || string.IsNullOrEmpty(job.Download.ContentFingerprint))
                    throw new Exception("Queue lost approved plan");
                Console.WriteLine("PASS in-process WPF update selection, preview, confirmation, frozen queue plan");
                var networkDriver = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                networkDriver.Tick += (_, _) =>
                {
                    var dialog = ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).FirstOrDefault(w => w.Title == "下载与网络设置");
                    if (dialog == null)
                        return;
                    networkDriver.Stop();
                    var controls = Descendants(dialog).ToList();
                    controls.OfType<ComboBox>().Single(c => c.Items.Contains("仅官方")).SelectedItem = "仅官方";
                    Save(dialog, "v015-network-settings.png");
                    controls.OfType<Button>().Single(b => Equals(b.Content, "保存，下次启动生效")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                };
                networkDriver.Start();
                LauncherTestAccess.Invoke(window, "NetworkClick", [window, new RoutedEventArgs()]);
                networkDriver.Stop();
                if (new SettingsStore(settings).Load().Network.DownloadSource != "仅官方")
                    throw new Exception("Network save failed");
                Console.WriteLine("PASS in-process WPF network source selection and persistence");
                void DriveDialog(string title, Action open, Action<InlineDialog> check)
                {
                    Exception? failure = null;
                    var until = DateTime.UtcNow.AddSeconds(20);
                    var probe = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                    probe.Tick += (_, _) =>
                    {
                        try
                        {
                            var pages = ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).ToList();
                            if (DateTime.UtcNow > until)
                                throw new Exception("Dialog workflow timed out: " + title);
                            if (pages.LastOrDefault() is not { } page || page.Title != title)
                                return;
                            probe.Stop();
                            check(page);
                        }
                        catch (Exception ex)
                        {
                            failure = ex;
                            probe.Stop();
                            foreach (var page in ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).Reverse().ToList())
                                page.Close();
                        }
                    };
                    probe.Start();
                    try
                    {
                        open();
                    }
                    finally { probe.Stop(); }
                    if (failure != null)
                        throw failure;
                }
                ((Button)LauncherTestAccess.Get(window, "StopGameButton"))
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var runningPage = ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages"))
                    .Single(page => page.Title == "运行中的游戏");
                if (!Descendants(runningPage).OfType<TextBlock>().Any(text => text.Text.Contains("当前没有运行中的游戏")))
                    throw new Exception("Running-game page not shown by the actual home entry");
                runningPage.Close();
                Console.WriteLine("PASS actual home running-game button opens its inline page");
                var confirmationType = typeof(MainWindow).Assembly.GetType("BlockLaunch.GameCloseConfirmationView")!;
                var confirmation = Activator.CreateInstance(confirmationType,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [window], null)!;
                var rowType = typeof(MainWindow).Assembly.GetType("BlockLaunch.RunningGameRow")!;
                var sampleRow = Activator.CreateInstance(rowType, [Guid.NewGuid(), "fixture", game, "PlayerA"])!;
                async Task ConfirmAndChoose(bool approve)
                {
                    var pending = (Task<bool>)confirmationType.GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(confirmation, [sampleRow])!;
                    var rootGrid = (Grid)window.Content;
                    var overlay = rootGrid.Children.OfType<Grid>().Single(grid => Equals(grid.Tag, "GameCloseConfirmation"));
                    if (!Descendants(overlay).OfType<TextBlock>().Any(text => text.Text.Contains("未保存的内容")))
                        throw new Exception("Close confirmation lost unsaved-content warning");
                    if (!approve)
                        Save(overlay, "v0221-close-confirmation.png");
                    Descendants(overlay).OfType<Button>().Single(button => Equals(button.Content, approve ? "确认关闭" : "取消"))
                        .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    if (await pending != approve || rootGrid.Children.Contains(overlay))
                        throw new Exception("Compact close confirmation did not return/clean up correctly");
                }
                await ConfirmAndChoose(false);
                await ConfirmAndChoose(true);
                ((Button)LauncherTestAccess.Get(window, "StopGameButton"))
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var beforeEscape = ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).Count();
                var escapeResult = (Task<bool>)confirmationType.GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(confirmation, [sampleRow])!;
                var escape = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(window), Environment.TickCount, System.Windows.Input.Key.Escape)
                {
                    RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent
                };
                window.RaiseEvent(escape);
                if (await escapeResult || ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).Count() != beforeEscape)
                    throw new Exception("Escape closed the underlying running-game page instead of just the confirmation");
                ((IEnumerable<InlineDialog>)LauncherTestAccess.Get(window, "inlinePages")).Last().Close();
                Console.WriteLine("PASS compact in-window close confirmation supports cancel and confirm");
                DriveDialog("启动设置", () => LauncherTestAccess.Invoke(window, "EditLaunchOptions", [null]), page =>
                {
                    TextBox FieldAfter(string label)
                    {
                        var caption = Descendants(page).OfType<TextBlock>().Single(t => t.Text == label);
                        var stack = (StackPanel)caption.Parent;
                        return (TextBox)stack.Children[stack.Children.IndexOf(caption) + 1];
                    }
                    FieldAfter("游戏窗口标题（留空使用默认）").Text = "Global options fixture";
                    FieldAfter("自定义内存 MB（关闭自动分配后生效）").Text = "5120";
                    FieldAfter("窗口宽度").Text = "1512";
                    Descendants(page).OfType<Button>().Single(b => Equals(b.Content, "保存设置"))
                        .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                });
                var saved = new SettingsStore(settings).Load();
                if (saved.Launch.WindowTitle != "Global options fixture" || saved.Ram != 5120 || saved.Width != 1512
                    || ((TextBox)LauncherTestAccess.Get(window, "Memory")).Text != "5120"
                    || ((TextBox)LauncherTestAccess.Get(window, "GameWidth")).Text != "1512")
                    throw new Exception("Global launch options lost persistence or linked field updates");
                Console.WriteLine("PASS reference launch-options dialog persists global settings and linked controls");
                DriveDialog("导出整合包", () => LauncherTestAccess.Invoke(window, "ExportPackDialog", ["1.21.1"]), page =>
                {
                    var options = Descendants(page).OfType<CheckBox>().ToList();
                    if (options.Count != 5 || options.Count(c => c.IsChecked == true) != 3
                        || !options.Any(c => Equals(c.Content, "包含存档") && c.IsChecked == false)
                        || !Descendants(page).OfType<Button>().Any(b => Equals(b.Content, "选择保存位置并导出")))
                        throw new Exception("Pack export options or conservative save defaults changed");
                    page.Close();
                });
                Console.WriteLine("PASS reference pack-export dialog preserves options and excludes worlds by default");
                var packPreview = new PackUpgradePreview
                {
                    Pack = new("UI fixture", "1.21.1", "fabric", "0.19.5", "base", "pack-123456abcdef"),
                    HasBaseline = true,
                    Files = [new("config/conflict.cfg", "conflict", true, true), new("saves/world/level.dat", "player world", false, true)]
                };
                string[]? approved = null;
                Func<string[], Task<bool>> approve = files =>
                {
                    approved = files;
                    return Task.FromResult(true);
                };
                var packDialog = (InlineDialog)LauncherTestAccess.Invoke(window, "BuildPackUpgradeDialog", [packPreview, window, approve])!;
                packDialog.Loaded += (_, _) =>
                {
                    var controls = Descendants(packDialog).ToList();
                    controls.OfType<CheckBox>().Single().IsChecked = false;
                    Save(packDialog, "v015-pack-conflicts.png");
                    controls.OfType<Button>().Single(b => Equals(b.Content, "确认迁移并创建升级副本")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                };
                packDialog.ShowDialog();
                if (approved == null || !approved.SequenceEqual(new[] { "saves/world/level.dat" }))
                    throw new Exception("Pack conflict UI choice lost");
                Console.WriteLine("PASS in-process WPF pack conflict choice keeps worlds and uses new config");
                Console.WriteLine("ALL_WORKFLOW_UI_COMPONENT_CHECKS_PASSED (not native input acceptance)");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Environment.ExitCode = 1;
            }
            finally
            {
                app.Shutdown();
            }
        };
        timer.Start();
        app.Run();
    }

    static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var item in Descendants(child))
                yield return item;
        }
    }

    static void Save(FrameworkElement window, string name)
    {
        window.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        // CI starts from a clean checkout; screenshot output is not a tracked directory.
        var path = Path.GetFullPath(Path.Combine("work", "verify", name));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = File.Create(path);
        encoder.Save(output);
    }
}

sealed class Handler : HttpMessageHandler
{
    public int Calls;
    public static ContentVersion V(string id) => new()
    {
        Id = id,
        ProjectId = "test",
        Name = "test",
        VersionNumber = id,
        VersionType = "release",
        GameVersions = ["1.21.1"],
        Loaders = ["fabric"],
        Files = [new()
        {
            Filename = "test.jar",
            Url = "https://cdn.modrinth.com/" + id,
            Size = id.Length,
            Hashes = new()
            {
                ["sha512"] = Convert.ToHexString(SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(id)))
            }
        }

        ]
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Interlocked.Increment(ref Calls);
        var text = request.RequestUri!.Host == "cdn.modrinth.com" ? request.RequestUri.AbsolutePath.Trim('/') : request.RequestUri.AbsolutePath.EndsWith("/version") ? JsonSerializer.Serialize(new[] { V("new") }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }) : "{\"project_type\":\"mod\",\"client_side\":\"required\"}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text, System.Text.Encoding.UTF8, "application/json") });
    }
}
