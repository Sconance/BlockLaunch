using System.Net.Http;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Linq;
using BlockLaunch;
using BlockLaunch.Contracts;
using BlockLaunch.Front;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Front 预览启动失败：" + exception.Message);
            return 1;
        }
    }

    static int Run(string[] args)
    {
        var settingsDirectory = Path.Combine(Path.GetTempPath(), "MCUGLauncher-FrontPreview");
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--settings-dir" && index + 1 < args.Length && !args[index + 1].StartsWith("--"))
                settingsDirectory = Path.GetFullPath(args[++index]);
            else if (args[index] is not ("--live" or "--simulate-game-running"))
                throw new ArgumentException("参数无效。用法：FrontPreview [--live] [--settings-dir <独立测试目录>] [--simulate-game-running]");
        }
        Directory.CreateDirectory(settingsDirectory);
        var marker = Path.Combine(settingsDirectory, ".front-preview");
        if (!File.Exists(marker) && Directory.EnumerateFileSystemEntries(settingsDirectory).Any())
            throw new IOException("设置预览仅使用空目录或带 .front-preview 标记的测试目录；不能使用正式启动器配置目录。");
        using var lease = new FileStream(Path.Combine(settingsDirectory, "download-queue.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(marker) && Directory.EnumerateFileSystemEntries(settingsDirectory)
            .Any(entry => Path.GetFileName(entry) != "download-queue.lock"))
            throw new IOException("设置预览仅使用空目录或带 .front-preview 标记的测试目录；不能使用正式启动器配置目录。");
        File.WriteAllText(marker, "FrontPreview isolated test settings");
        var store = new SettingsStore(settingsDirectory);
        if (!File.Exists(Path.Combine(settingsDirectory, "settings.json")))
            store.Save(new Preferences { Root = Path.Combine(settingsDirectory, "minecraft-preview") });
        var simulatedGame = args.Contains("--simulate-game-running");
        IFrontSettings settings = new FrontSettingsAdapter(store, new LauncherSettingsService(store, () => simulatedGame));
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
        var mode = live ? "真实只读资源查询 · Modrinth" : "模拟资源 · 不访问网络\n搜索 empty / error 查看状态";
        mode += "\n设置真实读写 · 独立测试目录\n" + settingsDirectory;
        if (simulatedGame)
            mode += "\n测试门禁：模拟游戏运行，禁止保存";
        Console.WriteLine("设置预览目录：" + settingsDirectory);
        var window = new FrontWindow(browser, settings, mode);
        application.Run(window);
        return 0;
    }
}
