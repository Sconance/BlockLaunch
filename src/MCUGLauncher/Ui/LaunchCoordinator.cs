using System.Security.Cryptography;
using System.Text;
using CmlLib.Core.Auth;
using MCUGLauncher.Authentication;
using MCUGLauncher.Models;
using MCUGLauncher.Services;

namespace MCUGLauncher.Ui;

/// <summary>
/// 启动流程：校验版本目录、准备会话（离线 / MUA 外置登录）、构建启动参数并启动进程。
/// </summary>
sealed class LaunchCoordinator(
    ConfigStore store,
    AppConfig config,
    UiBridge bridge,
    AccountSession accounts,
    HttpClient http)
{
    /// <summary>
    /// 执行 启动。
    /// </summary>
    public async Task LaunchAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            version = config.LastVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            Fail("还没有可启动的版本。");
            return;
        }

        var instanceDir = Path.Combine(config.Root, "versions", version);
        if (!Directory.Exists(instanceDir))
        {
            Fail("版本目录不存在：" + version);
            return;
        }

        try
        {
            var session = accounts.Session ?? new MSession
            {
                Username = config.Player,
                UUID = OfflineUuid(config.Player),
                AccessToken = "0",
                ClientToken = Guid.NewGuid().ToString("N"),
                UserType = "legacy"
            };
            BlockLaunch.AuthlibLaunch? external = null;
            if (config.AccountType == "mua" && accounts.Session != null && accounts.Mua != null)
            {
                var metadata = await accounts.Mua.Metadata(CancellationToken.None);
                var jar = await BlockLaunch.AuthlibLaunch.Prepare(
                    Path.Combine(store.DataDirectory, "authlib-injector"), http, CancellationToken.None);
                external = new BlockLaunch.AuthlibLaunch(jar, metadata.Json);
            }
            var defaults = new BlockLaunch.LaunchSettings();
            var settings = new BlockLaunch.LaunchSettings
            {
                AutoMemory = false,
                RamMb = config.Ram,
                Java = config.Java,
                Width = config.Width,
                Height = config.Height,
                Isolation = defaults.Isolation,
                Visibility = defaults.Visibility,
                Priority = defaults.Priority,
                VersionInfo = "MCUGLauncher"
            };
            var process = await BlockLaunch.LaunchService.Build(
                new BlockLaunch.LaunchRequest(
                    config.Root,
                    instanceDir,
                    version,
                    config.Java,
                    config.Ram,
                    config.Width,
                    config.Height,
                    session,
                    settings,
                    external),
                CancellationToken.None);
            process.Start();
            config.LastVersion = version;
            config.LastInstance = version;
            store.Save(config);
            bridge.Send(new
            {
                type = "launchResult",
                ok = true,
                message = "已启动 " + version
            });
        }
        catch (Exception ex)
        {
            Fail(ex.Message);
        }
    }

    void Fail(string message) => bridge.Send(new
    {
        type = "launchResult",
        ok = false,
        message
    });

    static string OfflineUuid(string player)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + player));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash).ToString("N");
    }
}
