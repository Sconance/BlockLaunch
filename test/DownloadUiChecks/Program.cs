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

            Call("SelectDownloadCategory", "Mod");
            var example = new ContentProject("fixture", "fixture", "用于验证长名称适配的模组项目 · MCUG", "项目介绍保留完整文本，下载选项独立排列。", "MCUG", 12345) { IconUrl = "pack://application:,,,/MCUGLauncher;component/Assets/MCUGLogo.png" };
            Call("ShowResourceDetail", example);
            var detailSurface = (Border)Field("resourceDetailSurface");
            Assert(detailSurface.Visibility == Visibility.Visible && ((Grid)Field("resourceResultsLayout")).Visibility == Visibility.Collapsed, "detail opens inside resource page");
            var versions = (ComboBox)Field("ProjectVersions");
            versions.ItemsSource = new[] { new ContentVersion { VersionNumber = "1.0", DatePublished = DateTimeOffset.Parse("2026-10-27T00:00:00Z"), GameVersions = ["1.21.1"], Loaders = ["fabric"], Dependencies = [new() { DependencyType = "required" }] } };
            versions.SelectedIndex = 0;
            Assert(((TextBlock)Field("resourceDependencies")).Text.Contains("必需依赖：1"), "dependency counts follow selected version");
            Call("ArrangeResourceDetail", 600d);
            Assert(Grid.GetRow(((Grid)Field("resourceDetailColumns")).Children[1]) == 1, "narrow detail stacks options below introduction");
            Call("ArrangeResourceDetail", 900d);
            Assert(Grid.GetColumn(((Grid)Field("resourceDetailColumns")).Children[1]) == 1, "wide detail restores right options column");
            var previewRoot = (Grid)window.Content;
            void Capture(string file, int width, int height)
            {
                previewRoot.Background = (Brush)window.Background;
                previewRoot.Measure(new Size(width, height));
                previewRoot.Arrange(new Rect(0, 0, width, height));
                previewRoot.UpdateLayout();
                var frame = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                frame.Render(previewRoot);
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(frame));
                using var stream = File.Create(file);
                png.Save(stream);
            }
            ((TextBlock)Field("ProjectTitle")).Text = example.Title;
            ((TextBlock)Field("ProjectDescription")).Text = example.Description;
            Capture("work/verify/v0201-resource-detail.png", 1220, 820);
            Capture("work/verify/v0201-resource-detail-small.png", 900, 640);
            Call("ShowResourceResults");
            Assert(detailSurface.Visibility == Visibility.Collapsed && ((Grid)Field("resourceResultsLayout")).Visibility == Visibility.Visible, "back preserves resource list");
            Assert(categories.Values.All(b => b.Content is Grid && b.ContentTemplate == null), "category artwork supports image slot without text template");
            ((ListBox)Field("SearchResults")).ItemsSource = new[] { example, example with { Title = "Fabric API · 布局验证样例" }, example with { Title = "Iris Shaders · 布局验证样例" } };
            ((TextBlock)Field("SearchHint")).Visibility = Visibility.Collapsed;
            Capture("work/verify/v0201-resource-list.png", 1220, 820);
            Call("RestoreResourcePage");
            Assert(((TabItem)tabs.Items[2]).Content == Field("resourcePage"), "standalone content center restores same resource page");
            Call("SelectDownloadCategory", "本地整合包");
            Assert(((ContentControl)Field("downloadHost")).Content == Field("packImportPage"), "local pack import remains available");
            Call("SelectDownloadCategory", "模组加载器");
            Assert(((ContentControl)Field("downloadHost")).Content == Field("loaderPage"), "loader installation remains available");
            Call("SelectDownloadCategory", "模组加载器");
            Capture("work/verify/v0201-loaders.png", 1220, 820);
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
            Call("SelectDownloadCategory", "Mod");
            ((TextBox)Field("resourceGame")).Text = "全部";
            ((ComboBox)Field("resourceLoader")).SelectedIndex = 1;
            Call("ToggleResourceFavorite", example);
            var store = (ResourceBookmarkStore)Field("bookmarks");
            Assert(store.Read().Any(b => b.Project.Id == example.Id), "heart favorite saves to existing bookmark document");
            typeof(MainWindow).GetField("resourceFavoritesOnly", flags)!.SetValue(window, true);
            var favorites = ((Task<SearchPage>)Call("SearchFavoriteResources", CancellationToken.None)!).GetAwaiter().GetResult();
            Assert(favorites.Items.Count == 1 && favorites.Items[0].Id == example.Id, "favorites filter searches full saved collection");
            Call("ToggleResourceFavorite", example);
            Assert(store.Read().Count == 0, "heart toggles favorite off");
            Call("ToggleResourceView");
            Assert(((bool)Field("resourceListView")), "resource list view switch changes layout");
            Call("ToggleResourceView");
            foreach (var area in Enum.GetValues<LauncherMenuArea>())
                window.MenuPages.Register(area, new("fixture", "扩展验收", context => new TextBlock { Text = "扩展页面：" + context.InstanceId }));
            Assert(window.MenuPages.Pages(LauncherMenuArea.Download).Count == 1 && ((StackPanel)Field("downloadNavigationPanel")).Children.OfType<Button>().Any(b => b.Tag is LauncherMenuPage), "download extension is visible after registration");
            Assert(((TabControl)Field("resourceDetailTabs")).Items.OfType<TabItem>().Any(t => t.Tag is LauncherMenuPage), "detail extension tab is attached");
            var downloadExtension = ((StackPanel)Field("downloadNavigationPanel")).Children.OfType<Button>().Single(b => b.Tag is LauncherMenuPage);
            downloadExtension.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(tabs.SelectedIndex == 1 && ((ContentControl)Field("downloadHost")).Content is ScrollViewer, "download extension preserves category sidebar");
            var detailTab = ((TabControl)Field("resourceDetailTabs")).Items.OfType<TabItem>().Single(t => t.Tag is LauncherMenuPage);
            detailTab.IsSelected = true;
            Assert(detailTab.Content is TextBlock, "detail extension factory is activated lazily");

            var duplicateRejected = false;
            try
            {
                window.MenuPages.Register(LauncherMenuArea.Download, new("fixture", "重复", _ => new TextBlock()));
            }
            catch (ArgumentException) { duplicateRejected = true; }
            Assert(duplicateRejected, "duplicate menu identifiers are rejected");
            Call("OpenMenuExtension", window.MenuPages.Pages(LauncherMenuArea.Download)[0], "fixture-instance");
            Assert(((TabItem)tabs.SelectedItem).Header?.ToString() == "扩展验收" && app.Windows.Count == 1, "extension navigates in main window");
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
