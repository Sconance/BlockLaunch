using MCUGLauncher.Models;
using MCUGLauncher.Services;

namespace MCUGLauncher.Ui;

/// <summary>
/// 界面状态快照构建器。
/// </summary>
/// <remarks>
/// 依赖 <see cref="AppConfig"/> 与 <see cref="VersionCatalog"/>；会顺带修正 LastInstance/LastVersion，因此不是纯函数，调用后应保存配置。
/// </remarks>
static class LauncherStateBuilder
{
    /// <summary>
    /// 构建 state 消息体。
    /// </summary>
    /// <param name="config">当前配置；会被修正 LastInstance/LastVersion</param>
    /// <returns>可直接发送给前端的 state 对象</returns>
    /// <remarks>
    /// 扫描版本目录、读取本地模组并投影为前端字段；返回匿名对象，由 UiBridge 序列化。
    /// </remarks>
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
