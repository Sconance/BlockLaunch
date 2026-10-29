using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlockLaunch.Contracts;
using Microsoft.Win32;

namespace BlockLaunch.Front;

public partial class SettingsView : UserControl
{
    readonly SettingsViewModel model;
    bool initialized;
    IFrontPreferences? preferences;
    public event Action<Preferences>? AppearanceChanged;

    public SettingsView(SettingsViewModel model)
    {
        this.model = model;
        InitializeComponent();
        DataContext = model;
        Loaded += async (_, _) =>
        {
            if (initialized)
                return;
            initialized = true;
            await model.Load();
        };
    }

    async void SaveClick(object sender, RoutedEventArgs args) => await model.Save();
    async void ReloadClick(object sender, RoutedEventArgs args) => await model.Load();

    void BrowseRootClick(object sender, RoutedEventArgs args)
    {
        var picker = new OpenFolderDialog { Title = "选择游戏目录" };
        if (picker.ShowDialog(Window.GetWindow(this)) == true)
            model.Root = picker.FolderName;
    }

    void BrowseJavaClick(object sender, RoutedEventArgs args)
    {
        var picker = new OpenFileDialog { Filter = "Java 程序|java.exe;javaw.exe|应用程序|*.exe" };
        if (picker.ShowDialog(Window.GetWindow(this)) == true)
            model.Java = picker.FileName;
    }

    public void ConfigurePreferences(IFrontPreferences value) => preferences = value;

    void GlobalLaunchClick(object sender, RoutedEventArgs args)
    {
        if (preferences == null)
            return;
        var editor = new LaunchSettingsEditor(preferences.ReadPreferences().Launch);
        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(editor);
        var dialog = Dialog("全局启动设置", body);
        body.Children.Add(new Button { Content = "保存全局启动设置", Command = new ActionCommand(() =>
        {
            preferences.SaveGlobalLaunch(editor.Read());
            _ = model.Load();
            AdvancedStatus.Text = "全局启动设置已保存；有独立配置的实例不受影响。";
            dialog.Close();
        }) });
        dialog.ShowDialog();
    }

    void NetworkClick(object sender, RoutedEventArgs args)
    {
        if (preferences == null)
            return;
        var value = preferences.ReadPreferences();
        var proxy = new ComboBox { ItemsSource = new[] { "系统代理", "直连", "自定义代理" }, SelectedItem = value.Network.ProxyMode };
        var address = new TextBox { Text = value.Network.ProxyAddress };
        var source = new ComboBox { ItemsSource = new[] { "仅官方", "官方优先，失败切换 BMCLAPI", "BMCLAPI 优先" }, SelectedItem = value.Network.DownloadSource };
        var limit = new TextBox { Text = value.Network.LimitKibPerSecond.ToString() };
        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(new TextBlock { Text = "代理方式" }); body.Children.Add(proxy);
        body.Children.Add(new TextBlock { Text = "代理地址" }); body.Children.Add(address);
        body.Children.Add(new TextBlock { Text = "游戏下载源" }); body.Children.Add(source);
        body.Children.Add(new TextBlock { Text = "下载限速 KiB/s（0 为不限速）" }); body.Children.Add(limit);
        var dialog = Dialog("下载与网络", body);
        body.Children.Add(new Button { Content = "保存", Command = new ActionCommand(() =>
        {
            if (!int.TryParse(limit.Text, out var result) || result is < 0 or > 1048576)
                throw new ArgumentException("下载限速必须在 0–1048576 KiB/s 之间。");
            var network = new NetworkSettings { ProxyMode = proxy.Text, ProxyAddress = address.Text.Trim(), DownloadSource = source.Text, LimitKibPerSecond = result };
            preferences.SaveNetwork(network); AdvancedStatus.Text = "网络设置已保存，重启后生效。"; dialog.Close();
        }) });
        dialog.ShowDialog();
    }

    void AppearanceClick(object sender, RoutedEventArgs args)
    {
        if (preferences == null)
            return;
        var value = preferences.ReadPreferences();
        var dark = new CheckBox { Content = "深色模式", IsChecked = value.DarkMode };
        var accent = new ComboBox { ItemsSource = new[] { "蓝色", "青绿", "石墨", "紫罗兰" }, SelectedItem = value.Accent == "地大蓝" ? "蓝色" : value.Accent };
        var path = new TextBox { Text = value.BackgroundPath };
        var opacity = new TextBox { Text = value.BackgroundOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        var fit = new ComboBox { ItemsSource = new[] { "裁剪填充", "完整显示", "拉伸" }, SelectedItem = value.BackgroundFit };
        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(dark); body.Children.Add(new TextBlock { Text = "主题色" }); body.Children.Add(accent);
        body.Children.Add(new TextBlock { Text = "背景图片路径（可留空）" }); body.Children.Add(path);
        var browse = new Button { Content = "选择背景图片…", HorizontalAlignment = HorizontalAlignment.Left };
        browse.Click += (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = "背景图片|*.png;*.jpg;*.jpeg;*.bmp;*.webp" };
            if (picker.ShowDialog(Window.GetWindow(this)) == true)
                path.Text = picker.FileName;
        };
        body.Children.Add(browse);
        body.Children.Add(new TextBlock { Text = "背景透明度 0–0.65" }); body.Children.Add(opacity);
        body.Children.Add(new TextBlock { Text = "图片填充方式" }); body.Children.Add(fit);
        var dialog = Dialog("界面与背景", body);
        body.Children.Add(new Button { Content = "保存并应用", Command = new ActionCommand(() =>
        {
            if (!double.TryParse(opacity.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amount)) throw new ArgumentException("背景透明度无效。");
            preferences.SaveAppearance(new(dark.IsChecked == true, accent.Text.Trim(), path.Text.Trim(), amount, fit.Text, value.BackgroundImages));
            AppearanceChanged?.Invoke(preferences.ReadPreferences()); AdvancedStatus.Text = "外观已保存并应用。"; dialog.Close();
        }) });
        dialog.ShowDialog();
    }

    void BackupClick(object sender, RoutedEventArgs args)
    {
        if (preferences == null)
            return;
        var value = preferences.ReadPreferences();
        var enabled = new CheckBox { Content = "操作前自动备份存档", IsChecked = value.ProtectSaves };
        var count = new TextBox { Text = value.SaveBackupCount.ToString() };
        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(enabled); body.Children.Add(new TextBlock { Text = "保留最近几次备份（1–100）" }); body.Children.Add(count);
        var dialog = Dialog("存档保护", body);
        body.Children.Add(new Button { Content = "保存策略", Command = new ActionCommand(() =>
        {
            if (!int.TryParse(count.Text, out var keep)) throw new ArgumentException("保留数量应为 1–100。");
            preferences.SaveBackupPolicy(enabled.IsChecked == true, keep); AdvancedStatus.Text = "存档保护策略已保存。"; dialog.Close();
        }) });
        dialog.ShowDialog();
    }

    Window Dialog(string title, UIElement content) => new()
    {
        Owner = Window.GetWindow(this), Title = title, Width = 460, Height = 520,
        Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, WindowStartupLocation = WindowStartupLocation.CenterOwner
    };

    sealed class ActionCommand(Action action) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { try { action(); } catch (Exception exception) { MessageBox.Show(exception.Message, "保存失败"); } }
    }
}
