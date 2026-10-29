using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class NetworkSettingsController(MainWindow host)
{
    public void Open()
    {
        var body = new StackPanel();
        body.Children.Add(MainWindow.Heading("下载与网络"));
        body.Children.Add(MainWindow.Hint("BMCLAPI 为游戏资源提供镜像；镜像与官方可自动切换，文件仍按原有校验信息验证。账号认证不切换到镜像。网络设置重启后生效。"));
        var proxy = new ComboBox
        {
            ItemsSource = new[] {
                "系统代理",
                "直连",
                "自定义代理"
            },
            SelectedItem = host.prefs.Network.ProxyMode
        };
        var address = new TextBox
        {
            Text = host.prefs.Network.ProxyAddress,
            ToolTip = "http://127.0.0.1:7890，不含账号密码"
        };
        var source = new ComboBox
        {
            ItemsSource = new[] {
                "仅官方",
                "官方优先，失败切换 BMCLAPI",
                "BMCLAPI 优先"
            },
            SelectedItem = host.prefs.Network.DownloadSource
        };
        body.Children.Add(MainWindow.Hint("代理方式"));
        body.Children.Add(proxy);
        body.Children.Add(address);
        body.Children.Add(MainWindow.Hint("游戏下载源 · BMCLAPI / Mojang"));
        body.Children.Add(source);
        var dialog = host.FeatureDialog("下载与网络设置", body);
        body.Children.Add(MainWindow.ActionButton("保存，下次启动生效", (_, _) =>
        {
            try
            {
                var value = new NetworkSettings
                {
                    ProxyMode = (string)proxy.SelectedItem,
                    ProxyAddress = address.Text.Trim(),
                    DownloadSource = (string)source.SelectedItem,
                    LimitKibPerSecond = host.prefs.Network.LimitKibPerSecond
                };
                value.Validate();
                host.launcherSettings.SaveNetwork(host.prefs, value);
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
