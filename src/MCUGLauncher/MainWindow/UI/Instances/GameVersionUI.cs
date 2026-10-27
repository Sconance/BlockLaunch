using System.IO;
using System.Text.Json;
using System.Windows;

namespace BlockLaunch;

public partial class MainWindow
{
    internal async Task Refresh()
    {
        Status.Text = "读取版本列表…";
        var local = LocalVersions();
        var known = ProfileKeys();
        try
        {
            var all = await launcher.GetAllVersionsAsync(CurrentToken);
            rows = all.Select(v => new VersionRow(v.Name, VersionLabel(v.Name, local), v.Type?.ToString() ?? "local", v.ReleaseTime == default ? null : v.ReleaseTime)).ToList();
            foreach (var id in known.Where(id => !rows.Any(r => r.Id == id)))
                rows.Insert(0, new(id, VersionLabel(id, local), "local"));
            Log($"读取到 {rows.Count} 个版本，{local.Count} 个本地版本。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            rows = known.Select(id => new VersionRow(id, VersionLabel(id, local), "local")).ToList();
            Log("在线列表不可用，已加载本地版本：" + ex.Message);
        }

        Filter();
        RenderVanillaDownloads();
        UpdateTargets();
        if (sketchHome != null)
            RefreshInstanceViews();
    }

    internal HashSet<string> LocalVersions()
    {
        var dir = Path.Combine(prefs.Root, "versions");
        return Directory.Exists(dir) ? Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".json")) && VersionInstallation.IsReady(prefs.Root, Path.GetFileName(d))).Select(Path.GetFileName).OfType<string>().ToHashSet() : new();
    }

    HashSet<string> ProfileKeys()
    {
        var dir = Path.Combine(prefs.Root, "versions");
        return Directory.Exists(dir) ? Directory.GetDirectories(dir).Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".json"))).Select(Path.GetFileName).OfType<string>().ToHashSet() : new();
    }

    void Filter()
    {
        var previous = Versions.SelectedValue as string;
        var local = LocalVersions();
        Versions.ItemsSource = rows.Where(r => local.Contains(r.Id)).ToList();
        if (previous != null)
            Versions.SelectedValue = previous;
        else if (rows.Any(r => r.Id == prefs.LastVersion))
            Versions.SelectedValue = prefs.LastVersion;
        else if (rows.Any(r => r.Id == "1.21.1"))
            Versions.SelectedValue = "1.21.1";
        if (Versions.SelectedIndex < 0 && Versions.Items.Count > 0)
            Versions.SelectedIndex = 0;
        InstalledVersionsHint.Text = local.Count == 0 ? "尚未安装游戏，请到「游戏安装」选择版本并下载。" : "这里只显示已安装版本；其他版本请到「游戏安装」下载。";
        LaunchGameButton.IsEnabled = !busy && Versions.SelectedItem != null;
    }

    string Selected() => Versions.SelectedValue as string ?? throw new InvalidOperationException("请先选择一个游戏版本。");

    internal string Instance()
    {
        var catalog = new InstanceCatalog(prefs.Root);
        var descriptor = catalog.GetOrCreate(Selected());
        return (descriptor.Launch ?? prefs.Launch).Isolation == "共享根目录" ? prefs.Root : catalog.DirectoryFor(Selected());
    }

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
        if (!rows.Any(r => r.Id == id && (r.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || r.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            throw new Exception("请选择一个官方原版版本。");
        Status.Text = "读取 Fabric 加载器…";
        using var list = JsonDocument.Parse(await http.GetStringAsync("https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(id), CurrentToken));
        var item = list.RootElement.EnumerateArray().FirstOrDefault(e => e.GetProperty("loader").GetProperty("stable").GetBoolean());
        if (item.ValueKind == JsonValueKind.Undefined)
            throw new Exception("该版本暂时没有稳定 Fabric 加载器。");
        var loader = item.GetProperty("loader").GetProperty("version").GetString()!;
        EnqueueDownload("Fabric · " + id, id, new()
        {
            Version = id,
            Loader = "fabric",
            LoaderVersion = loader
        });
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
        if (Versions != null)
            Filter();
    }
}
