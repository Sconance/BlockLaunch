using System.IO;
using System.Windows;

namespace BlockLaunch.Front;

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
            MessageBox.Show(exception.Message, "Front 启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
    }

    static int Run(string[] args)
    {
        var options = LauncherStartupOptions.Parse(args);
        var settingsDirectory = options.SettingsDirectory;
        var application = new FrontApp();
        application.InitializeComponent();
        var configurationDirectory = settingsDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory);
        FrontWindow? window = null;
        var browser = new FrontMicrosoftOAuthUI(() => window, Path.Combine(configurationDirectory, "webview2-front"));
        using var runtime = new FrontLauncherRuntime(settingsDirectory, browser);
        window = new FrontWindow(runtime.ResourceBrowser, runtime, runtime.Downloads, runtime,
            "正式配置与真实业务\n" + runtime.ConfigurationDirectory + "\n同目录不能同时运行新旧前端",
            options.Launch == null ? null : token => runtime.LaunchFromScript(options.Launch, message =>
                MessageBox.Show(window, message, "确认脚本启动", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK, token));
        application.Run(window);
        return 0;
    }
}
