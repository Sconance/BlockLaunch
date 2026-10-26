using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace BlockLaunch;
public partial class MainWindow
{
    bool uninstallPreviewOpen;
    async void UninstallInstance(string key)
    {
        if (busy || uninstallPreviewOpen) return;
        uninstallPreviewOpen = true;
        try
        {
            RequireStopped(); var root = prefs.Root; var defaults = prefs.Launch;
            var plan = await Task.Run(() => InstanceUninstaller.Plan(root, key, defaults));
            var body = new StackPanel { Margin = new Thickness(26) };
            body.Children.Add(new TextBlock { Text = "版本删除确认", FontSize = 23, Foreground = Brushes.Red, Margin = new Thickness(0, 0, 0, 10) });
            body.Children.Add(new Border { Height = 1, Background = Brushes.Red, Margin = new Thickness(0, 0, 0, 14) });
            body.Children.Add(new TextBlock { Text = "你确定要删除版本「" + plan.Name + "」吗？\n该版本对应的存档、资源包、Mod、配置和截图等文件也将一并永久删除。", TextWrapping = TextWrapping.Wrap, FontSize = 14 });
            var shared = new CheckBox { Content = "一并删除共享存档等数据（影响所有共用版本）", Margin = new Thickness(0, 14, 0, 8), Visibility = plan.SharedData ? Visibility.Visible : Visibility.Collapsed };
            if (plan.SharedData) body.Children.Add(Hint("此版本使用共享目录，默认保留共用数据。勾选后会删除所有共用版本的共享存档、Mod、配置、资源包和截图。"));
            body.Children.Add(shared);
            var dialog = new InlineDialog { Owner = (object?)versionSettingsWindow ?? this, Title = "版本删除确认", Width = 510, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.White, Content = body };
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var confirm = ActionButton("确定删除", (_, _) => dialog.DialogResult = true); confirm.Foreground = Brushes.Red; confirm.BorderBrush = Brushes.Red; confirm.BorderThickness = new Thickness(1);
            actions.Children.Add(confirm); actions.Children.Add(ActionButton("取消", (_, _) => dialog.DialogResult = false)); body.Children.Add(actions);
            if (dialog.ShowDialog() != true) return;
            var frozen = plan;
            if (plan.SharedData && shared.IsChecked == true)
            {
                frozen = await Task.Run(() => InstanceUninstaller.Plan(root, key, defaults, true));
                if (frozen.InstanceId != plan.InstanceId || frozen.SharedData != plan.SharedData) throw new System.IO.IOException("实例身份或隔离设置已变化，请重新确认删除。");
            }
            var deleted = false;
            await Run(async () =>
            {
                RequireStopped();
                foreach (var record in operations.History().Where(r => r.Download != null && r.Root.Equals(root, StringComparison.OrdinalIgnoreCase)))
                    if (record.Target == key || record.Download!.Version == key || record.Download.Destination.Length > 0 && frozen.Paths.Any(path => InstanceUninstaller.Inside(record.Download.Destination, path))) operations.ChangePending(record.Id, OperationState.Cancelled);
                try { await Task.Run(() => InstanceUninstaller.Delete(frozen, defaults)); deleted = true; Log("已删除版本及所选数据：" + key); }
                finally { SetupLauncher(); await Refresh(); RefreshInstanceViews(); }
            }, "删除版本 · " + key);
            if (deleted) versionSettingsWindow?.Close();
        }
        catch (Exception ex) { InlineMessageBox.Show((object?)versionSettingsWindow ?? this, ex.Message, "无法删除版本"); }
        finally { uninstallPreviewOpen = false; }
    }
}
