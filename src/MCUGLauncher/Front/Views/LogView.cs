using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public sealed class LogView : DockPanel
{
    readonly IFrontLauncher launcher;
    readonly Func<string?> selected;
    readonly TextBox live = Output();
    readonly TextBox latest = Output();
    readonly TextBlock status = new() { Margin = new Thickness(0, 8, 0, 8), TextWrapping = TextWrapping.Wrap };
    readonly CheckBox follow = new() { Content = "自动滚动", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
    readonly TabControl tabs = new();
    string lastOutput = "";

    public LogView(IFrontLauncher launcher, Func<string?> selected)
    {
        this.launcher = launcher;
        this.selected = selected;
        var toolbar = new WrapPanel();
        var refresh = new Button { Content = "读取当前实例 latest.log" };
        refresh.Click += (_, _) => ReadLatest();
        var copy = new Button { Content = "复制当前日志" };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(tabs.SelectedIndex == 0 ? live.Text : latest.Text);
                status.Text = "日志已复制；分享前请再次检查个人信息。";
            }
            catch (Exception exception) { status.Text = "复制失败：" + exception.Message; }
        };
        toolbar.Children.Add(refresh);
        toolbar.Children.Add(copy);
        toolbar.Children.Add(follow);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);
        SetDock(status, Dock.Bottom);
        Children.Add(status);
        tabs.Items.Add(new TabItem { Header = "实时输出", Content = live });
        tabs.Items.Add(new TabItem { Header = "游戏日志", Content = latest });
        Children.Add(tabs);
    }

    static TextBox Output() => new()
    {
        IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new System.Windows.Media.FontFamily("Consolas")
    };

    public void Update()
    {
        var text = string.Join(Environment.NewLine, launcher.Logs);
        if (text == lastOutput)
            return;
        lastOutput = text;
        live.Text = text;
        if (follow.IsChecked == true)
            live.ScrollToEnd();
    }

    void ReadLatest()
    {
        try
        {
            if (launcher is not IFrontVersions versions || selected() is not { } key)
                throw new InvalidOperationException("请先在启动页选择一个已安装实例。");
            latest.Text = versions.ReadGameLog(key);
            status.Text = "已读取当前选中实例的游戏日志；不自动轮询文件。";
            tabs.SelectedIndex = 1;
        }
        catch (Exception exception) { status.Text = "日志读取失败：" + exception.Message; }
    }
}
