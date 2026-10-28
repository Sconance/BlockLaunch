using System.IO;
using System.Windows;

namespace BlockLaunch;

public partial class MainWindow
{
    internal async Task Refresh()
    {
        OperationStatusLabel.Text = "读取版本列表…";
        var snapshot = await versionManagement.Read(prefs.Root, CurrentToken);
        if (!Path.GetFullPath(snapshot.Root).Equals(Path.GetFullPath(prefs.Root), StringComparison.OrdinalIgnoreCase))
            return;
        rows = snapshot.Versions;
        Log(snapshot.OnlineError == null
            ? $"读取到 {rows.Count} 个版本，{snapshot.LocalVersions.Count} 个本地版本。"
            : "在线列表不可用，已加载本地版本：" + snapshot.OnlineError);

        Filter();
        RenderVanillaDownloads();
        UpdateTargets();
        if (sketchHome != null)
            RefreshInstanceViews();
    }

    internal HashSet<string> LocalVersions() => versionManagement.LocalVersions(prefs.Root);

    HashSet<string> ProfileKeys() => versionManagement.ProfileKeys(prefs.Root);

    void Filter()
    {
        var previous = VersionSelectorControl.SelectedValue as string;
        var local = LocalVersions();
        VersionSelectorControl.ItemsSource = rows.Where(r => local.Contains(r.Id)).ToList();
        if (previous != null)
            VersionSelectorControl.SelectedValue = previous;
        else if (rows.Any(r => r.Id == prefs.LastVersion))
            VersionSelectorControl.SelectedValue = prefs.LastVersion;
        else if (rows.Any(r => r.Id == "1.21.1"))
            VersionSelectorControl.SelectedValue = "1.21.1";
        if (VersionSelectorControl.SelectedIndex < 0 && VersionSelectorControl.Items.Count > 0)
            VersionSelectorControl.SelectedIndex = 0;
        InstalledVersionsHintLabel.Text = local.Count == 0 ? "尚未安装游戏，请到「游戏安装」选择版本并下载。" : "这里只显示已安装版本；其他版本请到「游戏安装」下载。";
        LaunchGameButtonControl.IsEnabled = !busy && VersionSelectorControl.SelectedItem != null;
    }

    internal string Selected() => VersionSelectorControl.SelectedValue as string ?? throw new InvalidOperationException("请先选择一个游戏版本。");

    internal string Instance() => versionManagement.InstanceDirectory(prefs.Root, Selected(), prefs.Launch);

    Task Install()
    {
        var id = downloadVersion ?? SelectedLaunchVersion();
        EnqueueDownload("下载 / 修复游戏 · " + id, id, new()
        {
            Version = id
        });
        return Task.CompletedTask;
    }

    async Task InstallFabric()
    {
        var id = downloadVersion ?? Selected();
        OperationStatusLabel.Text = "读取 Fabric 加载器…";
        var request = await versionManagement.StableFabric(id, rows, CurrentToken);
        EnqueueDownload("Fabric · " + id, id, request);
    }

    async void RefreshClick(object s, RoutedEventArgs e) => await Run(Refresh);

    void InstallClick(object s, RoutedEventArgs e)
    {
        try
        {
            var id = downloadVersion ?? SelectedLaunchVersion();
            EnqueueDownload("下载 / 修复游戏 · " + id, id, new()
            {
                Version = id
            });
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    async void FabricClick(object s, RoutedEventArgs e) => await Run(InstallFabric);

    void FilterChanged(object s, RoutedEventArgs e)
    {
        if (xamlReady && VersionSelectorControl != null)
            Filter();
    }
}
