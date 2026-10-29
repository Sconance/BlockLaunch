using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;
using Microsoft.Win32;

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
        var diagnostics = new Button { Content = "导出诊断包" };
        diagnostics.Click += async (_, _) =>
        {
            try
            {
                if (launcher is not IFrontManagement management || selected() is not { } key)
                    throw new InvalidOperationException("请先选择实例。");
                var snapshot = await management.ReadDiagnostics(key, CancellationToken.None);
                var documents = Diagnostics.Preview(snapshot, new());
                var preview = new Window
                {
                    Owner = Window.GetWindow(this), Title = "检查诊断包内容（不会自动上传）",
                    Width = 820, Height = 620, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = DiagnosticPreview(documents)
                };
                if (preview.ShowDialog() != true)
                    return;
                var picker = new SaveFileDialog { Filter = "诊断 ZIP|*.zip", FileName = "MCUG-diagnostics.zip" };
                if (picker.ShowDialog(Window.GetWindow(this)) != true)
                    return;
                await management.ExportDiagnostics(documents, picker.FileName, CancellationToken.None);
                status.Text = "诊断包已导出；分享前请检查内容。";
            }
            catch (Exception exception) { status.Text = "诊断导出失败：" + exception.Message; }
        };
        toolbar.Children.Add(diagnostics);
        toolbar.Children.Add(follow);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);
        SetDock(status, Dock.Bottom);
        Children.Add(status);
        tabs.Items.Add(new TabItem { Header = "实时输出", Content = live });
        tabs.Items.Add(new TabItem { Header = "游戏日志", Content = latest });
        Children.Add(tabs);
    }

    static UIElement DiagnosticPreview(IReadOnlyList<FeedbackDocument> documents)
    {
        var panel = new DockPanel { Margin = new Thickness(14) };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var reviewed = new CheckBox { Content = "我已检查文本，确认可能包含需要隐藏的个人信息。", Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
        var confirm = new Button { Content = "继续导出", IsEnabled = false, MinWidth = 100 };
        var cancel = new Button { Content = "取消", MinWidth = 80 };
        reviewed.Checked += (_, _) => confirm.IsEnabled = true;
        reviewed.Unchecked += (_, _) => confirm.IsEnabled = false;
        confirm.Click += (_, _) => Window.GetWindow(panel)!.DialogResult = true;
        cancel.Click += (_, _) => Window.GetWindow(panel)!.DialogResult = false;
        actions.Children.Add(reviewed);
        actions.Children.Add(confirm);
        actions.Children.Add(cancel);
        DockPanel.SetDock(actions, Dock.Bottom);
        panel.Children.Add(actions);
        var tabs = new TabControl();
        foreach (var document in documents)
            tabs.Items.Add(new TabItem { Header = document.Name, Content = new TextBox
            {
                Text = document.Text, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            } });
        panel.Children.Add(tabs);
        return panel;
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
