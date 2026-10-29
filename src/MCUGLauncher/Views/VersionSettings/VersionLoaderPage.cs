using System.Text.Json;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed record VersionLoaderActions(
    Action<string, string, DownloadRequest> EnqueueDownload,
    Action CloneInstance);

/// <summary>Builds the 修改 page using per-instance data and explicit operations.</summary>
internal sealed class VersionLoaderPage(VersionSettingsPageContext context, VersionTaskActions tasks, VersionLoaderActions operations)
{
    internal void Build()
    {
        var loaders = context.View.Card("安装 / 修改模组加载器");
        loaders.Children.Add(FeatureUiElements.Hint("为 Minecraft " + context.Requirements.Minecraft + " 安装指定加载器，生成新的启动版本。原实例与存档保留；现有模组不会自动迁移。"));
        var loader = new ComboBox
        {
            ItemsSource = new[]
            {
                        "fabric",
                        "quilt",
                        "forge",
                        "neoforge"
                    },
            SelectedIndex = 0
        };
        var versions = new ComboBox
        {
            DisplayMemberPath = "Label"
        };
        loaders.Children.Add(loader);
        loaders.Children.Add(versions);
        loaders.Children.Add(VersionSettingsView.Button("获取兼容加载器版本", async (_, _) =>
        {
            var selected = (string)loader.SelectedItem;
            await tasks.Run(async () =>
            {
                List<LoaderRelease> releases;
                if (selected is "forge" or "neoforge")
                    releases = await LoaderCatalog.Get(tasks.GetHttp(), context.Requirements.Minecraft, selected, tasks.GetToken());
                else
                {
                    var api = selected == "fabric" ? "https://meta.fabricmc.net/v2" : "https://meta.quiltmc.org/v3";
                    using var json = JsonDocument.Parse(await tasks.GetHttp().GetStringAsync(api + "/versions/loader/" + Uri.EscapeDataString(context.Requirements.Minecraft), tasks.GetToken()));
                    releases = json.RootElement.EnumerateArray().Select(item => item.GetProperty("loader")).Select(item => new LoaderRelease(item.GetProperty("version").GetString()!, item.GetProperty("version").GetString()!)).ToList();
                }

                versions.Tag = selected;
                versions.ItemsSource = releases;
                versions.SelectedIndex = releases.Count > 0 ? 0 : -1;
                if (releases.Count == 0)
                    InlineMessageBox.Show(context.View.Window, "没有此游戏版本可用的加载器版本。");
            }, "读取加载器版本");
        }));
        loaders.Children.Add(VersionSettingsView.Button("安装所选加载器", (_, _) =>
        {
            if (tasks.IsBusy())
                return;
            if (versions.SelectedItem is not LoaderRelease release || versions.Tag as string != loader.SelectedItem as string)
            {
                InlineMessageBox.Show(context.View.Window, "请先获取当前加载器的兼容版本。");
                return;
            }

            operations.EnqueueDownload("安装 " + loader.SelectedItem + " · " + context.Requirements.Minecraft, context.Requirements.Minecraft, new()
            {
                Version = context.Requirements.Minecraft,
                Loader = (string)loader.SelectedItem,
                LoaderVersion = release.Version,
                BackupInstanceDirectory = context.DataDirectory()
            });
            InlineMessageBox.Show(context.View.Window, "已加入下载队列，可在任务与恢复中查看。");
        }));
        var tools = context.View.Card("实例维护");
        tools.Children.Add(VersionSettingsView.Button("补全游戏文件", (_, _) => operations.EnqueueDownload("下载 / 修复 · " + context.Descriptor.DisplayName, context.Key, new() { Version = context.Descriptor.LaunchProfileId })));
        tools.Children.Add(VersionSettingsView.Button("复制为独立实例", (_, _) =>
        {
            context.View.Window.Close();
            operations.CloneInstance();
        }));

    }
}
