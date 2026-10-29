using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using BlockLaunch.Contracts;
using BlockLaunch.SharedUI;

namespace BlockLaunch.Front;

public partial class FrontWindow : Window
{
    readonly IFrontLauncher launcher;
    readonly ResourceBrowserViewModel resources;
    readonly ResourceBrowserView browser;
    readonly SettingsViewModel settings;
    readonly SettingsView settingsView;
    readonly DownloadTasksView downloadsView;
    readonly LauncherView launcherView;
    readonly DispatcherTimer timer;
    readonly CancellationTokenSource lifetime = new();
    readonly LogView logView;
    readonly Func<CancellationToken, Task>? startupAction;
    bool closing;
    bool canClose;
    bool navigating;
    public FrontMenuRegistry MenuPages { get; } = new();

    public FrontWindow(IFrontResourceBrowser resourceBrowser, IFrontSettings settingsService, IFrontDownloadTasks downloads, IFrontLauncher launcher, string mode,
        Func<CancellationToken, Task>? startupAction = null)
    {
        this.launcher = launcher;
        this.startupAction = startupAction;
        InitializeComponent();
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 48,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(10),
            UseAeroCaptionButtons = false
        });
        WindowChrome.SetIsHitTestVisibleInChrome(NavigationPanel, true);
        WindowChrome.SetIsHitTestVisibleInChrome(HeaderBar.Children.OfType<StackPanel>().First(), true);
        ToolTip = mode;
        resources = new ResourceBrowserViewModel(resourceBrowser, launcher, launcher) { Destination = launcher.DownloadDirectory };
        browser = new ResourceBrowserView(resources);
        settings = new SettingsViewModel(settingsService);
        settingsView = new SettingsView(settings);
        downloadsView = new DownloadTasksView(new DownloadTasksViewModel(downloads));
        launcherView = new LauncherView(launcher);
        resources.CurrentInstance = () => launcher is IFrontVersions versions && launcherView.SelectedProfile is { } key
            ? versions.ReadInstance(key) : null;
        logView = new LogView(launcher, () => launcherView.SelectedProfile);
        launcherView.RequestPage += NavigatePage;
        downloadView = new DownloadBrowserView(launcher, browser, resources);
        launcherView.MenuPages = MenuPages;
        FrontMenuContext Context() => new(this, launcher, launcherView.SelectedProfile, resources.Selected);
        MenuPages.Attach(FrontMenuArea.Top, NavigationPanel, Context);
        var more = new StackPanel { Orientation = Orientation.Horizontal };
        NavigationPanel.Children.Add(more);
        MenuPages.Attach(FrontMenuArea.More, more, Context);
        MenuPages.Wrap(FrontMenuArea.Download, downloadView, Context);
        MenuPages.Wrap(FrontMenuArea.Settings, settingsView, Context);
        MenuPages.Wrap(FrontMenuArea.ResourceDetails, browser, Context);
        MenuPages.Wrap(FrontMenuArea.Home, launcherView, Context);
        MenuPages.Wrap(FrontMenuArea.InstanceContent, launcherView, Context);
        if (launcher is IFrontPreferences preferences)
        {
            settingsView.ConfigurePreferences(preferences);
            settingsView.AppearanceChanged += ApplyAppearance;
            ApplyAppearance(preferences.ReadPreferences());
        }
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += async (_, _) =>
        {
            UpdateStatus();
            try
            {
                await launcher.Tick();
            }
            catch (Exception exception)
            {
                RuntimeStatus.Text = "下载调度失败：" + exception.Message;
            }
        };
        Loaded += Initialize;
        Closing += CloseSafely;
        Closed += (_, _) =>
        {
            timer.Stop();
            lifetime.Dispose();
        };
        StartNavigation.IsChecked = true;
        StateChanged += (_, _) => MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    void MinimizeClick(object sender, RoutedEventArgs args) => WindowState = WindowState.Minimized;

    void MaximizeClick(object sender, RoutedEventArgs args) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void CloseClick(object sender, RoutedEventArgs args) => Close();

    async void Initialize(object sender, RoutedEventArgs args)
    {
        IsEnabled = false;
        try
        {
            await launcher.Initialize(lifetime.Token);
            if (!closing)
            {
                IsEnabled = true;
                if (startupAction != null)
                {
                    try
                    {
                        await startupAction(lifetime.Token);
                    }
                    catch (OperationCanceledException) when (closing)
                    {
                        return;
                    }
                    catch (Exception exception) when (!closing)
                    {
                        MessageBox.Show(this, exception.Message, "脚本启动未完成", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                if (closing)
                    return;
                timer.Start();
                UpdateStatus();
                await launcherView.RefreshProfiles();
            }
        }
        catch (OperationCanceledException) when (closing)
        {
        }
        catch (Exception exception) when (!closing)
        {
            MessageBox.Show(this, exception.Message, "启动恢复失败", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
        catch (Exception) when (closing)
        {
        }
    }

    async void CloseSafely(object? sender, CancelEventArgs args)
    {
        if (canClose)
            return;
        args.Cancel = true;
        if (closing)
            return;
        if (launcher.IsGameRunning && MessageBox.Show(this, "游戏仍在运行。关闭启动器将请求游戏退出，并等待游戏和任务清理完成。继续？",
            "关闭启动器", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            return;
        closing = true;
        IsEnabled = false;
        timer.Stop();
        lifetime.Cancel();
        resources.Dispose();
        settings.Dispose();
        downloadsView.Dispose();
        downloadView.Dispose();
        launcherView.Dispose();
        RuntimeStatus.Text = "正在清理任务；提交中的安装需要完成。游戏未退出时，请先在游戏中保存并退出。";
        try
        {
            await launcher.Shutdown();
            canClose = true;
            Close();
        }
        catch (Exception exception)
        {
            RuntimeStatus.Text = "关闭清理失败：" + exception.Message + "。请再次关闭窗口重试。";
            PageHost.IsEnabled = false;
            NavigationPanel.IsEnabled = false;
            IsEnabled = true;
            closing = false;
        }
    }

    void UpdateStatus()
    {
        RuntimeStatus.Text = launcher.Status;
        launcherView.UpdateGameState();
        if (!ReferenceEquals(PageHost.Content, logView))
            return;
        logView.Update();
    }

    void Navigate(object sender, RoutedEventArgs args)
    {
        if (navigating || sender is not RadioButton { Tag: string page })
            return;
        NavigatePage(page);
    }

    void NavigateMenu(object sender, RoutedEventArgs args)
    {
        if (sender is not MenuItem { Tag: string page })
            return;
        NavigatePage(page);
    }

    void OpenMoreMenu(object sender, RoutedEventArgs args)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    void NavigatePage(string page)
    {
        navigating = true;
        try
        {
            StartNavigation.IsChecked = page is "start" or "accounts" or "content";
            DownloadNavigation.IsChecked = page == "download";
            SettingsNavigation.IsChecked = page == "settings";
        }
        finally
        {
            navigating = false;
        }
        PageHost.Content = null;
        downloadView.DetachContent();
        PageTitle.Text = page switch
        {
            "download" => "下载", "settings" => "设置", "resources" => "内容中心",
            "accounts" => "账号管理", "content" => "内容管理", "logs" => "运行日志",
            "tasks" => "任务与恢复", _ => "启动"
        };
        switch (page)
        {
            case "download":
                PageHost.Content = downloadView;
                downloadView.ShowCategory(downloadView.CurrentCategory);
                break;
            case "settings":
                PageHost.Content = settingsView;
                break;
            case "start":
            case "accounts":
                launcherView.Navigate(page);
                PageHost.Content = launcherView;
                break;
            case "resources":
                PageSubtitle.Text = "发现并下载 Minecraft 内容。";
                browser.ShowContentCenter();
                PageHost.Content = browser;
                browser.ShowResults();
                break;
            case "content":
                PageSubtitle.Text = "管理当前实例中的本地内容。";
                launcherView.Navigate(page);
                PageHost.Content = launcherView;
                break;
            case "logs":
                PageSubtitle.Text = "查看当前 Front 宿主与游戏运行日志。";
                PageHost.Content = logView;
                UpdateStatus();
                break;
            case "tasks":
                PageSubtitle.Text = "查看下载任务与恢复状态。";
                PageHost.Content = downloadsView;
                break;
        }
    }

    readonly DownloadBrowserView downloadView;

    void ApplyAppearance(Preferences value)
    {
        var accent = value.Accent switch
        {
            "青绿" => "#147D78",
            "石墨" => "#526375",
            "紫罗兰" => "#7658A8",
            _ => "#006DB8"
        };
        var palette = new Dictionary<string, string>
        {
            ["ThemeSurfaceBrush"] = value.DarkMode ? "#212837" : "#FFFFFF",
            ["ThemeInputBrush"] = value.DarkMode ? "#283041" : "#F6F9FB",
            ["ThemeTextBrush"] = value.DarkMode ? "#D6E1E8" : "#243B49",
            ["ThemeSelectedBrush"] = value.DarkMode ? "#30415F" : "#E8F2F8",
            ["ThemeBorderBrush"] = value.DarkMode ? "#444F64" : "#D6E1E8"
        };
        foreach (var pair in palette)
            Application.Current.Resources[pair.Key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pair.Value));
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
        HeaderBar.SetResourceReference(Panel.BackgroundProperty, "ThemeSurfaceBrush");
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value.DarkMode ? "#171D2A" : "#F3F7F9"));
        SurfaceRoot.Background = Brushes.Transparent;
        if (!string.IsNullOrWhiteSpace(value.BackgroundPath) && File.Exists(value.BackgroundPath))
        {
            try
            {
                if (new FileInfo(value.BackgroundPath).Length > 40 * 1024 * 1024)
                    throw new IOException("背景图片不能超过 40 MB。");
                using var stream = File.OpenRead(value.BackgroundPath);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                var frame = decoder.Frames[0];
                if (frame.PixelWidth > 32768 || frame.PixelHeight > 32768 || (long)frame.PixelWidth * frame.PixelHeight > 100_000_000)
                    throw new IOException("背景图片尺寸过大。");
                stream.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = Math.Min(2560, frame.PixelWidth);
                image.EndInit();
                image.Freeze();
                SurfaceRoot.Background = new ImageBrush(image)
                {
                    Opacity = double.IsFinite(value.BackgroundOpacity) ? Math.Clamp(value.BackgroundOpacity, 0, 0.65) : 0.22,
                    Stretch = value.BackgroundFit switch
                    {
                        "完整显示" => Stretch.Uniform,
                        "拉伸" => Stretch.Fill,
                        _ => Stretch.UniformToFill
                    }
                };
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or FileFormatException)
            {
                SurfaceRoot.Background = Brushes.Transparent;
            }
        }
    }
}
