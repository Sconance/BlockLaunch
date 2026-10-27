using BlockLaunch;
using System.IO;
using System.Windows;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var root = Path.GetFullPath("work/window-layout-preview");
        var game = Path.Combine(root, "minecraft");
        var profile = Path.Combine(game, "versions", "1.21.1");
        Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(profile, "1.21.1.json"), "{\"id\":\"1.21.1\",\"type\":\"release\",\"libraries\":[]}");
        new SettingsStore(root).Save(new()
        {
            Root = game,
            ResourceDownloadDirectory = Path.Combine(root, "downloads")
        });
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));
        var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application
        {
            Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString())
        };
        app.Run(new MainWindow(root) { Title = "MCUG Launcher · 窗口还原隔离验收" });
    }
}
