using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;
using BlockLaunch.Front;

internal static class FrontUiChecks
{
    public static void Run(FrontLauncherRuntime runtime)
    {
        var application = new FrontApp();
        application.InitializeComponent();
        var resources = new FixtureResources();
        var window = new FrontWindow(resources, runtime, runtime.Downloads, runtime, "UI fixture");
        var start = (RadioButton)window.FindName("StartNavigation");
        var download = (RadioButton)window.FindName("DownloadNavigation");
        var settings = (RadioButton)window.FindName("SettingsNavigation");
        var host = (ContentControl)window.FindName("PageHost");
        Check(host.Content is LauncherView, "initial navigation selects launcher after all controls exist");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            download.IsChecked = true;
            Check(host.Content is DownloadBrowserView, "download navigation hosts category browser");
            var categories = (DownloadBrowserView)host.Content;
            categories.ShowCategory("Mod");
            Check(resources.Searches > 0, "resource category uses injected shared resource interface");
            settings.IsChecked = true;
            Check(host.Content is SettingsView, "settings navigation remains reachable after resource category");
            start.IsChecked = true;
            Check(host.Content is LauncherView, "launcher can be reparented out of download host");
            download.IsChecked = true;
            categories.ShowCategory("本地整合包");
            start.IsChecked = true;
            Check(host.Content is LauncherView, "import category does not retain launcher logical parent");
        }
        window.Close();
        Console.WriteLine("ALL_FRONT_UI_STRUCTURE_CHECKS_PASSED");
    }

    static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    sealed class FixtureResources : IFrontResourceBrowser
    {
        public int Searches { get; private set; }
        public Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token)
        {
            Searches++;
            return Task.FromResult(new FrontResourcePage([], 0));
        }
        public Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<FrontResourceVersion>>([]);
    }
}
