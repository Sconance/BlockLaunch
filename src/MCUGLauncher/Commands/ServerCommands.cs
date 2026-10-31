using System.Diagnostics;
using System.Text.Json;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;

namespace MCUGLauncher.Commands;

/// <summary>
/// 服务器与目录命令：打开实例目录、SLP 状态查询、收藏服务器增删。
/// </summary>
sealed class ServerCommands(ConfigStore store, AppConfig config, UiBridge bridge, Action stateChanged)
{
    readonly ConfigStore _store = store;
    readonly AppConfig _config = config;
    readonly UiBridge _bridge = bridge;
    readonly Action _stateChanged = stateChanged;

    void SendState() => _stateChanged();

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

    public void DeleteServer(string address)
    {
        _config.Servers.RemoveAll(s => s.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
        _store.Save(_config);
        SendState();
    }
}
