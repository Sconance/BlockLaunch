using MCUGLauncher.Services;
using System.Text.Json;
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

        InstancePath(context.Config.Root, version);
        var instanceDir = VersionCatalog.InstanceDirectory(context.Config.Root, version);
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
            var settings = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(context.Config.LegacyPreferences.Launch))!;
            settings.AutoMemory = false;
            settings.RamMb = context.Config.Ram;
            settings.Java = context.Config.Java;
            settings.Width = context.Config.Width;
            settings.Height = context.Config.Height;
            settings.VersionInfo = "MCUGLauncher";
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
            var game = context.Games.Start(context.Config.Root, instanceDir, version,
                Path.Combine(context.Store.DataDirectory, "game-logs"), process, context.Log, new[] { session.AccessToken ?? "" },
                context.Config.Player, context.Config.AccountType == "offline");
            context.SendGames();
            _ = ObserveAsync(game);
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
    async Task ObserveAsync(RunningGame game)
    {
        try
        {
            await game.Session.Completion;
        }
        catch (Exception ex) { context.Log(ex.ToString()); }
        finally { context.Games.Release(game.Id); context.SendGames(); }
    }

    public void Stop(string id)
    {
        var game = context.Games.Active.FirstOrDefault(g => g.Id.ToString("N") == id)
            ?? throw new InvalidOperationException("该游戏已经退出。");
        if (!game.Session.RequestStop())
            throw new IOException("关闭请求未成功，请稍后重试。");
    }

}
