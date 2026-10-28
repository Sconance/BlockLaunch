using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class NetworkController
{
    readonly LauncherWorkspace workspace;
    internal NetworkController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal void NetworkClick(object sender, RoutedEventArgs e)
    {
        var body = new StackPanel();
        body.Children.Add(InstancePagesController.Heading("下载与网络"));
        body.Children.Add(InstancePagesController.Hint("BMCLAPI 为游戏资源提供镜像；镜像与官方可自动切换，文件仍按原有校验信息验证。账号认证不切换到镜像。网络设置重启后生效。"));
        var proxy = new ComboBox
        {
            ItemsSource = new[]
            {
                "系统代理",
                "直连",
                "自定义代理"
            },
            SelectedItem = workspace.LaunchCoordinator.prefs.Network.ProxyMode
        };
        var address = new TextBox
        {
            Text = workspace.LaunchCoordinator.prefs.Network.ProxyAddress,
            ToolTip = "http://127.0.0.1:7890，不含账号密码"
        };
        var source = new ComboBox
        {
            ItemsSource = new[]
            {
                "仅官方",
                "官方优先，失败切换 BMCLAPI",
                "BMCLAPI 优先"
            },
            SelectedItem = workspace.LaunchCoordinator.prefs.Network.DownloadSource
        };
        body.Children.Add(InstancePagesController.Hint("代理方式"));
        body.Children.Add(proxy);
        body.Children.Add(address);
        body.Children.Add(InstancePagesController.Hint("游戏下载源 · BMCLAPI / Mojang"));
        body.Children.Add(source);
        var dialog = workspace.InstancePagesController.FeatureDialog("下载与网络设置", body);
        body.Children.Add(InstancePagesController.ActionButton("保存，下次启动生效", (_, _) =>
        {
            try
            {
                var value = new NetworkSettings
                {
                    ProxyMode = (string)proxy.SelectedItem,
                    ProxyAddress = address.Text.Trim(),
                    DownloadSource = (string)source.SelectedItem,
                    LimitKibPerSecond = workspace.LaunchCoordinator.prefs.Network.LimitKibPerSecond
                };
                value.Validate();
                workspace.LaunchCoordinator.prefs.Network = value;
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                dialog.DialogResult = true;
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(dialog, ex.Message);
            }
        }));
        dialog.ShowDialog();
    }
}
