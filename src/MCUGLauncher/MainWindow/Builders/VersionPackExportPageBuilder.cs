using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class VersionPackExportPageBuilder(VersionSettingsPageContext context)
{
    MainWindow host = context.Host;

    public void Build()
    {
        var export = context.Card("导出整合包");
        export.Children.Add(MainWindow.Hint("生成 MultiMC / Prism 兼容 ZIP，包含 Minecraft 与加载器组件清单。"));

        CheckBox Option(string label, bool selected)
        {
            var check = new CheckBox
            {
                Content = label,
                IsChecked = selected,
                Margin = new Thickness(0, 8, 0, 8)
            };
            export.Children.Add(check);
            return check;
        }

        var mods = Option("包含模组本体", true);
        var servers = Option("包含服务器列表", false);
        var shaders = Option("包含光影", true);
        var resources = Option("包含资源包", true);
        var saves = Option("包含存档", false);
        export.Children.Add(context.Button("选择位置并导出", async (_, _) =>
        {
            if (host.busy)
                return;
            var picker = new SaveFileDialog { Filter = "整合包 ZIP|*.zip", FileName = "BlockLaunch-Pack.zip" };
            if (picker.ShowDialog(context.Window) != true)
                return;
            var options = new ExportOptions(mods.IsChecked == true, servers.IsChecked == true, shaders.IsChecked == true, resources.IsChecked == true, saves.IsChecked == true);
            await host.Run(async () =>
            {
                host.RequireStopped();
                await InstanceManagement.Export(context.DataDirectory(), picker.FileName, context.Descriptor.DisplayName, context.Requirements, options, host.CurrentToken);
                InlineMessageBox.Show(context.Window, "整合包已导出。");
            }, "导出整合包");
        }));
    }
}
