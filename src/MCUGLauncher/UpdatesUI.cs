using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace BlockLaunch;
public partial class MainWindow
{
    async Task OpenContentUpdates(string directory, string key, string minecraft, string loader, string? project = null, object? owner = null)
    {
        List<ContentUpdate>? updates = null;
        await Run(async () => { RequireStopped(); updates = await ContentUpdates.Check(new ContentService(http, CurrentToken), directory, minecraft, loader, CurrentToken); }, "检查兼容更新");
        if (updates == null) return;
        var panel = new StackPanel(); panel.Children.Add(Heading("选择更新内容"));
        panel.Children.Add(Hint("仅检查已管理的 Modrinth 内容。先勾选项目，再预览版本、必需依赖和文件变化。"));
        var selected = new List<(CheckBox Box, ContentVersion Version)>();
        foreach (var update in updates.Where(u => project == null || u.Current.ProjectId == project))
        {
            var box = new CheckBox { Content = new TextBlock { Text = update.Label, TextWrapping = TextWrapping.Wrap }, IsEnabled = update.Latest != null, IsChecked = update.Latest != null, Margin = new Thickness(0, 5, 0, 8) };
            panel.Children.Add(box); if (update.Latest != null) selected.Add((box, update.Latest));
        }
        if (selected.Count == 0) panel.Children.Add(Hint("没有可选择的兼容正式版更新。"));
        var dialog = FeatureDialog("内容更新", panel); dialog.Owner = owner ?? this;
        var choices = new WrapPanel(); choices.Children.Add(ActionButton("全选", (_, _) => selected.ForEach(x => x.Box.IsChecked = true))); choices.Children.Add(ActionButton("全不选", (_, _) => selected.ForEach(x => x.Box.IsChecked = false))); panel.Children.Add(choices);
        panel.Children.Add(ActionButton("预览所选更新", async (_, _) =>
        {
            var roots = selected.Where(x => x.Box.IsChecked == true).Select(x => x.Version).ToList();
            ContentUpdatePreview? preview = null;
            await Run(async () => { RequireStopped(); preview = await ContentUpdates.Preview(new ContentService(http, CurrentToken), directory, roots, minecraft, loader, CurrentToken); }, "解析更新与依赖");
            if (preview == null) return;
            var details = new StackPanel(); details.Children.Add(Heading("确认更新清单")); details.Children.Add(Hint($"{preview.Changes.Count} 项（含必需依赖） · 文件总量约 {preview.Bytes / 1024d / 1024:N1} MiB，已校验缓存可复用\n禁用状态会保留，原文件保留为回退备份。"));
            foreach (var change in preview.Changes) details.Children.Add(Hint(change.Label));
            var confirm = FeatureDialog("更新预览", details); confirm.Owner = dialog;
            details.Children.Add(ActionButton("确认并加入下载队列", (_, _) => confirm.DialogResult = true));
            if (confirm.ShowDialog() != true) return;
            EnqueueDownload("批量内容更新 · " + preview.Changes.Count + " 项", key, new() { Kind = "content", PreparedPlan = preview.Plan, ContentFingerprint = preview.Fingerprint, Destination = directory, GameVersion = minecraft, Loader = loader });
            dialog.Close();
        }));
        dialog.Closing += (_, e) => { if (busy) e.Cancel = true; }; dialog.ShowDialog();
    }
    async void CheckAllUpdatesClick(object sender, RoutedEventArgs e)
    {
        if (busy) return; try { var target = Target(); await OpenContentUpdates(ManagedDirectory(), target.Version, target.GameVersion, target.Loader); } catch (Exception ex) { InlineMessageBox.Show(ex.Message); }
    }
    async void RestoreContentClick(object sender, RoutedEventArgs e) => await RestoreContent(ManagedDirectory());
    async Task RestoreContent(string directory)
    {
        if (busy || InlineMessageBox.Show("回退最近一次内容安装/更新？\n更新后手动改动的文件会阻止回退。存档不受影响。", "内容回退", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
        await Run(async () => { RequireStopped(); var token = CurrentToken; await Task.Run(() => ContentTransaction.RestoreLatest(directory, token), token); RefreshLocalContent(); Log("最近一次内容操作已回退。"); }, "回退内容更新");
    }
}
