using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;


/// <summary>Builds the 导出 page using per-instance data and explicit operations.</summary>
internal sealed class VersionExportPage(VersionSettingsPageContext context, VersionTaskActions tasks, Action exportScript)
{
    internal void Build()
    {
        var export = context.View.Card("导出整合包");
        export.Children.Add(FeatureUiElements.Hint("生成 MultiMC / Prism 兼容 ZIP，包含 Minecraft 与加载器组件清单。"));
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
        export.Children.Add(VersionSettingsView.Button("选择位置并导出", async (_, _) =>
        {
            if (tasks.IsBusy())
                return;
            var picker = new SaveFileDialog
            {
                Filter = "整合包 ZIP|*.zip",
                FileName = "BlockLaunch-Pack.zip"
            };
            if (picker.ShowDialog(context.View.Window) != true)
                return;
            var options = new ExportOptions(mods.IsChecked == true, servers.IsChecked == true, shaders.IsChecked == true, resources.IsChecked == true, saves.IsChecked == true);
            await tasks.Run(async () =>
            {
                tasks.RequireStopped();
                await InstanceManagement.Export(context.DataDirectory(), picker.FileName, context.Descriptor.DisplayName, context.Requirements, options, tasks.GetToken());
                InlineMessageBox.Show(context.View.Window, "整合包已导出。");
            }, "导出整合包");
        }));
        var scripts = context.View.Card("导出启动脚本");
        scripts.Children.Add(FeatureUiElements.Hint("生成调用此启动器的脚本，不保存账号令牌。"));
        scripts.Children.Add(VersionSettingsView.Button("导出启动脚本", (_, _) => exportScript()));

    }
}
