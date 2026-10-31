using MCUGLauncher.Models;
using MCUGLauncher.Services;

namespace MCUGLauncher.Ui;

/// <summary>
/// 组装界面状态快照：读取版本目录与本地模组，并把配置整理成前端使用的 camelCase 对象。
/// </summary>
static class LauncherStateBuilder
{
    public static object Build(AppConfig config)
    {
        var catalog = VersionCatalog.Load(config.Root, config.LastInstance);
        config.LastInstance = catalog.Selected;
        if (string.IsNullOrWhiteSpace(config.LastVersion) ||
            !catalog.Instances.Any(item => item.Name.Equals(config.LastVersion, StringComparison.OrdinalIgnoreCase)))
            config.LastVersion = catalog.Selected;

        var mods = VersionCatalog.ReadMods(config.Root, catalog.Selected);
        return new
        {
            type = "state",
            player = config.Player,
            accountType = config.AccountType,
            accountLabel = config.AccountType switch
            {
                "microsoft" => "正版账号 · 在线验证",
                "mua" => "MUA 账号 · 外置登录",
                _ => "离线账号 · 本地启动"
            },
            root = config.Root,
            java = config.Java,
            width = config.Width,
            height = config.Height,
            selectedVersion = config.LastVersion,
            selectedInstance = catalog.Selected,
            ram = config.Ram,
            anonymous = config.Anonymous,
            communityKey = !string.IsNullOrWhiteSpace(config.CommunityKey),
            adminKey = !string.IsNullOrWhiteSpace(config.AdminKey),
            skinStation = config.SkinStation,
            cape = config.Cape,
            versionGroups = catalog.Groups,
            instances = catalog.Instances,
            mods = mods.Select(mod => new
            {
                id = mod.Id,
                name = mod.Name,
                author = "本地模组",
                desc = mod.File,
                tags = new[] { "本地" },
                size = FormatSize(mod.Size),
                downloads = "",
                v = string.IsNullOrWhiteSpace(mod.Version) ? "—" : mod.Version,
                color = mod.Enabled ? "green" : "slate",
                ic = "box",
                file = mod.File,
                path = mod.Path,
                enabled = mod.Enabled,
                update = ""
            }),
            servers = config.Servers,
            dataSources = new
            {
                announcements = config.DataSources.Announcements,
                servers = config.DataSources.Servers,
                recommendations = config.DataSources.Recommendations,
                quotes = config.DataSources.Quotes
            },
            writeEndpoint = config.WriteEndpoint
        };
    }

    static string FormatSize(long bytes)
    {
        if (bytes <= 0)
            return "—";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / 1024.0 / 1024.0:0.#} MB";
    }
}
