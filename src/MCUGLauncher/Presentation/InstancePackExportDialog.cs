using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed record InstancePackExportActions(
    Func<bool> IsBusy,
    Func<string> GetRoot,
    Func<LaunchSettings> GetGlobalLaunch,
    Func<string, StackPanel, InlineDialog> FeatureDialog,
    Func<Func<Task>, string, Task> Run,
    Action RequireStopped,
    Func<string, PackRequirements> RequirementsFor,
    Func<CancellationToken> GetToken,
    Action<string> Log);

internal sealed class InstancePackExportDialog(InstancePackExportActions host)
{
    public void Show(string key)
    {
        if (host.IsBusy())
            return;
        var descriptor = new InstanceCatalog(host.GetRoot()).GetOrCreate(key);
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("导出 MultiMC / Prism 兼容整合包"));
        body.Children.Add(FeatureUiElements.Hint("按所选内容打包当前实例，自动生成组件清单。关闭游戏后导出；分享模组本体前请确认分发许可。"));
        CheckBox Check(string title, bool selected)
        {
            var checkBox = new CheckBox
            {
                Content = title,
                IsChecked = selected,
                Margin = new Thickness(0, 9, 0, 9)
            };
            body.Children.Add(checkBox);
            return checkBox;
        }

        var mods = Check("包含模组本体（不勾选时仅导出配置）", true);
        var servers = Check("包含服务器列表 servers.dat", false);
        var shaders = Check("包含光影", true);
        var resources = Check("包含资源包", true);
        var saves = Check("包含存档", false);
        var dialog = host.FeatureDialog("导出整合包", body);
        body.Children.Add(FeatureUiElements.ActionButton("选择保存位置并导出", async (_, _) =>
        {
            var picker = new SaveFileDialog
            {
                Filter = "整合包 ZIP|*.zip",
                FileName = "BlockLaunch-Pack.zip"
            };
            if (picker.ShowDialog() != true)
                return;
            var options = new ExportOptions(mods.IsChecked == true, servers.IsChecked == true, shaders.IsChecked == true, resources.IsChecked == true, saves.IsChecked == true);
            dialog.Close();
            await host.Run(async () =>
            {
                host.RequireStopped();
                var requirements = host.RequirementsFor(descriptor.LaunchProfileId);
                var directory = (descriptor.Launch ?? host.GetGlobalLaunch()).Isolation == "共享根目录" ? host.GetRoot() : new InstanceCatalog(host.GetRoot()).DirectoryFor(key);
                await InstanceManagement.Export(directory, picker.FileName, descriptor.DisplayName, requirements, options, host.GetToken());
                host.Log("整合包已导出：" + picker.FileName);
            }, "导出整合包");
        }));
        dialog.ShowDialog();
    }
}
