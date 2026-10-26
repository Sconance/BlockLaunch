using BlockLaunch;
using System.IO;
using System.Windows;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var directory = Path.GetFullPath(args.Contains("--empty") ? "work/v013-ui-empty" : "work/v013-ui-populated"); var root = Path.Combine(directory, "minecraft");
        Directory.CreateDirectory(Path.Combine(root, "versions", "1.21.1")); File.WriteAllText(Path.Combine(root, "versions", "1.21.1", "1.21.1.json"), "{\"id\":\"1.21.1\",\"type\":\"release\",\"libraries\":[]}");
        var catalog = new InstanceCatalog(root); var key = args.Contains("--empty") ? "" : catalog.Create("0.9 诊断验收（合成日志）", "1.21.1", "1.21.1");
        if (key.Length > 0) { var incomplete = catalog.Create("未完成安装测试", "1.21.1", "1.21.1"); AtomicFile.WriteJson(Path.Combine(root,".blocklaunch-installations",incomplete+".json"), new VersionInstallation { Version=incomplete, Ready=false, State="Interrupted" }); }
        var instance = key.Length > 0 ? catalog.DirectoryFor(key) : Path.Combine(root,"fixture-only"); Directory.CreateDirectory(Path.Combine(instance, "logs")); Directory.CreateDirectory(Path.Combine(instance, "mods"));
        File.WriteAllText(Path.Combine(instance, "logs", "latest.log"), "[Synthetic fixture, not a real crash]\njava.lang.OutOfMemoryError: Java heap space\nIncompatible mods found! Example mod needs a compatible dependency\nAuthorization: Bearer demo-secret-not-real\nuser@example.com C:\\Users\\FixtureUser\\Desktop 127.0.0.1\n");
        File.WriteAllText(Path.Combine(instance, "mods", "Example-Not-A-Real-Mod.jar"), "UI fixture only");
        new SettingsStore(Path.Combine(directory, "settings")).Save(new Preferences { SchemaVersion = 2, Root = root, LastVersion = key, Player = "FixtureUser", PreferMicrosoft = false });
        var bookmarkStore = new ResourceBookmarkStore(Path.Combine(directory,"settings"));
        bookmarkStore.Save(new ContentProject("sodium","sodium","Sodium（隔离验收）","测试收藏","CaffeineMC",0),"mod","性能优化");
        bookmarkStore.Save(new ContentProject("fabric-api","fabric-api","Fabric API（隔离验收）","测试收藏","FabricMC",0),"mod","基础组件");
        bookmarkStore.CreateFolder("常用资源");
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));
        var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose, Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString()) };
        var window = new MainWindow(Path.Combine(directory, "settings")) { Title = "BlockLaunch 0.13 · 收藏夹隔离验收", Width = 1080, Height = 760 };
        app.Run(window);
    }
}
