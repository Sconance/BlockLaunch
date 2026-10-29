using System.IO;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed record VersionModActions(
    Func<string, Task> Scan,
    Func<string, Task> CheckUpdates,
    Func<string, Task> RestoreContent,
    Func<bool> IsInstalled,
    Action SearchMods);

/// <summary>Builds the Mod 管理 page using per-instance data and explicit operations.</summary>
internal sealed class VersionModPage(VersionSettingsPageContext context, VersionTaskActions tasks, VersionModActions operations)
{
    internal void Build()
    {
        var card = context.View.Card("此版本的 Mod");
        card.Children.Add(FeatureUiElements.Hint("目录：" + Path.Combine(context.DataDirectory(), "mods")));
        var query = new TextBox
        {
            ToolTip = "搜索 Mod 文件名"
        };
        var list = new ListBox
        {
            MinHeight = 220,
            MaxHeight = 390,
            DisplayMemberPath = "Detail"
        };
        card.Children.Add(query);
        card.Children.Add(list);
        void RefreshMods()
        {
            var items = ContentService.ListLocal(context.DataDirectory()).Where(item => Path.GetFileName(Path.GetDirectoryName(item.Path)) == "mods" && item.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase));
            list.ItemsSource = items.Select(item => new VersionModView(item, item.Name + " · " + item.State)).ToList();
        }

        query.TextChanged += (_, _) => RefreshMods();
        RefreshMods();
        var actions = new WrapPanel();
        card.Children.Add(actions);
        actions.Children.Add(VersionSettingsView.Button("启动前检查", async (_, _) => await operations.Scan(context.DataDirectory())));
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
                ContentService.MoveToBackup(context.DataDirectory(), item.Item);
                RefreshMods();
                return Task.CompletedTask;
            }, "移除 Mod");
        }));
        actions.Children.Add(VersionSettingsView.Button("批量检查更新", async (_, _) => await operations.CheckUpdates(context.DataDirectory())));
        actions.Children.Add(VersionSettingsView.Button("回退最近内容操作", async (_, _) =>
        {
            await operations.RestoreContent(context.DataDirectory());
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
            if (picker.ShowDialog(context.View.Window) != true)
                return;
            await tasks.Run(async () =>
            {
                tasks.RequireStopped();
                if (context.Requirements.Loader == "vanilla")
                    throw new IOException("请先安装加载器，再导入 Mod。");
                await new ContentService(tasks.GetHttp(), tasks.GetToken()).ImportFiles(context.DataDirectory(), "mods", picker.FileNames);
                RefreshMods();
            }, "导入 Mod");
        }));
        actions.Children.Add(VersionSettingsView.Button("搜索下载 Mod", (_, _) =>
        {
            if (!operations.IsInstalled())
            {
                InlineMessageBox.Show(context.View.Window, "请先完成版本安装。");
                return;
            }




            operations.SearchMods();
        }));

    }
}
