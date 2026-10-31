using BlockLaunch;
using System.Text.Json;
using System.IO.Compression;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

// Handles settings use cases; no WPF controls or browser access.
internal sealed class SettingsCommands(LauncherContext context)
{
    public void SaveSettings(JsonElement root)
    {
        if (root.TryGetProperty("player", out var player))
            context.Config.Player = player.GetString() ?? context.Config.Player;
        if (root.TryGetProperty("anonymous", out var anonymous))
            context.Config.Anonymous = anonymous.GetBoolean();
        if (root.TryGetProperty("writeEndpoint", out var endpoint))
            context.Config.WriteEndpoint = endpoint.GetString() ?? "";
        if (root.TryGetProperty("dataSources", out var sources))
        {
            context.Config.DataSources.Announcements = ReadString(sources, "announcements");
            context.Config.DataSources.Servers = ReadString(sources, "servers");
            context.Config.DataSources.Recommendations = ReadString(sources, "recommendations");
            context.Config.DataSources.Quotes = ReadString(sources, "quotes");
        }
        if (root.TryGetProperty("ram", out var ram) && ram.TryGetInt32(out var ramValue))
            context.Config.Ram = Math.Clamp(ramValue, 512, 65536);
        if (root.TryGetProperty("root", out var gameRoot))
        {
            var value = gameRoot.GetString()?.Trim() ?? "";
            if (value.Length > 0)
                context.Config.Root = value;
        }
        if (root.TryGetProperty("java", out var java))
            context.Config.Java = java.GetString()?.Trim() ?? "";
        if (root.TryGetProperty("width", out var width) && width.TryGetInt32(out var widthValue))
            context.Config.Width = Math.Clamp(widthValue, 640, 7680);
        if (root.TryGetProperty("height", out var height) && height.TryGetInt32(out var heightValue))
            context.Config.Height = Math.Clamp(heightValue, 480, 4320);
        context.Store.Save(context.Config);
        context.Send(new
        {
            type = "settingsSaved"
        });
        context.RefreshState();
    }

    public async Task ScanJavaAsync()
    {
        var list = await JavaDiscovery.Scan(context.Config.Root, new[] { context.Config.Java }, context.Token);
        context.Send(new
        {
            type = "javaList",
            items = list.Select(item => new
            {
                path = item.Path,
                major = item.Major,
                arch = item.Architecture,
                vendor = item.Vendor,
                error = item.Error
            })
        });
    }

}
