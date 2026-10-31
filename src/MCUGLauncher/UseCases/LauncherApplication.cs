using BlockLaunch;
using System.Text.Json;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

internal sealed class LauncherApplication : IDisposable
{
    readonly LauncherContext context;
    Task? startup;
    readonly IDesktopHost desktop;
    readonly AccountCommands accountCommands;
    readonly GameCommands gameCommands;
    readonly InstanceCommands instanceCommands;
    readonly ResourceCommands resourceCommands;
    readonly SettingsCommands settingsCommands;
    readonly DiagnosticCommands diagnosticCommands;
    readonly CommunityCommands communityCommands;

    public AppConfig Config => context.Config;
    public LauncherApplication(Action<object> send, IDesktopHost desktop, ConfigStore? store = null)
    {
        this.desktop = desktop;
        context = new LauncherContext(send, store);
        context.RefreshState = SendState;
        accountCommands = new AccountCommands(context);
        gameCommands = new GameCommands(context);
        instanceCommands = new InstanceCommands(context);
        resourceCommands = new ResourceCommands(context);
        settingsCommands = new SettingsCommands(context);
        diagnosticCommands = new DiagnosticCommands(context);
        communityCommands = new CommunityCommands(context);
    }

    public async Task HandleAsync(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("消息必须是对象。");
            ProtocolValues.ValidateMessage(root);
            var type = ReadString(root, "type");
            switch (type)
            {
                case "ready":
                    // The browser can request fresh state after every navigation.
                    context.RefreshState();
                    await (startup ??= Task.WhenAll(communityCommands.SendCommunityAsync(), accountCommands.RestoreMicrosoftAsync()));
                    break;
                case "requestState":
                    context.RefreshState();
                    break;
                case "launch":
                    await gameCommands.LaunchAsync(ReadString(root, "version"));
                    break;
                case "window":
                    desktop.Window(ReadString(root, "action"));
                    break;
                case "drag":
                    desktop.Drag();
                    break;
                case "selectVersion":
                    context.Config.LastVersion = ReadString(root, "version");
                    context.Config.LastInstance = context.Config.LastVersion;
                    context.Store.Save(context.Config);
                    context.RefreshState();
                    break;
                case "selectInstance":
                    context.Config.LastInstance = ReadString(root, "instance");
                    context.Store.Save(context.Config);
                    context.RefreshState();
                    break;
                case "openFolder":
                    instanceCommands.OpenFolder(ReadString(root, "kind"));
                    break;
                case "pingServer":
                    await communityCommands.PingAsync(ReadString(root, "address"), ReadString(root, "id"));
                    break;
                case "saveServer":
                    communityCommands.SaveServer(root);
                    break;
                case "deleteServer":
                    communityCommands.DeleteServer(ReadString(root, "address"));
                    break;
                case "toggleMod":
                    instanceCommands.ToggleMod(ReadString(root, "file"), root.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean());
                    break;
                case "deleteMod":
                    instanceCommands.DeleteMod(ReadString(root, "file"));
                    break;
                case "deleteVersion":
                    instanceCommands.DeleteVersion(ReadString(root, "version"));
                    break;
                case "searchContent":
                    context.Log("searchContent " + ReadString(root, "category") + " " + ReadString(root, "query"));
                    await resourceCommands.SearchContentAsync(root);
                    break;
                case "installContent":
                    await resourceCommands.InstallContentAsync(root);
                    break;
                case "vanillaVersions":
                    await resourceCommands.SendVanillaVersionsAsync();
                    break;
                case "installVanilla":
                    await resourceCommands.InstallVanillaAsync(ReadString(root, "version"));
                    break;
                case "installMod":
                    await resourceCommands.InstallModAsync(root);
                    break;
                case "saveSettings":
                    settingsCommands.SaveSettings(root);
                    break;
                case "pickFolder":
                    desktop.PickFolder(ReadString(root, "target"));
                    break;
                case "pickFile":
                    desktop.PickFile(ReadString(root, "target"));
                    break;
                case "scanJava":
                    await settingsCommands.ScanJavaAsync();
                    break;
                case "backupSaves":
                    settingsCommands.BackupSaves();
                    break;
                case "readLog":
                    diagnosticCommands.ReadLog();
                    break;
                case "systemInfo":
                    diagnosticCommands.SendSystemInfo();
                    break;
                case "resetWindow":
                    desktop.ResetWindow();
                    break;
                case "loginMicrosoft":
                    await accountCommands.LoginMicrosoftAsync();
                    break;
                case "loginMua":
                    await accountCommands.LoginMuaAsync(ReadString(root, "account"), ReadString(root, "password"));
                    break;
                case "logoutAccount":
                    context.OnlineSession = null;
                    context.Config.AccountType = "offline";
                    context.Store.Save(context.Config);
                    context.RefreshState();
                    break;
                case "testSource":
                    await communityCommands.TestSourceAsync(ReadString(root, "name"), ReadString(root, "url"));
                    break;
                case "verifyKey":
                    communityCommands.VerifyKey(ReadString(root, "role"), ReadString(root, "key"));
                    break;
                case "publish":
                    await communityCommands.PublishAsync(root);
                    break;
                case "saveAnnouncements":
                    communityCommands.SaveCommunityLocal("announcements.local.json", root, "items");
                    break;
                case "saveRecommendations":
                    communityCommands.SaveCommunityLocal("recommendations.local.json", root, "items");
                    break;
                default:
                    throw new ArgumentException("不支持的消息类型：" + type);
            }
        }
        catch (Exception ex)
        {
            context.Log(ex.ToString());
            context.Send(new
            {
                type = "error",
                message = ex.Message
            });
        }
    }

    public void SendState()
    {
        var catalog = VersionCatalog.Load(context.Config.Root, context.Config.LastInstance);
        context.Config.LastInstance = catalog.Selected;
        if (string.IsNullOrWhiteSpace(context.Config.LastVersion) ||
            !catalog.Instances.Any(item => item.Name.Equals(context.Config.LastVersion, StringComparison.OrdinalIgnoreCase)))
            context.Config.LastVersion = catalog.Selected;

        var mods = VersionCatalog.ReadMods(context.Config.Root, catalog.Selected);
        var state = new
        {
            type = "state",
            player = context.Config.Player,
            accountType = context.Config.AccountType,
            accountLabel = context.Config.AccountType switch
            {
                "microsoft" => "正版账号 · 在线验证",
                "mua" => "MUA 账号 · 外置登录",
                _ => "离线账号 · 本地启动"
            },
            root = context.Config.Root,
            java = context.Config.Java,
            width = context.Config.Width,
            height = context.Config.Height,
            selectedVersion = context.Config.LastVersion,
            selectedInstance = catalog.Selected,
            ram = context.Config.Ram,
            anonymous = context.Config.Anonymous,
            communityKey = !string.IsNullOrWhiteSpace(context.Config.CommunityKey),
            adminKey = !string.IsNullOrWhiteSpace(context.Config.AdminKey),
            skinStation = context.Config.SkinStation,
            cape = context.Config.Cape,
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
            servers = context.Config.Servers,
            dataSources = new
            {
                announcements = context.Config.DataSources.Announcements,
                servers = context.Config.DataSources.Servers,
                recommendations = context.Config.DataSources.Recommendations,
                quotes = context.Config.DataSources.Quotes
            },
            writeEndpoint = context.Config.WriteEndpoint
        };
        context.Send(state);
    }
    public void Dispose() => context.Dispose();
}
