using System.IO;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed record ServerSettingsActions(Func<Preferences> Preferences, Action Save,
    Func<HashSet<string>> Profiles, Func<bool> IsBusy, Func<ServerFavorite, Task> Connect);

/// <summary>Server favorites form. Connection and persistence belong to injected operations.</summary>
internal sealed class ServerSettingsPage(ServerSettingsActions operations)
{
    internal SettingsPageContent Build(string? selectedProfile, Action close)
    {
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("服务器收藏"));
        body.Children.Add(FeatureUiElements.Hint("收藏保存在启动器配置中。点击查询才连接服务器，显示 MOTD、版本、人数和 Ping；暂不解析 DNS SRV，必要时填写实际端口。"));
        var list = new ListBox
        {
            MinHeight = 70,
            MaxHeight = 220,
            DisplayMemberPath = "Name"
        };
        body.Children.Add(list);
        var name = new TextBox
        {
            ToolTip = "服务器名称"
        };
        var address = new TextBox
        {
            ToolTip = "域名:端口 或 [IPv6]:端口"
        };
        var profiles = new ComboBox
        {
            ItemsSource = operations.Profiles().Order().ToList(),
            SelectedItem = selectedProfile
        };
        body.Children.Add(FeatureUiElements.Hint("名称 / 地址 / 启动实例"));
        body.Children.Add(name);
        body.Children.Add(address);
        body.Children.Add(profiles);
        var status = FeatureUiElements.Hint("选择或添加服务器。");
        body.Children.Add(status);
        void Reload()
        {
            var id = (list.SelectedItem as ServerFavorite)?.Id;
            list.ItemsSource = operations.Preferences().Servers.ToList();
            list.SelectedItem = operations.Preferences().Servers.FirstOrDefault(s => s.Id == id);
        }

        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is ServerFavorite item)
            {
                name.Text = item.Name;
                address.Text = item.Address;
                profiles.SelectedItem = item.Profile;
                status.Text = "尚未查询服务器状态。";
            }
        };
        Reload();
        var cts = new CancellationTokenSource();
        body.Children.Add(FeatureUiElements.ActionButton("新增 / 保存", (_, _) =>
        {
            try
            {
                var endpoint = ServerEndpoint.Parse(address.Text);
                if (string.IsNullOrWhiteSpace(name.Text) || name.Text.Length > 100)
                    throw new IOException("名称应为 1–100 个字符。");
                if (profiles.SelectedItem is not string profile)
                    throw new IOException("请选择已安装实例。");
                var old = list.SelectedItem as ServerFavorite;
                var item = new ServerFavorite(old?.Id ?? Guid.NewGuid().ToString("N"), name.Text.Trim(), endpoint.ToString(), profile);
                if (old != null)
                    operations.Preferences().Servers.Remove(old);
                operations.Preferences().Servers.Add(item);
                operations.Save();
                Reload();
                list.SelectedItem = item;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }));
        body.Children.Add(FeatureUiElements.ActionButton("清空选择，添加新收藏", (_, _) =>
        {
            list.SelectedItem = null;
            name.Clear();
            address.Clear();
        }));
        body.Children.Add(FeatureUiElements.ActionButton("查询在线状态", async (_, _) =>
        {
            var captured = address.Text;
            status.Text = "正在查询…";
            try
            {
                var result = await ServerFavorites.Ping(captured, cts.Token);
                if (address.Text == captured && !cts.IsCancellationRequested)
                    status.Text = $"{result.Description}\n{result.Version} · {result.Online}/{result.Maximum} 人 · {result.LatencyMs} ms";
            }
            catch (OperationCanceledException)
            {
                if (!cts.IsCancellationRequested)
                    status.Text = "查询超时；不代表服务器一定离线。";
            }
            catch (Exception ex)
            {
                status.Text = "无法查询：" + ex.Message;
            }
        }));
        body.Children.Add(FeatureUiElements.ActionButton("启动并连接所选服务器", async (_, _) =>
        {
            if (operations.IsBusy() || list.SelectedItem is not ServerFavorite item)
                return;
            if (!operations.Profiles().Contains(item.Profile))
            {
                status.Text = "对应实例已不存在，请重新选择并保存。";
                return;
            }

            close();
            await operations.Connect(item);
        }));
        body.Children.Add(FeatureUiElements.ActionButton("移除收藏", (_, _) =>
        {
            if (list.SelectedItem is ServerFavorite item)
            {
                operations.Preferences().Servers.Remove(item);
                operations.Save();
                Reload();
            }
        }));
        return new(new SettingsToolView(body), () => { cts.Cancel(); cts.Dispose(); });
    }
}
