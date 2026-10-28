using System.Net.Http;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Linq;
using BlockLaunch;
using BlockLaunch.Contracts;
using BlockLaunch.Front;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var source = XDocument.Load(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));
        var ns = source.Root!.Name.Namespace;
        var dictionary = new XElement(ns + "ResourceDictionary", new XAttribute(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var application = new Application
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose,
            Resources = (ResourceDictionary)XamlReader.Parse(dictionary.ToString())
        };
        var live = args.Contains("--live");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MCUGLauncher/0.21.0 (Front-preview)");
        IFrontResourceBrowser browser = live
            ? new FrontResourceBrowserAdapter(new ResourceManagementService(http))
            : new DemoResourceBrowser();
        var window = new FrontWindow(browser, live ? "真实只读查询 · Modrinth" : "模拟模式 · 不访问网络\n搜索 empty / error 查看状态");
        application.Run(window);
    }
}
