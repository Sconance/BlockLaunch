using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed record InstanceModActions(
    Func<string, Task> Scan, Func<string, Task> CheckUpdates, Func<string, Task> RestoreContent,
    Func<bool> IsInstalled, Action SearchMods, Func<LocalContent, Task> Update, Action OpenFolder, Action OpenBackups);

/// <summary>The single mod management surface, with an expanding list and explicit per-instance operations.</summary>
internal sealed class InstanceModManagementPage(string directory, PackRequirements requirements, object owner,
    VersionTaskActions tasks, InstanceModActions operations)
{
    internal Grid Build()
    {
        var layout = new Grid();
        layout.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        layout.RowDefinitions.Add(new());
        layout.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        var card = new StackPanel();
        layout.Children.Add(card);
        card.Children.Add(FeatureUiElements.Heading("模组管理"));
        card.Children.Add(FeatureUiElements.Hint("目录：" + Path.Combine(directory, "mods")));
        var query = new TextBox
        {
            ToolTip = "搜索 Mod 文件名"
        };
        var list = new ListBox
        {
            MinHeight = 220,
            DisplayMemberPath = "Detail"
        };
        card.Children.Add(query);
        Grid.SetRow(list, 1);
        layout.Children.Add(list);
        void RefreshMods()
        {
            var items = ContentService.ListLocal(directory).Where(item => Path.GetFileName(Path.GetDirectoryName(item.Path)) == "mods" && item.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase));
            list.ItemsSource = items.Select(item => new VersionModView(item, item.Name + " · " + item.State)).ToList();
        }

        query.TextChanged += (_, _) => RefreshMods();
        RefreshMods();
        var actions = new WrapPanel();
        Grid.SetRow(actions, 2);
        layout.Children.Add(actions);
        actions.Children.Add(VersionSettingsView.Button("打开文件夹", (_, _) => operations.OpenFolder()));
        actions.Children.Add(VersionSettingsView.Button("打开备份目录", (_, _) => operations.OpenBackups()));
        actions.Children.Add(VersionSettingsView.Button("启动前检查", async (_, _) => await operations.Scan(directory)));
        actions.Children.Add(VersionSettingsView.Button("刷新", (_, _) => RefreshMods()));
        actions.Children.Add(VersionSettingsView.Button("启用 / 禁用", async (_, _) =>
        {
            if (list.SelectedItem is not VersionModView item)
                return;
            await tasks.Run(() =>
            {
                tasks.RequireStopped();
                ContentService.Toggle(item.Item);
                RefreshMods();
                return Task.CompletedTask;
            }, "切换 Mod 状态");
        }));
        actions.Children.Add(VersionSettingsView.Button("移除选中 Mod", async (_, _) =>
        {
            if (list.SelectedItem is not VersionModView item)
                return;
            await tasks.Run(() =>
            {
                tasks.RequireStopped();
                ContentService.MoveToBackup(directory, item.Item);
                RefreshMods();
                return Task.CompletedTask;
            }, "移除 Mod");
        }));
        actions.Children.Add(VersionSettingsView.Button("更新选中 Mod", async (_, _) =>
        {
            if (list.SelectedItem is VersionModView item)
                await operations.Update(item.Item);
        }));
        actions.Children.Add(VersionSettingsView.Button("批量检查更新", async (_, _) => await operations.CheckUpdates(directory)));
        actions.Children.Add(VersionSettingsView.Button("回退最近内容操作", async (_, _) =>
        {
            await operations.RestoreContent(directory);
            RefreshMods();
        }));
        actions.Children.Add(VersionSettingsView.Button("导入 Mod", async (_, _) =>
        {
            if (tasks.IsBusy())
                return;
            var picker = new OpenFileDialog
            {
                Filter = "Mod JAR|*.jar",
                Multiselect = true
            };
            if (picker.ShowDialog(owner) != true)
                return;
            await tasks.Run(async () =>
            {
                tasks.RequireStopped();
                if (requirements.Loader == "vanilla")
                    throw new IOException("请先安装加载器，再导入 Mod。");
                await new ContentService(tasks.GetHttp(), tasks.GetToken()).ImportFiles(directory, "mods", picker.FileNames);
                RefreshMods();
            }, "导入 Mod");
        }));
        actions.Children.Add(VersionSettingsView.Button("搜索下载 Mod", (_, _) =>
        {
            if (!operations.IsInstalled())
            {
                InlineMessageBox.Show(owner, "请先完成版本安装。");
                return;
            }

            operations.SearchMods();
        }));
        return layout;
    }
}
