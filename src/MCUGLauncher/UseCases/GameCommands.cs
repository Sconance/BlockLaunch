using BlockLaunch;
using CmlLib.Core.Auth;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

// Handles game use cases; no WPF controls or browser access.
internal sealed class GameCommands(LauncherContext context)
{
    bool preparing;

    public async Task LaunchAsync(string version)
    {
        if (preparing)
        {
            context.Send(new
            {
                type = "launchResult",
                ok = false,
                message = "游戏正在准备启动，请稍候。"
            });
            return;
        }
        preparing = true;
        try
        {
            await LaunchCoreAsync(version);
        }
        finally { preparing = false; }
    }

    async Task LaunchCoreAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            version = context.Config.LastVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            context.Send(new
            {
                type = "launchResult",
                ok = false,
                message = "还没有可启动的版本。"
            });
            return;
        }

        var instanceDir = InstancePath(context.Config.Root, version);
        if (!Directory.Exists(instanceDir))
        {
            context.Send(new
            {
                type = "launchResult",
                ok = false,
                message = "版本目录不存在：" + version
            });
            return;
        }

        try
        {
            if (context.Config.AccountType != "offline" && context.OnlineSession == null)
                throw new InvalidOperationException("在线账号尚未验证完成，请完成登录后再启动。");
            var session = (context.Config.AccountType == "offline" ? null : context.OnlineSession) ?? new MSession
            {
                Username = context.Config.Player,
                UUID = OfflineUuid(context.Config.Player),
                AccessToken = "0",
                ClientToken = Guid.NewGuid().ToString("N"),
                UserType = "legacy"
            };
            BlockLaunch.AuthlibLaunch? external = null;
            if (context.Config.AccountType == "mua" && context.OnlineSession != null && context.Mua != null)
            {
                var metadata = await context.Mua.Metadata(context.Token);
                var jar = await BlockLaunch.AuthlibLaunch.Prepare(
                    Path.Combine(context.Store.DataDirectory, "authlib-injector"), context.Http, context.Token);
                external = new BlockLaunch.AuthlibLaunch(jar, metadata.Json);
            }
            var defaults = new BlockLaunch.LaunchSettings();
            var settings = new BlockLaunch.LaunchSettings
            {
                AutoMemory = false,
                RamMb = context.Config.Ram,
                Java = context.Config.Java,
                Width = context.Config.Width,
                Height = context.Config.Height,
                Isolation = defaults.Isolation,
                Visibility = defaults.Visibility,
                Priority = defaults.Priority,
                VersionInfo = "MCUGLauncher"
            };
            var process = await BlockLaunch.LaunchService.Build(
                new BlockLaunch.LaunchRequest(
                    context.Config.Root,
                    instanceDir,
                    version,
                    context.Config.Java,
                    context.Config.Ram,
                    context.Config.Width,
                    context.Config.Height,
                    session,
                    settings,
                    external),
                context.Token);
            process.Start();
            context.Config.LastVersion = version;
            context.Config.LastInstance = version;
            context.Store.Save(context.Config);
            context.Send(new
            {
                type = "launchResult",
                ok = true,
                message = "已启动 " + version
            });
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "launchResult",
                ok = false,
                message = ex.Message
            });
        }
    }
}
