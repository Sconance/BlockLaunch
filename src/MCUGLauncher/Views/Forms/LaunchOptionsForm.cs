using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public sealed class LaunchOptionsForm
{
    public StackPanel Panel { get; } = new();

    StackPanel active = new();

    void Section(string title)
    {
        active = new StackPanel();
        active.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
        Panel.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(9), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 14), Child = active });
    }

    public void SetJava(string path) => texts["Java"].Text = path;

    readonly Dictionary<string, TextBox> texts = new();
    readonly Dictionary<string, ComboBox> combos = new();
    readonly Dictionary<string, CheckBox> checks = new();

    void Text(string id, string label, string value)
    {
        active.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 0) });
        var text = new TextBox
        {
            Text = value
        };
        texts[id] = text;
        active.Children.Add(text);
    }

    void Combo(string id, string label, string value, string[] values)
    {
        active.Children.Add(new TextBlock { Text = label });
        var combo = new ComboBox
        {
            ItemsSource = values,
            SelectedItem = value
        };
        combos[id] = combo;
        active.Children.Add(combo);
    }

    void Check(string id, string label, bool value)
    {
        var c = new CheckBox
        {
            Content = label,
            IsChecked = value,
            Margin = new Thickness(0, 8, 0, 12)
        };
        checks[id] = c;
        active.Children.Add(c);
    }

    public LaunchOptionsForm(LaunchSettings settings)
    {
        Section("启动选项");
        Combo("Isolation", "版本隔离", settings.Isolation, ["独立实例", "共享根目录"]);
        Text("WindowTitle", "游戏窗口标题（留空使用默认）", settings.WindowTitle);
        Text("VersionInfo", "自定义版本信息", settings.VersionInfo);
        Combo("Visibility", "启动器可见性", settings.Visibility, ["保持显示", "启动后最小化", "启动后隐藏"]);
        Combo("Priority", "进程优先级", settings.Priority, ["BelowNormal", "Normal", "AboveNormal", "High"]);
        Check("FullScreen", "全屏启动", settings.FullScreen);
        Text("Width", "窗口宽度", settings.Width.ToString());
        Text("Height", "窗口高度", settings.Height.ToString());
        active.Children.Add(new TextBlock { Text = "常用分辨率（可继续修改宽高）", Margin = new Thickness(0, 6, 0, 0) });
        var resolutions = new ComboBox
        {
            ItemsSource = new[] {
                "选择预设…",
                "854 × 480",
                "1280 × 720",
                "1600 × 900",
                "1920 × 1080",
                "2560 × 1440",
                "3840 × 2160"
            },
            SelectedIndex = 0
        };
        resolutions.SelectionChanged += (_, _) =>
        {
            if (resolutions.SelectedIndex > 0)
            {
                var parts = ((string)resolutions.SelectedItem).Split('×');
                texts["Width"].Text = parts[0].Trim();
                texts["Height"].Text = parts[1].Trim();
            }
        };
        active.Children.Add(resolutions);
        Section("内存分配");
        Check("AutoMemory", "自动分配内存（按可用内存推荐）", settings.AutoMemory);
        Text("RamMb", "自定义内存 MB（关闭自动分配后生效）", settings.RamMb.ToString());
        var memory = MemoryPolicy.Read();
        active.Children.Add(new TextBlock { Text = $"系统已用 / 总计：{(memory.Total - memory.Available) / 1073741824.0:F1} / {memory.Total / 1073741824.0:F1} GB · 推荐 {MemoryPolicy.Recommend()} MB", TextWrapping = TextWrapping.Wrap });
        Section("Java 与高级参数");
        Check("TrimLauncher", "启动后收缩启动器工作集（不清理其他进程）", settings.TrimLauncher);
        Text("Java", "Java 路径（留空自动）", settings.Java);
        Text("JvmArguments", "额外 JVM 参数", settings.JvmArguments);
        Text("Server", "启动后连接服务器（可选 host:port）", settings.Server);
    }

    public LaunchSettings Read() => new()
    {
        Isolation = (string)combos["Isolation"].SelectedItem,
        WindowTitle = texts["WindowTitle"].Text,
        VersionInfo = texts["VersionInfo"].Text,
        Visibility = (string)combos["Visibility"].SelectedItem,
        Priority = (string)combos["Priority"].SelectedItem,
        FullScreen = checks["FullScreen"].IsChecked == true,
        Width = int.Parse(texts["Width"].Text),
        Height = int.Parse(texts["Height"].Text),
        RamMb = int.Parse(texts["RamMb"].Text),
        AutoMemory = checks["AutoMemory"].IsChecked == true,
        TrimLauncher = checks["TrimLauncher"].IsChecked == true,
        Java = texts["Java"].Text.Trim().Trim('"'),
        JvmArguments = texts["JvmArguments"].Text,
        Server = texts["Server"].Text.Trim()
    };
}
