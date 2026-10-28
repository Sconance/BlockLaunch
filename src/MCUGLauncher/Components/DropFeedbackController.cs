using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;
internal sealed class DropFeedbackController
{
    readonly LauncherWorkspace workspace;
    internal DropFeedbackController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal Border? dropFeedback;
    internal string? sketchManagerKey;
    internal Action? refreshSketchManager;
    internal void BuildDropFeedback()
    {
        var text = new TextBlock
        {
            Text = "松开以导入文件\n支持整合包、模组、资源包、光影、存档及数据包\n整合包将先确认名称；其他资源导入当前所选实例",
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 16,
            Margin = new Thickness(20)
        };
        dropFeedback = new Border
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
        var root = (Grid)workspace.Window.Content;
        // Feature pages are constructed before the final shell: attach after the shell exists.
        workspace.Window.Dispatcher.BeginInvoke(() =>
        {
            if (dropFeedback.Parent == null && workspace.Window.Content is Grid shell)
            {
                Grid.SetRow(dropFeedback, 1);
                Panel.SetZIndex(dropFeedback, 100);
                shell.Children.Add(dropFeedback);
            }
        });
        workspace.Window.PreviewDragOver += (_, e) =>
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            text.Text = workspace.LaunchCoordinator.busy ? "当前任务执行中\n整合包可在任务结束后拖入；其他资源请先暂停下载。" : "松开以导入文件\n支持整合包、模组、资源包、光影、存档及数据包\n整合包先确认名称；其他资源导入当前所选实例";
            dropFeedback.Visibility = Visibility.Visible;
            e.Effects = workspace.LaunchCoordinator.busy ? DragDropEffects.None : DragDropEffects.Copy;
            e.Handled = true;
        };
        workspace.Window.PreviewDragLeave += (_, e) =>
        {
            if (e.GetPosition(workspace.Window) is var p && (p.X <= 0 || p.Y <= 0 || p.X >= workspace.Window.ActualWidth || p.Y >= workspace.Window.ActualHeight))
                dropFeedback.Visibility = Visibility.Collapsed;
        };
        workspace.Window.PreviewDrop += (_, _) => dropFeedback.Visibility = Visibility.Collapsed;
        workspace.Window.Deactivated += (_, _) => dropFeedback.Visibility = Visibility.Collapsed;
    }

    internal string? ConfirmPackImport(PackOverview overview, string root)
    {
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Hint($"{overview.Format} · Minecraft {overview.Requirements.Minecraft} · {overview.Requirements.Loader}\n安装目录：{root}\n将创建独立实例，确认后加入下载队列。"));
        body.Children.Add(FeatureUiElements.Heading("实例名称"));
        var name = new TextBox
        {
            Text = overview.Name,
            MaxLength = 80
        };
        body.Children.Add(name);
        var validation = FeatureUiElements.Hint("");
        body.Children.Add(validation);
        var page = workspace.InstancePagesController.FeatureDialog("确认导入整合包", body);
        var actions = new WrapPanel();
        actions.Children.Add(FeatureUiElements.ActionButton("确认并加入队列", (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text))
            {
                validation.Text = "请输入实例名称。";
                return;
            }

            page.DialogResult = true;
        }));
        actions.Children.Add(FeatureUiElements.ActionButton("取消", (_, _) => page.DialogResult = false));
        body.Children.Add(actions);
        return page.ShowDialog() == true ? name.Text.Trim() : null;
    }

    internal string DropTargetDirectory()
    {
        if (sketchManagerKey == null)
            return workspace.LaunchCoordinator.Instance();
        var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
        var d = catalog.GetOrCreate(sketchManagerKey);
        return (d.Launch ?? workspace.LaunchCoordinator.prefs.Launch).Isolation == "共享根目录" ? workspace.LaunchCoordinator.prefs.Root : catalog.DirectoryFor(sketchManagerKey);
    }
}
