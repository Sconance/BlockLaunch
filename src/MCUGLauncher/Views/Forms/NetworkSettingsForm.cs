using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Edits network values without accessing persistence or other feature controllers.</summary>
internal sealed class NetworkSettingsForm : StackPanel
{
    readonly ComboBox proxy;
    readonly TextBox address;
    readonly ComboBox source;

    internal NetworkSettingsForm(NetworkSettings settings)
    {

        Children.Add(FeatureUiElements.Heading("下载与网络"));
        Children.Add(FeatureUiElements.Hint("BMCLAPI 为游戏资源提供镜像；镜像与官方可自动切换，文件仍按原有校验信息验证。账号认证不切换到镜像。网络设置重启后生效。"));
        proxy = new ComboBox
        {
            ItemsSource = new[]
            {
                "系统代理",
                "直连",
                "自定义代理"
            },
            SelectedItem = settings.ProxyMode
        };
        address = new TextBox
        {
            Text = settings.ProxyAddress,
            ToolTip = "http://127.0.0.1:7890，不含账号密码"
        };
        source = new ComboBox
        {
            ItemsSource = new[]
            {
                "仅官方",
                "官方优先，失败切换 BMCLAPI",
                "BMCLAPI 优先"
            },
            SelectedItem = settings.DownloadSource
        };
        Children.Add(FeatureUiElements.Hint("代理方式"));
        Children.Add(proxy);
        Children.Add(address);
        Children.Add(FeatureUiElements.Hint("游戏下载源 · BMCLAPI / Mojang"));
        Children.Add(source);

    }

    internal NetworkSettings Read(int limit) => new()
    {
        ProxyMode = (string)proxy.SelectedItem,
        ProxyAddress = address.Text.Trim(),
        DownloadSource = (string)source.SelectedItem,
        LimitKibPerSecond = limit
    };
}
