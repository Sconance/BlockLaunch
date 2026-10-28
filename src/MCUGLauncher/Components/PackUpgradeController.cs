using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class PackUpgradeController
{
    readonly LauncherWorkspace workspace;
    internal PackUpgradeController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal async Task UpgradePack(string key, string directory, InlineDialog owner)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        var picker = new OpenFileDialog
        {
            Title = "选择新版整合包（原实例保留）",
            Filter = "整合包|*.mrpack;*.zip"
        };
        if (picker.ShowDialog(owner) != true)
            return;
        var root = workspace.LaunchCoordinator.prefs.Root;
        var descriptor = new InstanceCatalog(root).GetOrCreate(key);
        PackUpgradePreview? preview = null;
        await workspace.LaunchCoordinator.Run(async () =>
        {
            workspace.ContentController.RequireStopped();
            preview = await PackUpgrade.Prepare(workspace.LaunchCoordinator.http, root, key, directory, picker.FileName, requirements => new GameInstaller(workspace.LaunchCoordinator.http, root, workspace.LaunchCoordinator.prefs.Java, text => workspace.LaunchCoordinator.operations.Current?.Report(text), workspace.LaunchCoordinator.CurrentToken, workspace.LaunchCoordinator.runtimeNetwork).Install(requirements), workspace.Controls.PackOptional.IsChecked == true, text => workspace.LaunchCoordinator.operations.Current?.Report(text), workspace.LaunchCoordinator.CurrentToken);
        }, "准备整合包升级");
        if (preview == null)
            return;
        using (preview)
        {
            string? installed = null;
            var dialog = BuildPackUpgradeDialog(preview, owner, async keep =>
            {
                await workspace.LaunchCoordinator.Run(async () =>
                {
                    workspace.ContentController.RequireStopped();
                    await workspace.ExperienceController.ProtectWorlds(directory, "升级整合包", workspace.LaunchCoordinator.CurrentToken);
                    installed = await PackUpgrade.Commit(preview, keep, descriptor.Launch ?? workspace.LaunchCoordinator.prefs.Launch, text => workspace.LaunchCoordinator.operations.Current?.Report(text), workspace.LaunchCoordinator.CurrentToken);
                }, "迁移整合包数据");
                return installed != null;
            });
            dialog.ShowDialog();
            if (installed != null)
            {
                owner.Close();
                await workspace.LaunchCoordinator.Run(async () =>
                {
                    await workspace.LaunchCoordinator.Refresh();
                    workspace.Controls.Versions.SelectedValue = installed;
                    workspace.InstancePagesController.RefreshInstanceViews();
                }, "刷新升级实例");
                InlineMessageBox.Show("升级副本已创建，原实例保留。可在新版本设置中选择“返回升级前实例”。");
            }
        }
    }

    internal InlineDialog BuildPackUpgradeDialog(PackUpgradePreview preview, object owner, Func<string[], Task<bool>> commit)
    {
        var panel = new StackPanel();
        panel.Children.Add(InstancePagesController.Heading("升级到 " + preview.Pack.Name));
        panel.Children.Add(InstancePagesController.Hint($"Minecraft {preview.Pack.Minecraft} · {preview.Pack.Loader} {preview.Pack.LoaderVersion}\n升级创建独立副本，原实例和存档保留。跨游戏版本打开存档可能改变世界格式。\n勾选保留旧文件；取消勾选使用新包文件或移除旧文件。"));
        if (!preview.HasBaseline)
            panel.Children.Add(InstancePagesController.Hint("旧包未记录文件基线，请逐项检查旧 Mod，避免与新版重复。"));
        foreach (var group in preview.Files.Where(f => !f.Conflict).GroupBy(f => f.Relative.Split('/')[0]))
            panel.Children.Add(InstancePagesController.Hint("自动保留 " + group.Key + " · " + group.Count() + " 个文件"));
        var choices = new List<(CheckBox Box, PackUpgradeFile File)>();
        var tooMany = preview.Files.Count(f => f.Conflict) > 500;
        if (tooMany)
            panel.Children.Add(InstancePagesController.Hint("冲突超过 500 项，请先整理旧实例，再重新准备升级。原实例未修改。"));
        else
            foreach (var file in preview.Files.Where(f => f.Conflict))
            {
                var box = new CheckBox
                {
                    Content = new TextBlock
                    {
                        Text = file.Label,
                        TextWrapping = TextWrapping.Wrap
                    },
                    IsChecked = file.KeepOld,
                    Margin = new Thickness(0, 4, 0, 6)
                };
                panel.Children.Add(box);
                choices.Add((box, file));
            }

        panel.Children.Add(InstancePagesController.Hint($"{preview.Files.Count} 项迁移 · {preview.Files.Count(f => f.Conflict)} 项需确认；存档、截图和玩家选项默认保留。"));
        var dialog = workspace.InstancePagesController.FeatureDialog("整合包升级预览", panel);
        dialog.Owner = owner;
        var confirm = InstancePagesController.ActionButton("确认迁移并创建升级副本", async (_, _) =>
        {
            var keep = choices.Where(c => c.Box.IsChecked == true).Select(c => c.File.Relative).Concat(preview.Files.Where(f => !f.Conflict && f.KeepOld).Select(f => f.Relative)).ToArray();
            if (await commit(keep))
                dialog.DialogResult = true;
        });
        confirm.IsEnabled = !tooMany;
        panel.Children.Add(confirm);
        dialog.Closing += (_, e) =>
        {
            if (workspace.LaunchCoordinator.busy)
                e.Cancel = true;
        };
        return dialog;
    }

    internal void ReturnToOriginal(string key, InlineDialog owner)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        try
        {
            workspace.ContentController.RequireStopped();
            var file = Path.Combine(new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).MetadataDirectory(key), ".blocklaunch-upgrade.json");
            var original = AtomicFile.ReadJson<Dictionary<string, string>>(file)["OriginalKey"];
            if (!workspace.LaunchCoordinator.LocalVersions().Contains(original))
                throw new IOException("升级前实例不再存在。");
            workspace.Controls.Versions.SelectedValue = original;
            owner.Close();
            workspace.Controls.HomeNav.IsChecked = true;
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "返回原实例");
        }
    }
}
