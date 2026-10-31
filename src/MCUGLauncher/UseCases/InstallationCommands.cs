using System.Text.Json;
using System.Security.Cryptography;
using BlockLaunch;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

internal sealed class InstallationCommands(LauncherContext context)
{
    public async Task LoadersAsync(JsonElement root)
    {
        var game = ReadString(root, "version");
        InstancePath(context.Config.Root, game);
        var loader = ReadString(root, "loader");
        List<string> versions;
        if (loader is "forge" or "neoforge")
            versions = (await LoaderCatalog.Get(context.Http, game, loader, context.Token)).Select(v => v.Version).ToList();
        else if (loader is "fabric" or "quilt")
        {
            var endpoint = loader == "fabric" ? "https://meta.fabricmc.net/v2" : "https://meta.quiltmc.org/v3";
            using var doc = JsonDocument.Parse(await context.Http.GetStringAsync(endpoint + "/versions/loader/" + Uri.EscapeDataString(game), context.Token));
            versions = doc.RootElement.EnumerateArray().Select(v => v.GetProperty("loader").GetProperty("version").GetString()!).Distinct().ToList();
        }
        else
            throw new ArgumentException("不支持的加载器。");
        context.Send(new
        {
            type = "loaderList",
            version = game,
            loader,
            versions
        });
    }

    public void InstallLoader(JsonElement root)
    {
        var game = ReadString(root, "version");
        var loader = ReadString(root, "loader");
        var release = ReadString(root, "loaderVersion");
        InstancePath(context.Config.Root, game);
        if (loader is not ("fabric" or "quilt" or "forge" or "neoforge") || string.IsNullOrWhiteSpace(release))
            throw new ArgumentException("请先选择加载器版本。");
        context.Tasks.Enqueue("安装 " + game + " / " + loader + " " + release, context.Config.Root, game,
            new DownloadRequest { Kind = "game", Version = game, Loader = loader, LoaderVersion = release, Java = context.Config.Java });
    }

    public async Task ImportPackAsync(JsonElement root)
    {
        var file = Path.GetFullPath(ReadString(root, "path"));
        var overview = await Task.Run(() => PackService.ReadOverview(file), context.Token);
        await using var input = File.OpenRead(file);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, context.Token));
        context.Tasks.Enqueue("导入整合包 " + overview.Name, context.Config.Root, overview.Name,
            new DownloadRequest { Kind = "pack", File = file, SourceHash = hash, Java = context.Config.Java });
    }
}
