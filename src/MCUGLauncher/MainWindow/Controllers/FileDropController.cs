using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class FileDropController(MainWindow host)
{
    Border? feedback;

    public void Build()
    {
        var text = new TextBlock
        {
            Text = "松开以导入文件\n支持整合包、模组、资源包、光影、存档及数据包\n整合包将先确认名称；其他资源导入当前所选实例",
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 16,
            Margin = new Thickness(20)
        };
        feedback = new Border
        {
            Child = text,
            Background = new SolidColorBrush(Color.FromArgb(242, 232, 242, 248)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(18),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        host.dropFeedback = feedback;
        host.Dispatcher.BeginInvoke(() =>
        {
            if (feedback.Parent == null && host.Content is Grid shell)
            {
                Grid.SetRow(feedback, 1);
                Panel.SetZIndex(feedback, 100);
                shell.Children.Add(feedback);
            }
        });
        host.PreviewDragOver += (_, e) =>
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            text.Text = host.busy ? "当前任务执行中\n整合包可在任务结束后拖入；其他资源请先暂停下载。" : "松开以导入文件\n支持整合包、模组、资源包、光影、存档及数据包\n整合包先确认名称；其他资源导入当前所选实例";
            feedback.Visibility = Visibility.Visible;
            e.Effects = host.busy ? DragDropEffects.None : DragDropEffects.Copy;
            e.Handled = true;
        };
        host.PreviewDragLeave += (_, e) =>
        {
            if (e.GetPosition(host) is var p && (p.X <= 0 || p.Y <= 0 || p.X >= host.ActualWidth || p.Y >= host.ActualHeight))
                feedback.Visibility = Visibility.Collapsed;
        };
        host.PreviewDrop += (_, _) => feedback.Visibility = Visibility.Collapsed;
        host.Deactivated += (_, _) => feedback.Visibility = Visibility.Collapsed;
    }

    public string? ConfirmPackImport(PackOverview overview, string root)
    {
        var body = new StackPanel();
        body.Children.Add(MainWindow.Hint($"{overview.Format} · Minecraft {overview.Requirements.Minecraft} · {overview.Requirements.Loader}\n安装目录：{root}\n将创建独立实例，确认后加入下载队列。"));
        body.Children.Add(MainWindow.Heading("实例名称"));
        var name = new TextBox
        {
            Text = overview.Name,
            MaxLength = 80
        };
        body.Children.Add(name);
        var validation = MainWindow.Hint("");
        body.Children.Add(validation);
        var page = host.FeatureDialog("确认导入整合包", body);
        var actions = new WrapPanel();
        actions.Children.Add(MainWindow.ActionButton("确认并加入队列", (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text))
            {
                validation.Text = "请输入实例名称。";
                return;
            }

            page.DialogResult = true;
        }));
        actions.Children.Add(MainWindow.ActionButton("取消", (_, _) => page.DialogResult = false));
        body.Children.Add(actions);
        return page.ShowDialog() == true ? name.Text.Trim() : null;
    }

    public string DropTargetDirectory()
    {
        if (host.sketchManagerKey == null)
            return host.Instance();
        var catalog = new InstanceCatalog(host.prefs.Root);
        var descriptor = catalog.GetOrCreate(host.sketchManagerKey);
        return (descriptor.Launch ?? host.prefs.Launch).Isolation == "共享根目录"
            ? host.prefs.Root
            : catalog.DirectoryFor(host.sketchManagerKey);
    }
}
