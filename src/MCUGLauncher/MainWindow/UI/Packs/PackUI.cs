using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace BlockLaunch;

public partial class MainWindow
{
    string ContentKind => ContentType.SelectedIndex switch
    {
        1 => "resourcepack",
        2 => "modpack",
        3 => "shader",
        4 => "datapack",
        _ => "mod"
    };

    string SelectedLaunchVersion()
    {
        return new InstanceCatalog(prefs.Root).GetOrCreate(Selected()).LaunchProfileId;
    }

    string VersionLabel(string id, HashSet<string> local)
    {
        var customFile = Path.Combine(prefs.Root, "instances", id, ".blocklaunch-instance.json");
        if (File.Exists(customFile))
        {
            try
            {
                var custom = AtomicFile.ReadJson<InstanceDescriptor>(customFile);
                if (custom.DisplayName != id || custom.Favorite)
                    return (custom.Favorite ? "★ " : "") + custom.DisplayName + (VersionInstallation.IsReady(prefs.Root, id) ? "" : " · 需修复");
            }
            catch
            {
            }
        }

        var record = Path.Combine(prefs.Root, "instances", id, ".blocklaunch-pack.json");
        if (File.Exists(record))
        {
            try
            {
                var pack = JsonSerializer.Deserialize<InstalledPack>(File.ReadAllText(record));
                if (pack != null)
                    return $"▦ {pack.Name} · {pack.Minecraft} · {pack.Loader}";
            }
            catch
            {
            }
        }

        var identity = Path.Combine(prefs.Root, "instances", id, ".blocklaunch-instance.json");
        if (File.Exists(identity))
        {
            try
            {
                var instance = AtomicFile.ReadJson<InstanceDescriptor>(identity);
                if (instance.DisplayName != id)
                    return instance.DisplayName + (VersionInstallation.IsReady(prefs.Root, id) ? "" : " · 需修复");
            }
            catch
            {
            }
        }

        return id + (local.Contains(id) ? "   · 已安装" : !VersionInstallation.IsReady(prefs.Root, id) ? " · 未完成，需修复" : "");
    }

    internal async Task<string> InstallPackGame(PackRequirements requirements)
    {
        var context = operations.Current;
        var installer = new GameInstaller(http, prefs.Root, prefs.Java, text => context?.Report(text), CurrentToken, runtimeNetwork);
        return await installer.Install(requirements);
    }

    async Task ImportPack(string file)
    {
        RequireStopped();
        var optional = PackOptional.IsChecked == true;
        var service = new PackService(http, CurrentToken);
        var result = await service.Import(file, prefs.Root, InstallPackGame, optional, text => operations.Current?.Report(text));
        new InstanceCatalog(prefs.Root).GetOrCreate(result.InstanceId);
        // New profiles must be read by a fresh core; previous manifests may have been cached.
        SetupLauncher();
        await Refresh();
        Versions.SelectedValue = result.InstanceId;
        HomeNav.IsChecked = true;
        Log($"整合包安装完成：{result.Name} / {result.Minecraft} / {result.Loader} {result.LoaderVersion}");
        InlineMessageBox.Show("整合包已安装为独立版本，点击首页「启动游戏」即可运行。", "整合包安装完成");
    }

    async Task DownloadPack(ContentVersion version)
    {
        RequireStopped();
        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault() ?? throw new IOException("整合包版本没有下载文件。");
        if (!file.Filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase))
            throw new IOException("此项目未提供 .mrpack 文件。");
        var cache = Path.Combine(prefs.Root, ".blocklaunch-downloads");
        Directory.CreateDirectory(cache);
        var target = Path.Combine(cache, ContentService.SafeFile(file.Filename));
        Status.Text = "下载并校验整合包…";
        await new PackService(http, CurrentToken).Download(file.Url, target, file.Hashes.GetValueOrDefault("sha512") ?? "", text => operations.Current?.Report(text));
        await ImportPack(target);
    }

    async void ImportPackClick(object s, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "整合包（Modrinth / MultiMC / Prism）|*.mrpack;*.zip"
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            await QueuePackImport(dialog.FileName);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "无法加入队列");
        }
    }

    void NoticesClick(object s, RoutedEventArgs e)
    {
        var file = Path.Combine(settingsDir, "第三方许可与来源.txt");
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BlockLaunch.Resources.Notices.txt")!;
        using var reader = new StreamReader(stream);
        File.WriteAllText(file, reader.ReadToEnd());
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = true });
    }
}
