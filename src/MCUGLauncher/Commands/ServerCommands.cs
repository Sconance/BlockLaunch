using System.Diagnostics;
using System.Text.Json;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;

namespace MCUGLauncher.Commands;

/// <summary>
/// 服务器与目录命令。
/// </summary>
/// <remarks>
/// 依赖 <see cref="ConfigStore"/> 与 <see cref="AppConfig"/> 保存收藏；<see cref="VersionCatalog"/> 解析当前实例路径；<see cref="ServerPingService"/> 查询 SLP 状态；<see cref="UiBridge"/> 回推结果；构造时注入 <c>SendState</c> 回调刷新界面。
/// </remarks>
sealed class ServerCommands(ConfigStore store, AppConfig config, UiBridge bridge, Action stateChanged)
{
    readonly ConfigStore _store = store;
    readonly AppConfig _config = config;
    readonly UiBridge _bridge = bridge;
    readonly Action _stateChanged = stateChanged;

    void SendState() => _stateChanged();

    /// <summary>
    /// 用资源管理器打开实例子目录。
    /// </summary>
    /// <param name="kind">mods / saves / screenshots / version，其他值打开游戏根目录</param>
    /// <remarks>
    /// 参数 <paramref name="kind"/> 取 mods/saves/screenshots/version，其他值回退到游戏根目录；目录不存在时自动创建。
    /// </remarks>
    public void OpenFolder(string kind)
    {
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        var instance = VersionCatalog.InstanceDirectory(_config.Root, catalog.Selected);
        var path = kind switch
        {
            "mods" => Path.Combine(instance, "mods"),
            "saves" => Path.Combine(instance, "saves"),
            "screenshots" => Path.Combine(instance, "screenshots"),
            "version" => instance,
            _ => _config.Root
        };
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    /// <summary>
    /// 查询服务器 SLP 状态。
    /// </summary>
    /// <param name="address">服务器域名或 IP，可带端口</param>
    /// <param name="id">前端请求标识，原样回传</param>
    /// <remarks>
    /// 地址为 host 或 host:port；id 原样回传；超时与连接失败写入 result.Error。
    /// </remarks>
    public async Task PingAsync(string address, string id)
    {
        var result = await ServerPingService.PingAsync(address, CancellationToken.None);
        _bridge.Send(new
        {
            type = "serverPing",
            id,
            online = result.Online,
            players = result.OnlinePlayers,
            max = result.MaxPlayers,
            ping = result.Ping,
            motd = result.Motd,
            version = result.Version,
            error = result.Error
        });
    }

    /// <summary>
    /// 新增或更新一条收藏服务器。
    /// </summary>
    /// <param name="root">包含 name、address、oldAddress 的消息</param>
    /// <remarks>
    /// address 必填；oldAddress 与 address 不同则先移除旧记录；保存后回推 serverResult 并刷新状态。
    /// </remarks>
    public void SaveServer(JsonElement root)
    {
        var name = UiBridge.ReadString(root, "name");
        var address = UiBridge.ReadString(root, "address").Trim();
        var oldAddress = UiBridge.ReadString(root, "oldAddress").Trim();
        if (address.Length == 0)
        {
            _bridge.Send(new
            {
                type = "serverResult",
                ok = false,
                message = "请填写服务器地址。"
            });
            return;
        }
        if (name.Length == 0)
            name = address.Split(':')[0];
        if (!string.IsNullOrWhiteSpace(oldAddress) &&
            !oldAddress.Equals(address, StringComparison.OrdinalIgnoreCase))
            _config.Servers.RemoveAll(s => s.Address.Equals(oldAddress, StringComparison.OrdinalIgnoreCase));
        _config.Servers.RemoveAll(s => s.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
        _config.Servers.Add(new SavedServer { Name = name, Address = address });
        _store.Save(_config);
        _bridge.Send(new
        {
            type = "serverResult",
            ok = true,
            message = (oldAddress.Length > 0 ? "已更新 " : "已添加 ") + name
        });
        SendState();
    }

    /// <summary>
    /// 按地址删除收藏服务器。
    /// </summary>
    /// <param name="address">要删除的服务器地址，忽略大小写</param>
    /// <remarks>
    /// 地址匹配忽略大小写；不存在的地址静默忽略。
    /// </remarks>
    public void DeleteServer(string address)
    {
        _config.Servers.RemoveAll(s => s.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
        _store.Save(_config);
        SendState();
    }
}
