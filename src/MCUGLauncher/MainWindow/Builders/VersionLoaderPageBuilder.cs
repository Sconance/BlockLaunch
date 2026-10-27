using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class VersionLoaderPageBuilder(VersionSettingsPageContext context)
{
    MainWindow host = context.Host;
    InstanceDescriptor descriptor = context.Descriptor;
    PackRequirements requirements = context.Requirements;

    public void Build()
    {
        var loaders = context.Card("安装 / 修改模组加载器");
        loaders.Children.Add(MainWindow.Hint("为 Minecraft " + requirements.Minecraft + " 安装指定加载器，生成新的启动版本。原实例与存档保留；现有模组不会自动迁移。"));
        var loader = new ComboBox
        {
            ItemsSource = new[] { "fabric", "quilt", "forge", "neoforge" },
            SelectedIndex = 0
        };
        var versions = new ComboBox { DisplayMemberPath = "Label" };
        loaders.Children.Add(loader);
        loaders.Children.Add(versions);
        loaders.Children.Add(context.Button("获取兼容加载器版本", async (_, _) =>
        {
            var selected = (string)loader.SelectedItem;
            await host.Run(async () =>
            {
                List<LoaderRelease> releases;
                if (selected is "forge" or "neoforge")
                    releases = await LoaderCatalog.Get(host.http, requirements.Minecraft, selected, host.CurrentToken);
                else
                {
                    var api = selected == "fabric" ? "https://meta.fabricmc.net/v2" : "https://meta.quiltmc.org/v3";
                    using var json = JsonDocument.Parse(await host.http.GetStringAsync(api + "/versions/loader/" + Uri.EscapeDataString(requirements.Minecraft), host.CurrentToken));
                    releases = json.RootElement.EnumerateArray()
                        .Select(item => item.GetProperty("loader"))
                        .Select(item => new LoaderRelease(item.GetProperty("version").GetString()!, item.GetProperty("version").GetString()!))
                        .ToList();
                }

                versions.Tag = selected;
                versions.ItemsSource = releases;
                versions.SelectedIndex = releases.Count > 0 ? 0 : -1;
                if (releases.Count == 0)
                    InlineMessageBox.Show(context.Window, "没有此游戏版本可用的加载器版本。");
            }, "读取加载器版本");
        }));
        loaders.Children.Add(context.Button("安装所选加载器", (_, _) =>
        {
            if (host.busy)
                return;
            if (versions.SelectedItem is not LoaderRelease release || versions.Tag as string != loader.SelectedItem as string)
            {
                InlineMessageBox.Show(context.Window, "请先获取当前加载器的兼容版本。");
                return;
            }

            host.EnqueueDownload("安装 " + loader.SelectedItem + " · " + requirements.Minecraft, requirements.Minecraft, new()
            {
                Version = requirements.Minecraft,
                Loader = (string)loader.SelectedItem,
                LoaderVersion = release.Version,
                BackupInstanceDirectory = context.DataDirectory()
            });
            InlineMessageBox.Show(context.Window, "已加入下载队列，可在任务与恢复中查看。");
        }));

        var tools = context.Card("实例维护");
        tools.Children.Add(context.Button("补全游戏文件", (_, _) => host.EnqueueDownload("下载 / 修复 · " + descriptor.DisplayName, context.Key, new() { Version = descriptor.LaunchProfileId })));
        tools.Children.Add(context.Button("复制为独立实例", (_, _) =>
        {
            context.Window.Close();
            host.CloneInstanceDialog(context.Key);
        }));
    }
}
