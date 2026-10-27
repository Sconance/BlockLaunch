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
            typeof(MainWindow).GetField("rows", flags)!.SetValue(window, new List<VersionRow> { new("26.4-snapshot-5", "snapshot", "snapshot"), new("26.4-snapshot-4", "snapshot", "snapshot"), new("26.3", "release", "release"), new("1.21.1", "release", "release"), new("24w14potato", "april", "snapshot"), new("a1.2.6", "alpha", "old_alpha"), new("custom", "local", "local") });
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
            var content = (Grid)window.Content;
            content.Background = (Brush)window.Background;
            content.Measure(new Size(1220, 820));
            content.Arrange(new Rect(0, 0, 1220, 820));
            content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1220, 820, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var output = File.Create("work/verify/v0151-download-page.png"))
                encoder.Save(output);
            Console.WriteLine("ALL_DOWNLOAD_UI_COMPONENT_CHECKS_PASSED (not native input acceptance)");
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
}
