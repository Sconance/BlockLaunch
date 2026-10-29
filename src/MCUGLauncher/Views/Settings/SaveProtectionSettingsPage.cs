using System.IO;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed record SaveProtectionSettingsActions(Func<Preferences> Preferences, Action Save,
    Func<bool> IsBusy, Func<Func<Task>, string, Task> Run, Action RequireStopped,
    Func<CancellationToken> Token, Action<string> OpenFolder);

/// <summary>Per-instance backup UI; launch/task state is supplied by explicit operations.</summary>
internal sealed class SaveProtectionSettingsPage(string directory, string backupRoot, SaveProtectionSettingsActions operations)
{
    internal record SaveChoice(SaveBackup Backup, SaveBackupItem World, string Label);
    internal SettingsPageContent Build()
    {
        try
        {
            var body = new StackPanel();
            body.Children.Add(FeatureUiElements.Heading("存档保护与恢复"));
            body.Children.Add(FeatureUiElements.Hint("修改加载器和确认整合包升级前备份。恢复创建新世界，不覆盖现有存档。备份按当前实例独立保留。"));
            var enabled = new CheckBox
            {
                Content = "操作前自动备份存档",
                IsChecked = operations.Preferences().ProtectSaves
            };
            var count = new TextBox
            {
                Text = operations.Preferences().SaveBackupCount.ToString()
            };
            body.Children.Add(enabled);
            body.Children.Add(FeatureUiElements.Hint("保留最近几次备份（1–100）"));
            body.Children.Add(count);
            var choices = new ListBox
            {
                MinHeight = 70,
                MaxHeight = 220,
                DisplayMemberPath = "Label"
            };
            body.Children.Add(choices);
            var preview = FeatureUiElements.Hint("选择备份查看内容。");
            body.Children.Add(preview);
            void RefreshList()
            {
                choices.ItemsSource = SaveProtection.List(backupRoot, directory).SelectMany(b => b.Worlds.Select(w => new SaveChoice(b, w, $"{b.Created.ToLocalTime():yyyy-MM-dd HH:mm} · {w.World} · {w.Version} · {b.Reason}"))).ToList();
            }

            RefreshList();
            choices.SelectionChanged += (_, _) =>
            {
                if (choices.SelectedItem is SaveChoice item)
                {
                    var path = SaveProtection.ArchivePath(backupRoot, directory, item.Backup, item.World);
                    using var zip = System.IO.Compression.ZipFile.OpenRead(path);
                    preview.Text = $"将恢复为新世界：{item.World.World}\n保存版本：{item.World.Version} · {zip.Entries.Count} 个归档条目 · {new FileInfo(path).Length / 1048576d:0.0} MiB\n原世界保持不变。";
                }
            };
            body.Children.Add(FeatureUiElements.ActionButton("保存策略", (_, _) =>
            {
                if (!int.TryParse(count.Text, out var keep) || keep is < 1 or > 100)
                {
                    preview.Text = "保留数量应为 1–100。";
                    return;
                }

                operations.Preferences().ProtectSaves = enabled.IsChecked == true;
                operations.Preferences().SaveBackupCount = keep;
                operations.Save();
                preview.Text = "策略已保存。";
            }));
            body.Children.Add(FeatureUiElements.ActionButton("立即备份所有世界", async (_, _) =>
            {
                if (operations.IsBusy())
                {
                    preview.Text = "请等待当前任务完成。";
                    return;
                }

                await operations.Run(async () =>
                {
                    operations.RequireStopped();
                    await Task.Run(() => SaveProtection.Create(directory, backupRoot, "手动备份", operations.Preferences().SaveBackupCount, operations.Token()), operations.Token());
                }, "备份存档");
                RefreshList();
            }));
            body.Children.Add(FeatureUiElements.ActionButton("确认预览，恢复为新世界", async (_, _) =>
            {
                if (choices.SelectedItem is not SaveChoice item || operations.IsBusy())
                    return;
                var path = SaveProtection.ArchivePath(backupRoot, directory, item.Backup, item.World);
                await operations.Run(async () =>
                {
                    operations.RequireStopped();
                    var restored = await Task.Run(() => WorldArchives.Import(directory, path, item.World.World + "-恢复", token: operations.Token()), operations.Token());
                    preview.Text = "已恢复为独立世界：" + restored;
                }, "恢复存档副本");
            }));
            body.Children.Add(FeatureUiElements.ActionButton("打开备份目录", (_, _) => operations.OpenFolder(SaveProtection.DirectoryFor(backupRoot, directory))));
            return new(new SettingsToolView(body));
        }
        catch (Exception ex)
        {
            return new(FeatureUiElements.Hint(ex.Message));
        }
    }
}
