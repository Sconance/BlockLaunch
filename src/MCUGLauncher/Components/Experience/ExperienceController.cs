using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class ExperienceController
{
    readonly LauncherWorkspace workspace;
    internal ExperienceController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal string? nextServerAddress;
    internal string SaveBackupRoot => Path.Combine(workspace.LaunchCoordinator.settingsDir, "world-backups");

    internal void BuildExperienceUI()
    {
        var queue = (WrapPanel)((Grid)((TabItem)workspace.Window.MainTabs.Items[6]).Content).Children.OfType<WrapPanel>().Single();
        foreach (var pair in new[]
        {
            ("上移", -1),
            ("下移", 1)
        }

        )
            queue.Children.Add(FeatureUiElements.ActionButton(pair.Item1, (_, _) =>
            {
                if (workspace.Controls.TaskHistoryList.SelectedItem is OperationRecord record)
                {
                    workspace.LaunchCoordinator.operations.MovePending(record.Id, pair.Item2);
                    workspace.TaskRecoveryController.RefreshOperations();
                }
            }));
        queue.Children.Add(FeatureUiElements.ActionButton("下载限速", (_, _) => OpenDownloadLimit()));
        workspace.Window.Width = Math.Min(workspace.Window.Width, Math.Max(780, SystemParameters.WorkArea.Width - 24));
        workspace.Window.Height = Math.Min(workspace.Window.Height, Math.Max(540, SystemParameters.WorkArea.Height - 24));
        workspace.Window.MinWidth = 780;
        workspace.Window.MinHeight = 540;
        workspace.DownloadMiniBarController.miniDownloadProgress.Foreground = (Brush)workspace.Window.FindResource("AccentBrush");
        workspace.Window.Loaded += (_, _) => workspace.ExperienceThemeController.ThemeWindow(workspace.Window);
    }

    internal async Task ProtectWorlds(string directory, string reason, CancellationToken token)
    {
        if (!workspace.LaunchCoordinator.prefs.ProtectSaves)
            return;
        workspace.ResourceBrowserController.RequireStopped();
        var registry = Path.Combine(workspace.LaunchCoordinator.prefs.Root, "instances");
        var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
        var registered = Directory.Exists(registry) && Directory.EnumerateDirectories(registry).Any(d => File.Exists(Path.Combine(d, ".blocklaunch-instance.json")) && Path.GetFullPath(catalog.DirectoryFor(Path.GetFileName(d))).Equals(Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase));
        if (!registered && !InstanceUninstaller.Inside(directory, workspace.LaunchCoordinator.prefs.Root) && !Path.GetFullPath(directory).Equals(Path.GetFullPath(workspace.LaunchCoordinator.prefs.Root), StringComparison.OrdinalIgnoreCase))
            throw new IOException("存档保护目录未登记在当前游戏目录中。");
        var backup = await Task.Run(() => SaveProtection.Create(directory, SaveBackupRoot, reason, workspace.LaunchCoordinator.prefs.SaveBackupCount, token), token);
        if (backup != null)
            workspace.LaunchCoordinator.Log($"存档保护完成：{backup.Worlds.Count} 个世界 · {reason}");
    }

    internal bool ConfirmModScan(ModScan scan, string title)
    {
        var panel = new StackPanel();
        panel.Children.Add(FeatureUiElements.Heading(scan.Summary));
        panel.Children.Add(FeatureUiElements.Hint("只检查已启用 JAR 的可识别元数据；未知条件不当作兼容结论。不会自动移动或删除模组。"));
        foreach (var finding in scan.Findings.Take(200))
            panel.Children.Add(FeatureUiElements.Hint($"{finding.Severity} · {finding.File}\n{finding.Message}"));
        var dialog = workspace.InstanceWorkspaceController.FeatureDialog(title, panel);
        var actions = new WrapPanel();
        actions.Children.Add(FeatureUiElements.ActionButton("返回检查", (_, _) => dialog.DialogResult = false));
        actions.Children.Add(FeatureUiElements.ActionButton("了解风险，继续启动", (_, _) => dialog.DialogResult = true));
        panel.Children.Add(actions);
        return dialog.ShowDialog() == true;
    }

    internal async Task ShowModScan(string key, string directory, object? owner = null)
    {
        try
        {
            var descriptor = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).GetOrCreate(key);
            var req = workspace.InstanceWorkspaceController.RequirementsFor(descriptor.LaunchProfileId);
            var scan = await Task.Run(() => ModPreflight.Scan(directory, req.Minecraft, req.Loader, JavaDiscovery.RequiredMajor(workspace.LaunchCoordinator.prefs.Root, descriptor.LaunchProfileId), req.LoaderVersion));
            var panel = new StackPanel();
            panel.Children.Add(FeatureUiElements.Heading(scan.Summary));
            panel.Children.Add(FeatureUiElements.Hint("扫描启用的 JAR，含 Fabric 内嵌模组；复杂条件或未知元数据会提示人工核对。"));
            foreach (var finding in scan.Findings.Take(200))
                panel.Children.Add(workspace.InstanceWorkspaceController.FeatureCard(new StackPanel { Children = { FeatureUiElements.Heading(finding.Severity + " · " + finding.File), FeatureUiElements.Hint(finding.Message) } }));
            if (scan.Findings.Count == 0)
                panel.Children.Add(FeatureUiElements.Hint("未发现已知元数据问题；仍需由加载器完成实际兼容性验证。"));
            var dialog = workspace.InstanceWorkspaceController.FeatureDialog("本地 Mod 检查", panel);
            dialog.Owner = owner ?? workspace.Window;
            panel.Children.Add(FeatureUiElements.ActionButton("打开 Mod 文件夹", (_, _) => workspace.LaunchCoordinator.OpenFolder(Path.Combine(directory, "mods"))));
            panel.Children.Add(FeatureUiElements.ActionButton("修复后重新检查", async (_, _) =>
            {
                dialog.Close();
                await ShowModScan(key, directory, owner);
            }));
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(owner ?? workspace.Window, ex.Message, "Mod 检查失败");
        }
    }

    internal void OpenDownloadLimit()
    {
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("下载限速"));
        body.Children.Add(FeatureUiElements.Hint("单位 KiB/s，0 为不限速。对启动器 HTTP 下载立即生效；外部 Forge/NeoForge 安装器自身的网络请求不受此限制。"));
        var input = new TextBox
        {
            Text = workspace.LaunchCoordinator.prefs.Network.LimitKibPerSecond.ToString()
        };
        body.Children.Add(input);
        var dialog = workspace.InstanceWorkspaceController.FeatureDialog("下载限速", body);
        dialog.Height = 290;
        body.Children.Add(FeatureUiElements.ActionButton("应用", (_, _) =>
        {
            try
            {
                if (!int.TryParse(input.Text, out var limit) || limit is < 0 or > 1048576)
                    throw new IOException("请输入 0–1048576。");
                workspace.LaunchCoordinator.prefs.Network.LimitKibPerSecond = limit;
                workspace.LaunchCoordinator.runtimeNetwork.LimitKibPerSecond = limit;
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                dialog.Close();
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(dialog, ex.Message);
            }
        }));
        dialog.ShowDialog();
    }

    internal void OpenSaveProtection() => SettingsDialogPresenter.Show("存档保护", workspace.Window,
        _ => CreateSaveProtectionPage(), workspace.ExperienceThemeController.ConfigureUtilityWindow);

    internal SettingsPageContent CreateSaveProtectionPage()
    {
        try
        {
            return new SaveProtectionSettingsPage(workspace.LaunchCoordinator.Instance(), SaveBackupRoot, new(
                () => workspace.LaunchCoordinator.prefs,
                () => workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs),
                () => workspace.LaunchCoordinator.busy,
                (action, name) => workspace.LaunchCoordinator.Run(action, name),
                workspace.ResourceBrowserController.RequireStopped,
                () => workspace.LaunchCoordinator.CurrentToken, workspace.LaunchCoordinator.OpenFolder)).Build();
        }
        catch (Exception ex) { return new(FeatureUiElements.Hint(ex.Message)); }
    }

    internal void OpenServers() => SettingsDialogPresenter.Show("服务器收藏", workspace.Window,
        CreateServersPage, workspace.ExperienceThemeController.ConfigureUtilityWindow);

    internal SettingsPageContent CreateServersPage(Action close) => new ServerSettingsPage(new(
        () => workspace.LaunchCoordinator.prefs,
        () => workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs),
        workspace.LaunchCoordinator.LocalVersions, () => workspace.LaunchCoordinator.busy,
        ConnectFavoriteServer)).Build(workspace.Controls.Versions.SelectedValue as string, close);

    async Task ConnectFavoriteServer(ServerFavorite item)
    {
        workspace.Controls.Versions.SelectedValue = item.Profile;
        nextServerAddress = item.Address;
        try
        {
            await workspace.LaunchCoordinator.Run(workspace.LaunchCoordinator.Launch, "连接收藏服务器");
        }
        finally { nextServerAddress = null; }
    }
}
