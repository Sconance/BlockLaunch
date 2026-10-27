using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class LoaderInstallationController(MainWindow host)
{
    public async void AutomaticLoaderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button
            {
                Tag: string loader
            })
            return;
        await host.Run(async () =>
        {
            host.RequireStopped();
            var gameVersion = host.downloadVersion ?? host.Target().GameVersion;
            var releases = await LoaderCatalog.Get(host.http, gameVersion, loader, host.CurrentToken);
            if (releases.Count == 0)
                throw new IOException("此游戏版本没有可用的自动安装版本。NeoForge 从 1.20.2 起支持；旧版本可使用官方安装器。");
            var body = new StackPanel();
            body.Children.Add(MainWindow.Heading(loader + " · Minecraft " + gameVersion));
            body.Children.Add(MainWindow.Hint("请选择加载器版本。测试版可能与模组不兼容；安装过程中请保持联网。"));
            var list = new ListBox
            {
                ItemsSource = releases,
                Height = 400,
                SelectedIndex = 0
            };
            body.Children.Add(list);
            var dialog = host.FeatureDialog("选择加载器版本", body);
            LoaderRelease? chosen = null;
            body.Children.Add(MainWindow.ActionButton("安装所选版本", (_, _) =>
            {
                if (list.SelectedItem is LoaderRelease release)
                {
                    chosen = release;
                    dialog.Close();
                }
            }));
            dialog.ShowDialog();
            if (chosen == null)
                return;
            host.EnqueueDownload(loader + " · " + gameVersion, gameVersion, new()
            {
                Version = gameVersion,
                Loader = loader,
                LoaderVersion = chosen.Version
            });
        }, "安装 " + loader);
    }
}
