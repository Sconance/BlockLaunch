using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class LoaderSelectionController
{
    readonly LauncherWorkspace workspace;
    internal LoaderSelectionController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal async void AutomaticLoaderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string loader })
            return;
        await workspace.LaunchCoordinator.Run(async () =>
        {
            workspace.ResourceBrowserController.RequireStopped();
            var gameVersion = workspace.LaunchCoordinator.downloadVersion ?? workspace.ResourceBrowserController.Target().GameVersion;
            var releases = await LoaderCatalog.Get(workspace.LaunchCoordinator.http, gameVersion, loader, workspace.LaunchCoordinator.CurrentToken);
            if (releases.Count == 0)
                throw new System.IO.IOException("此游戏版本没有可用的自动安装版本。NeoForge 从 1.20.2 起支持；旧版本可使用官方安装器。");
            var body = new StackPanel();
            body.Children.Add(FeatureUiElements.Heading(loader + " · Minecraft " + gameVersion));
            body.Children.Add(FeatureUiElements.Hint("请选择加载器版本。测试版可能与模组不兼容；安装过程中请保持联网。"));
            var list = new ListBox
            {
                ItemsSource = releases,
                Height = 400,
                SelectedIndex = 0
            };
            body.Children.Add(list);
            var dialog = workspace.InstanceWorkspaceController.FeatureDialog("选择加载器版本", body);
            LoaderRelease? chosen = null;
            body.Children.Add(FeatureUiElements.ActionButton("安装所选版本", (_, _) =>
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
            workspace.DownloadQueueController.EnqueueDownload(loader + " · " + gameVersion, gameVersion, new()
            {
                Version = gameVersion,
                Loader = loader,
                LoaderVersion = chosen.Version
            });
        }, "安装 " + loader);
    }
}
