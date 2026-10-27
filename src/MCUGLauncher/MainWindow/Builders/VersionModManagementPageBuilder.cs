using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class VersionModManagementPageBuilder(VersionSettingsPageContext context)
{
    MainWindow host = context.Host;

    public void Build()
    {
        var card = context.Card("此版本的 Mod");
        card.Children.Add(MainWindow.Hint("目录：" + Path.Combine(context.DataDirectory(), "mods")));
        var query = new TextBox { ToolTip = "搜索 Mod 文件名" };
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
            var items = ContentService.ListLocal(context.DataDirectory())
                .Where(item => Path.GetFileName(Path.GetDirectoryName(item.Path)) == "mods" && item.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase));
            list.ItemsSource = items.Select(item => new VersionModView(item, item.Name + " · " + item.State)).ToList();
        }

        query.TextChanged += (_, _) => RefreshMods();
        RefreshMods();
        var actions = new WrapPanel();
        card.Children.Add(actions);
        actions.Children.Add(context.Button("启动前检查", async (_, _) => await host.ShowModScan(context.Key, context.DataDirectory(), context.Window)));
        actions.Children.Add(context.Button("刷新", (_, _) => RefreshMods()));
        actions.Children.Add(context.Button("启用 / 禁用", async (_, _) =>
        {
            if (list.SelectedItem is not VersionModView item)
                return;
            await host.Run(() =>
            {
                host.RequireStopped();
                ContentService.Toggle(item.Item);
                RefreshMods();
                return Task.CompletedTask;
            }, "切换 Mod 状态");
        }));
        actions.Children.Add(context.Button("移除选中 Mod", async (_, _) =>
        {
            if (list.SelectedItem is not VersionModView item)
                return;
            await host.Run(() =>
            {
                host.RequireStopped();
                ContentService.MoveToBackup(context.DataDirectory(), item.Item);
                RefreshMods();
                return Task.CompletedTask;
            }, "移除 Mod");
        }));
        actions.Children.Add(context.Button("批量检查更新", async (_, _) =>
            await host.OpenContentUpdates(context.DataDirectory(), context.Key, context.Requirements.Minecraft, context.Requirements.Loader, owner: context.Window)));
        actions.Children.Add(context.Button("回退最近内容操作", async (_, _) =>
        {
            await host.RestoreContent(context.DataDirectory());
            RefreshMods();
        }));
        actions.Children.Add(context.Button("导入 Mod", async (_, _) =>
        {
            if (host.busy)
                return;
            var picker = new OpenFileDialog { Filter = "Mod JAR|*.jar", Multiselect = true };
            if (picker.ShowDialog(context.Window) != true)
                return;
            await host.Run(async () =>
            {
                host.RequireStopped();
                if (context.Requirements.Loader == "vanilla")
                    throw new IOException("请先安装加载器，再导入 Mod。");
                await new ContentService(host.http, host.CurrentToken).ImportFiles(context.DataDirectory(), "mods", picker.FileNames);
                RefreshMods();
            }, "导入 Mod");
        }));
        actions.Children.Add(context.Button("搜索下载 Mod", (_, _) =>
        {
            if (!host.LocalVersions().Contains(context.Key))
            {
                InlineMessageBox.Show(context.Window, "请先完成版本安装。");
                return;
            }

            host.Versions.SelectedValue = context.Key;
            host.ContentType.SelectedIndex = 0;
            context.Window.Close();
            host.DiscoverNav.IsChecked = true;
        }));
    }
}
