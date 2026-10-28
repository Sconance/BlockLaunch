using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class PackUpgradeController(MainWindow host, Action<string> selectVersion, Action returnHome)
{
    public async Task Upgrade(string key, string directory, InlineDialog owner)
    {
        if (host.busy)
            return;
        var picker = new OpenFileDialog
        {
            Title = "选择新版整合包（原实例保留）",
            Filter = "整合包|*.mrpack;*.zip"
        };
        if (picker.ShowDialog(owner) != true)
            return;
        var root = host.prefs.Root;
        var descriptor = new InstanceCatalog(root).GetOrCreate(key);
        PackUpgradePreview? preview = null;
        await host.Run(async () =>
        {
            host.RequireStopped();
            preview = await PackUpgrade.Prepare(host.http, root, key, directory, picker.FileName, host.InstallPackGame, host.IncludeOptionalPackContent, text => host.operations.Current?.Report(text), host.CurrentToken);
        }, "准备整合包升级");
        if (preview == null)
            return;
        using (preview)
        {
            string? installed = null;
            var dialog = BuildDialog(preview, owner, async keep =>
            {
                await host.Run(async () =>
                {
                    host.RequireStopped();
                    await host.ProtectWorlds(directory, "升级整合包", host.CurrentToken);
                    installed = await PackUpgrade.Commit(preview, keep, descriptor.Launch ?? host.prefs.Launch, text => host.operations.Current?.Report(text), host.CurrentToken);
                }, "迁移整合包数据");
                return installed != null;
            });
            dialog.ShowDialog();
            if (installed != null)
            {
                owner.Close();
                await host.Run(async () =>
                {
                    await host.Refresh();
                    selectVersion(installed);
                    host.RefreshInstanceViews();
                }, "刷新升级实例");
                InlineMessageBox.Show("升级副本已创建，原实例保留。可在新版本设置中选择“返回升级前实例”。");
            }
        }
    }

    public InlineDialog BuildDialog(PackUpgradePreview preview, object owner, Func<string[], Task<bool>> commit)
    {
        var panel = new StackPanel();
        panel.Children.Add(MainWindow.Heading("升级到 " + preview.Pack.Name));
        panel.Children.Add(MainWindow.Hint($"Minecraft {preview.Pack.Minecraft} · {preview.Pack.Loader} {preview.Pack.LoaderVersion}\n升级创建独立副本，原实例和存档保留。跨游戏版本打开存档可能改变世界格式。\n勾选保留旧文件；取消勾选使用新包文件或移除旧文件。"));
        if (!preview.HasBaseline)
            panel.Children.Add(MainWindow.Hint("旧包未记录文件基线，请逐项检查旧 Mod，避免与新版重复。"));
        foreach (var group in preview.Files.Where(f => !f.Conflict).GroupBy(f => f.Relative.Split('/')[0]))
            panel.Children.Add(MainWindow.Hint("自动保留 " + group.Key + " · " + group.Count() + " 个文件"));
        var choices = new List<(CheckBox Box, PackUpgradeFile File)>();
        var tooMany = preview.Files.Count(f => f.Conflict) > 500;
        if (tooMany)
            panel.Children.Add(MainWindow.Hint("冲突超过 500 项，请先整理旧实例，再重新准备升级。原实例未修改。"));
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

        panel.Children.Add(MainWindow.Hint($"{preview.Files.Count} 项迁移 · {preview.Files.Count(f => f.Conflict)} 项需确认；存档、截图和玩家选项默认保留。"));
        var dialog = host.FeatureDialog("整合包升级预览", panel);
        dialog.Owner = owner;
        var confirm = MainWindow.ActionButton("确认迁移并创建升级副本", async (_, _) =>
        {
            var keep = choices.Where(c => c.Box.IsChecked == true).Select(c => c.File.Relative).Concat(preview.Files.Where(f => !f.Conflict && f.KeepOld).Select(f => f.Relative)).ToArray();
            if (await commit(keep))
                dialog.DialogResult = true;
        });
        confirm.IsEnabled = !tooMany;
        panel.Children.Add(confirm);
        dialog.Closing += (_, e) =>
        {
            if (host.busy)
                e.Cancel = true;
        };
        return dialog;
    }

    public void ReturnToOriginal(string key, InlineDialog owner)
    {
        if (host.busy)
            return;
        try
        {
            host.RequireStopped();
            var file = Path.Combine(new InstanceCatalog(host.prefs.Root).MetadataDirectory(key), ".blocklaunch-upgrade.json");
            var original = AtomicFile.ReadJson<Dictionary<string, string>>(file)["OriginalKey"];
            if (!host.LocalVersions().Contains(original))
                throw new IOException("升级前实例不再存在。");
            selectVersion(original);
            owner.Close();
            returnHome();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "返回原实例");
        }
    }
}
