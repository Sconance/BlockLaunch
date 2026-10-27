using BlockLaunch;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var root = Path.GetFullPath("work/v0151-ui-" + Guid.NewGuid().ToString("N"));
        new SettingsStore(root).Save(new()
        {
            Root = Path.Combine(root, "minecraft")
        });
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));
        var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
            Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString())
        };
        var window = new MainWindow(root);
        SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
        object Field(string name) => typeof(MainWindow).GetField(name, flags)!.GetValue(window)!;
        object? Call(string name, params object[] args) => typeof(MainWindow).GetMethod(name, flags)!.Invoke(window, args);
        void Assert(bool condition, string label)
        {
            if (!condition)
                throw new Exception(label);
            Console.WriteLine("PASS " + label);
        }

        try
        {
            typeof(MainWindow).GetField("rows", flags)!.SetValue(window, new List<VersionRow> { new("26.4-snapshot-5", "snapshot", "snapshot"), new("26.4-snapshot-4", "snapshot", "snapshot"), new("26.3", "release", "release", new DateTimeOffset(2026, 9, 15, 11, 23, 0, TimeSpan.Zero)), new("1.21.1", "release", "release"), new("24w14potato", "april", "snapshot"), new("a1.2.6", "alpha", "old_alpha"), new("custom", "local", "local") });
            Call("SelectDownloadCategory", "原版游戏");
            var tabs = (TabControl)Field("MainTabs");
            ((StackPanel)((RadioButton)Field("HomeNav")).Parent).Children.OfType<RadioButton>().Single(r => r.Tag?.ToString() == "1").IsChecked = true;
            tabs.SelectedIndex = 1;
            var groups = (StackPanel)Field("vanillaGroups");
            List<Expander> Groups() => groups.Children.OfType<Border>().Select(b => b.Child).OfType<Expander>().ToList();
            var latest = (StackPanel)Groups()[0].Content;
            Assert(latest.Children.Count == 2 && latest.Children.OfType<Button>().Select(b => b.ToolTip!.ToString()).SequenceEqual(new[] { "选择 26.3", "选择 26.4-snapshot-5" }), "latest includes one release and one snapshot");
            Assert(Groups().Count == 5 && !string.Join("", Groups().Select(e => e.Header)).Contains("custom"), "five groups exclude local profiles");
            var april = Groups().Last();
            april.IsExpanded = true;
            Assert(((StackPanel)april.Content).Children.OfType<Button>().Single().ToolTip!.ToString() == "选择 24w14potato", "April versions separate from snapshots");
            Groups()[1].IsExpanded = true;
            ((StackPanel)Groups()[1].Content).Children.OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert((string)Field("downloadVersion") == "26.3" && Groups()[1].IsExpanded && app.Windows.Count == 1, "inline selection keeps expansion and opens no dialog");
            Call("InstallClick", window, new RoutedEventArgs());
            Assert(((OperationCoordinator)Field("operations")).History().Any(r => r.Download?.Version == "26.3"), "selected version reaches persistent download queue");
            var categories = (Dictionary<string, RadioButton>)Field("downloadCategories");
            foreach (var pair in new[] {
                ("Mod", 0),
                ("整合包", 2),
                ("数据包", 4),
                ("资源包", 1),
                ("光影包", 3)
            }

            )
            {
                categories[pair.Item1].IsChecked = true;
                Assert(((ComboBox)Field("ContentType")).SelectedIndex == pair.Item2 && ((ContentControl)Field("downloadHost")).Content == Field("resourcePage"), "embedded category " + pair.Item1);
            }

            Call("RestoreResourcePage");
            Assert(((TabItem)tabs.Items[2]).Content == Field("resourcePage"), "standalone content center restores same resource page");
            Call("SelectDownloadCategory", "本地整合包");
            Assert(((ContentControl)Field("downloadHost")).Content == Field("packImportPage"), "local pack import remains available");
            Call("SelectDownloadCategory", "模组加载器");
            Assert(((ContentControl)Field("downloadHost")).Content == Field("loaderPage"), "loader installation remains available");
            Call("SelectDownloadCategory", "原版游戏");
            ((TextBox)Field("vanillaQuery")).Text = "1.21.1";
            Assert(((StackPanel)Groups()[1].Content).Children.OfType<Button>().Count() == 1, "version search filters groups");
            ((TextBox)Field("vanillaQuery")).Clear();
            foreach (var group in Groups().Skip(1))
                group.IsExpanded = false;
            var latestText = string.Join(" ", Walk(latest).OfType<TextBlock>().Select(t => t.Text));
            Assert(latestText.Contains("发布于") && !latestText.Contains("点击选择下载"), "release rows show provided publication date");
            var prefs = (Preferences)Field("prefs");
            prefs.ResourceDownloadDirectory = Path.Combine(root, "downloads");
            Call("SelectDownloadCategory", "Mod");
            var raw = new ContentVersion
            {
                ProjectId = "test",
                Id = "version1",
                Files = [new()
                {
                    Filename = "sample.jar",
                    Url = "https://cdn.modrinth.com/sample.jar",
                    Size = 1,
                    Hashes = new()
                    {
                        ["sha512"] = new string('a', 128)
                    }
                }

                ]
            };
            Call("QueueContent", raw);
            Assert(((OperationCoordinator)Field("operations")).History().Any(r => r.Download?.Kind == "resource" && r.Download.Destination == prefs.ResourceDownloadDirectory), "download queues independently without installed game");
            var top = (StackPanel)((RadioButton)Field("HomeNav")).Parent;
            Assert(top.Children.OfType<RadioButton>().Count() == 3 && !top.Children.OfType<RadioButton>().Any(r => r.Content?.ToString()?.Contains("版本管理") == true), "main navigation contains launch download settings only");
            void Wait(Task task)
            {
                var frame = new System.Windows.Threading.DispatcherFrame();
                task.ContinueWith(_ => window.Dispatcher.BeginInvoke(() => frame.Continue = false));
                System.Windows.Threading.Dispatcher.PushFrame(frame);
                task.GetAwaiter().GetResult();
            }

            var content = (Grid)window.Content;
            content.Background = (Brush)window.Background;
            void Capture(string name)
            {
                content.Measure(new Size(1060, 700));
                content.Arrange(new Rect(0, 0, 1060, 700));
                content.UpdateLayout();
                var bitmap = new RenderTargetBitmap(1060, 700, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(content);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create("work/verify/" + name);
                encoder.Save(output);
            }

            Call("NavigateDownloads", "原版游戏");
            Wait((Task)Call("Refresh")!);
            var liveRows = (List<VersionRow>)Field("rows");
            Assert(liveRows.Any(r => r.Published != null && r.Type == "release"), "live manifest supplies release timestamps");
            Capture("v0152-vanilla.png");
            Call("NavigateDownloads", "Mod");
            Wait((Task)Call("SearchContent")!);
            Assert(((ListBox)Field("SearchResults")).Items.Count > 0, "live Modrinth search works without installed instance");
            Wait(Task.Delay(1500));
            Capture("v0202-resources.png");
            var list = (ListBox)Field("SearchResults");
            var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            Assert(row.ActualHeight <= 152 && row.ActualWidth < list.ActualWidth / 1.5, "responsive resource cards use multiple columns");
            list.SelectedIndex = 0;
            async Task Settle()
            {
                var deadline = DateTime.UtcNow.AddMinutes(2);
                // Resource browsing has its own cancellation scope and does not block other UI actions.
                while ((bool)Field("busy") || Field("browseCancellation") != null)
                {
                    if (DateTime.UtcNow > deadline)
                        throw new Exception("Project detail timed out");
                    await Task.Delay(50);
                }
            }

            Wait(Settle());
            var versions = (ComboBox)Field("ProjectVersions");
            Assert(versions.Items.Count > 0, "project versions load without vanilla-instance restriction");
            Capture("v0202-resource-detail.png");
            var selectedResource = (ContentVersion)versions.SelectedItem;
            ((Button)Field("InstallContentButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var coordinator = (OperationCoordinator)Field("operations");
            var resourceRecord = coordinator.History().First(r => r.Download?.Kind == "resource" && r.Download.Content?.Id == selectedResource.Id);
            var beforeFiles = Directory.GetFiles(prefs.Root, "*", SearchOption.AllDirectories).ToDictionary(f => f, File.ReadAllBytes);
            Wait(coordinator.Run("实际独立资源下载", prefs.Root, selectedResource.Id, async context => await (Task)Call("ExecuteDownload", resourceRecord, context)!));
            var downloadedPath = ResourceDownloads.PathFor(prefs.ResourceDownloadDirectory, "mod", selectedResource);
            Assert(File.Exists(downloadedPath) && new FileInfo(downloadedPath).Length > 0, "real Modrinth selected file downloaded outside game");
            Assert(Directory.GetFiles(prefs.Root, "*", SearchOption.AllDirectories).Length == beforeFiles.Count && beforeFiles.All(x => File.ReadAllBytes(x.Key).SequenceEqual(x.Value)), "real resource download leaves game directory unchanged");
            var favoriteProject = (ContentProject)list.SelectedItem;
            Call("ToggleResourceFavorite", favoriteProject);
            Wait((Task)Call("ToggleResourceFavoriteFilter")!);
            Assert(list.Items.Count > 0 && list.Items.Cast<ContentProject>().All(p => p.IsFavorite), "only-favorites view returns saved resources across search pages");
            Capture("v0202-favorites.png");
            Wait((Task)Call("ToggleResourceFavoriteFilter")!);
            Call("NavigateDownloads", "模组加载器");
            var minecraftChoice = (ComboBox)Field("loaderMinecraft");
            minecraftChoice.SelectedItem = liveRows.First(r => r.Id == "1.21.1");
            Wait((Task)Call("LoadConfigurationVersions")!);
            var loaderRows = (System.Collections.IDictionary)Field("loaderRows");
            var fabricControls = loaderRows["fabric"]!;
            var fabricVersions = (ComboBox)fabricControls.GetType().GetField("Item1")!.GetValue(fabricControls)!;
            Assert(fabricVersions.Items.Count > 0 && fabricVersions.SelectedItem is LoaderRelease, "inline loader versions load from official metadata");
            Call("QueueConfiguredLoader", "fabric");
            var chosenLoader = (LoaderRelease)fabricVersions.SelectedItem;
            Assert(coordinator.History().Any(r => r.Download?.Loader == "fabric" && r.Download.Version == "1.21.1" && r.Download.LoaderVersion == chosenLoader.Version), "inline loader selection reaches existing download queue");
            Capture("v0202-loader-configuration.png");
            ((RadioButton)Field("HomeNav")).IsChecked = true;
            Capture("v0152-home.png");
            Assert(Walk(content).OfType<Button>().Contains((Button)Field("sketchToggle")) && Walk(content).OfType<Button>().Any(b => Equals(b.ToolTip, "管理当前版本")), "home exposes version selection and settings");
            Call("ShowHomeVersions");
            Assert(tabs.SelectedIndex == 0 && ((Border)Field("sketchVersions")).Visibility == Visibility.Visible, "version selection stays in main window");
            Capture("v0152-version-selection.png");
            Console.WriteLine("ALL_COMPACT_UI_CHECKS_PASSED (component rendering, not native input acceptance)");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Environment.ExitCode = 1;
        }
        finally
        {
            ((FileStream)Field("queueLease")).Dispose();
            app.Shutdown();
        }
    }

    static IEnumerable<DependencyObject> Walk(DependencyObject node)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in Walk(child))
                yield return nested;
        }
    }
}
