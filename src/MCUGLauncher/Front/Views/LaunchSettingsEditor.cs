using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch.Front;

public sealed class LaunchSettingsEditor : StackPanel
{
    readonly CheckBox auto = new() { Content = "自动分配内存" };
    readonly TextBox ram = new();
    readonly TextBox java = new();
    readonly TextBox width = new();
    readonly TextBox height = new();
    readonly CheckBox fullScreen = new() { Content = "全屏启动" };
    readonly ComboBox isolation = new() { ItemsSource = new[] { "独立实例", "共享根目录" } };
    readonly ComboBox visibility = new() { ItemsSource = new[] { "保持显示", "启动后最小化", "启动后隐藏" } };
    readonly ComboBox priority = new() { ItemsSource = new[] { "Normal", "BelowNormal", "AboveNormal", "High" } };
    readonly CheckBox trim = new() { Content = "启动后压缩启动器内存" };
    readonly TextBox title = new();
    readonly TextBox versionInfo = new();
    readonly TextBox arguments = new();
    readonly TextBox server = new();

    public LaunchSettingsEditor(LaunchSettings settings)
    {
        Field("内存", auto);
        Field("手动内存（MB）", ram);
        Field("Java 路径（留空自动选择）", java);
        var browse = new Button { Content = "选择 Java 文件", HorizontalAlignment = HorizontalAlignment.Left };
        browse.Click += (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = "Java 程序|java.exe;javaw.exe|应用程序|*.exe" };
            if (picker.ShowDialog(Window.GetWindow(this)) == true)
                java.Text = picker.FileName;
        };
        Children.Add(browse);
        Field("窗口宽度", width);
        Field("窗口高度", height);
        Field("显示", fullScreen);
        Field("实例隔离", isolation);
        Field("启动器可见性", visibility);
        Field("游戏进程优先级", priority);
        Field("内存优化", trim);
        Field("窗口标题", title);
        Field("版本信息", versionInfo);
        Field("额外 JVM 参数", arguments);
        Field("自动加入服务器（可留空）", server);
        Load(settings);
    }

    void Field(string label, UIElement input)
    {
        Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 4) });
        Children.Add(input);
    }

    public void Load(LaunchSettings settings)
    {
        auto.IsChecked = settings.AutoMemory;
        ram.Text = settings.RamMb.ToString();
        java.Text = settings.Java;
        width.Text = settings.Width.ToString();
        height.Text = settings.Height.ToString();
        fullScreen.IsChecked = settings.FullScreen;
        isolation.SelectedItem = settings.Isolation;
        visibility.SelectedItem = settings.Visibility;
        priority.SelectedItem = settings.Priority;
        trim.IsChecked = settings.TrimLauncher;
        title.Text = settings.WindowTitle;
        versionInfo.Text = settings.VersionInfo;
        arguments.Text = settings.JvmArguments;
        server.Text = settings.Server;
    }

    public LaunchSettings Read()
    {
        if (!int.TryParse(ram.Text, out var memory) || !int.TryParse(width.Text, out var windowWidth) || !int.TryParse(height.Text, out var windowHeight))
            throw new ArgumentException("内存与窗口尺寸必须是整数。");
        return new()
        {
            AutoMemory = auto.IsChecked == true, RamMb = memory, Java = java.Text.Trim(),
            Width = windowWidth, Height = windowHeight, FullScreen = fullScreen.IsChecked == true,
            Isolation = isolation.Text, Visibility = visibility.Text, Priority = priority.Text,
            TrimLauncher = trim.IsChecked == true, WindowTitle = title.Text, VersionInfo = versionInfo.Text,
            JvmArguments = arguments.Text, Server = server.Text
        };
    }
}
