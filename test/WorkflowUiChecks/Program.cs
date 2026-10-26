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
    [STAThread] public static void Main()
    {
        var root = Path.GetFullPath("work/v015-component-ui"); var settings = Path.Combine(root, "settings"); var game = Path.Combine(root, "minecraft");
        new SettingsStore(settings).Save(new() { Root = game });
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml")); var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown, Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString()) };
        var window = new MainWindow(settings) { Title = "0.15 component tests" }; window.Show();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) }; var deadline = DateTime.UtcNow.AddMinutes(3); bool started = false;
        timer.Tick += async (_, _) =>
        {
            if (started) return;
            if ((bool)typeof(MainWindow).GetField("busy", flags)!.GetValue(window)!) { if (DateTime.UtcNow > deadline) { Environment.ExitCode = 1; app.Shutdown(); } return; }
            started = true; timer.Stop();
            try
            {
                typeof(MainWindow).GetField("queueReady", flags)!.SetValue(window, false);
                ((DispatcherTimer?)typeof(MainWindow).GetField("downloadTimer", flags)!.GetValue(window))?.Stop();
                using var http = new HttpClient(new Handler()); typeof(MainWindow).GetField("http", flags)!.SetValue(window, http);
                var instance = Path.Combine(game, "test-instance");
                if (ContentService.ReadManaged(instance).Count == 0) await new ContentService(http).Install(new([Handler.V("old")], new() { ["test"] = "mod" }), instance);
                var stage = 0; var errors = new List<Exception>();
                var driver = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                driver.Tick += (_, _) =>
                {
                    try
                    {
                        var dialogs = ((IEnumerable<InlineDialog>)typeof(MainWindow).GetField("inlinePages", flags)!.GetValue(window)!).ToList();
                        if (stage == 0 && dialogs.FirstOrDefault(w => w.Title == "内容更新") is { } updates)
                        {
                            var controls = Descendants(updates).ToList(); if (!controls.OfType<CheckBox>().Any(c => c.IsEnabled && c.IsChecked == true)) throw new Exception("No selectable update");
                            stage = 1; controls.OfType<Button>().Single(b => Equals(b.Content, "预览所选更新")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        else if (stage == 1 && dialogs.FirstOrDefault(w => w.Title == "更新预览") is { } preview)
                        {
                            var text = string.Join("\n", Descendants(preview).OfType<TextBlock>().Select(t => t.Text));
                            if (!text.Contains("old") || !text.Contains("new") || !text.Contains("mods/test.jar")) throw new Exception("Preview missing old/new/path");
                            Save(preview, "v015-update-preview.png");
                            stage = 2; Descendants(preview).OfType<Button>().Single(b => Equals(b.Content, "确认并加入下载队列")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                    }
                    catch (Exception ex) { errors.Add(ex); foreach (var dialog in ((IEnumerable<InlineDialog>)typeof(MainWindow).GetField("inlinePages", flags)!.GetValue(window)!).Reverse().ToList()) dialog.Close(); }
                };
                driver.Start();
                await (Task)typeof(MainWindow).GetMethod("OpenContentUpdates", flags)!.Invoke(window, [instance, "fixture", "1.21.1", "fabric", null, null])!;
                driver.Stop(); if (errors.Count > 0) throw errors[0]; if (stage != 2) throw new Exception("Workflow incomplete");
                var queue = (OperationCoordinator)typeof(MainWindow).GetField("operations", flags)!.GetValue(window)!;
                var job = queue.History().First(r => r.Download?.PreparedPlan != null);
                if (job.Download!.PreparedPlan!.Versions.Single().Id != "new" || string.IsNullOrEmpty(job.Download.ContentFingerprint)) throw new Exception("Queue lost approved plan");
                Console.WriteLine("PASS in-process WPF update selection, preview, confirmation, frozen queue plan");
                var networkDriver = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                networkDriver.Tick += (_, _) =>
                {
                    var dialog = ((IEnumerable<InlineDialog>)typeof(MainWindow).GetField("inlinePages", flags)!.GetValue(window)!).FirstOrDefault(w => w.Title == "下载与网络设置"); if (dialog == null) return; networkDriver.Stop();
                    var controls = Descendants(dialog).ToList(); controls.OfType<ComboBox>().Single(c => c.Items.Contains("仅官方")).SelectedItem = "仅官方";
                    Save(dialog, "v015-network-settings.png");
                    controls.OfType<Button>().Single(b => Equals(b.Content, "保存，下次启动生效")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                };
                networkDriver.Start(); typeof(MainWindow).GetMethod("NetworkClick", flags)!.Invoke(window, [window, new RoutedEventArgs()]); networkDriver.Stop();
                if (new SettingsStore(settings).Load().Network.DownloadSource != "仅官方") throw new Exception("Network save failed");
                Console.WriteLine("PASS in-process WPF network source selection and persistence");
                var packPreview = new PackUpgradePreview { Pack = new("UI fixture", "1.21.1", "fabric", "0.19.5", "base", "pack-123456abcdef"), HasBaseline = true,
                    Files = [new("config/conflict.cfg", "conflict", true, true), new("saves/world/level.dat", "player world", false, true)] };
                string[]? approved = null;
                Func<string[], Task<bool>> approve = files => { approved = files; return Task.FromResult(true); };
                var packDialog = (InlineDialog)typeof(MainWindow).GetMethod("BuildPackUpgradeDialog", flags)!.Invoke(window, [packPreview, window, approve])!;
                packDialog.Loaded += (_, _) =>
                {
                    var controls = Descendants(packDialog).ToList(); controls.OfType<CheckBox>().Single().IsChecked = false;
                    Save(packDialog, "v015-pack-conflicts.png");
                    controls.OfType<Button>().Single(b => Equals(b.Content, "确认迁移并创建升级副本")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                };
                packDialog.ShowDialog();
                if (approved == null || !approved.SequenceEqual(new[] { "saves/world/level.dat" })) throw new Exception("Pack conflict UI choice lost");
                Console.WriteLine("PASS in-process WPF pack conflict choice keeps worlds and uses new config");
                Console.WriteLine("ALL_WORKFLOW_UI_COMPONENT_CHECKS_PASSED (not native input acceptance)");
            }
            catch (Exception ex) { Console.WriteLine(ex); Environment.ExitCode = 1; }
            finally { app.Shutdown(); }
        };
        timer.Start(); app.Run();
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) { yield return child; foreach (var item in Descendants(child)) yield return item; }
    }
    static void Save(FrameworkElement window, string name)
    {
        window.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.GetFullPath(Path.Combine("work", "verify", name))); encoder.Save(output);
    }
}
sealed class Handler : HttpMessageHandler
{
    public static ContentVersion V(string id) => new() { Id = id, ProjectId = "test", Name = "test", VersionNumber = id, VersionType = "release", GameVersions = ["1.21.1"], Loaders = ["fabric"], Files = [new() { Filename = "test.jar", Url = "https://cdn.modrinth.com/" + id, Size = id.Length, Hashes = new() { ["sha512"] = Convert.ToHexString(SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(id))) } }] };
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var text = request.RequestUri!.Host == "cdn.modrinth.com" ? request.RequestUri.AbsolutePath.Trim('/') : request.RequestUri.AbsolutePath.EndsWith("/version") ? JsonSerializer.Serialize(new[] { V("new") }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }) : "{\"project_type\":\"mod\",\"client_side\":\"required\"}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text, System.Text.Encoding.UTF8, "application/json") });
    }
}
