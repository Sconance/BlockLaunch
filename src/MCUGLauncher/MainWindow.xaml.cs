using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CmlLib.Core.Auth;
using BlockLaunch;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace MCUGLauncher;

public partial class MainWindow : Window
{
    readonly ConfigStore _store = new();
    readonly CommunityService _community = new();
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    MicrosoftAccountService? _microsoft;
    MuaAccountService? _mua;
    MSession? _onlineSession;
    AppConfig _config;
    bool _ready;
    string _uiDirectory = "";

    public MainWindow()
    {
        InitializeComponent();
        _config = _store.Load();
        var authDirectory = Path.Combine(_store.DataDirectory, "auth");
        try
        {
            _microsoft = new MicrosoftAccountService(authDirectory, _http);
            _mua = new MuaAccountService(authDirectory, _http);
            if (_config.AccountType == "mua")
            {
                var saved = _mua.Read();
                if (saved != null)
                {
                    _onlineSession = MuaAccountService.Session(saved);
                    _config.Player = saved.Profile.Name;
                }
            }
        }
        catch
        {
            // A damaged account cache must never prevent the launcher from opening.
            _config.AccountType = "offline";
        }
        Loaded += OnLoaded;
    }

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X; public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left; public int Top; public int Right; public int Bottom;
    }

    DispatcherTimer? _dragTimer;
    IntPtr _dragWindow;
    POINT _dragCursor;
    RECT _dragRect;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Let Windows 11 round the real window frame; WPF's chrome alone cannot.
        var hwnd = new WindowInteropHelper(this).Handle;
        var preference = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(hwnd, 33, ref preference, sizeof(int));
    }

    void BeginWindowDrag()
    {
        // WebView2 owns the mouse, so track the cursor on a timer instead of using DragMove().
        _dragWindow = new WindowInteropHelper(this).Handle;
        GetCursorPos(out _dragCursor);
        GetWindowRect(_dragWindow, out _dragRect);
        if (_dragTimer is null)
        {
            _dragTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            _dragTimer.Tick += (_, _) => DragTick();
        }
        _dragTimer.Start();
    }

    void DragTick()
    {
        if ((GetAsyncKeyState(0x01) & 0x8000) == 0)
        {
            _dragTimer?.Stop();
            return;
        }
        GetCursorPos(out var cursor);
        SetWindowPos(_dragWindow, IntPtr.Zero,
            _dragRect.Left + cursor.X - _dragCursor.X,
            _dragRect.Top + cursor.Y - _dragCursor.Y,
            0, 0, 0x0001 | 0x0004 | 0x0010);
    }

    void FitUiToWindow()
    {
        // The UI is laid out for a 1200x780 DIP client; WebView2 already maps CSS pixels to DIPs.
        Browser.ZoomFactor = 1.0;
    }

    async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log("Launcher started");
        try
        {
            _uiDirectory = PrepareUi();
            var dataFolder = Path.Combine(_store.DataDirectory, "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, dataFolder);
            await Browser.EnsureCoreWebView2Async(environment);
            FitUiToWindow();
            Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", _uiDirectory, CoreWebView2HostResourceAccessKind.Allow);
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.WebMessageReceived += OnWebMessage;
            Browser.NavigationCompleted += OnNavigationCompleted;
            Browser.Source = new Uri("https://app.local/index.html");
        }
        catch (Exception ex)
        {
            MessageBox.Show("界面初始化失败。\n\n" + ex.Message, "MCUG Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    static string PrepareUi()
    {
        var target = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MCUGLauncher",
            "ui-0.22.3");
        Directory.CreateDirectory(target);
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith("MCUGLauncher.Ui.")))
        {
            var file = Path.Combine(target, name["MCUGLauncher.Ui.".Length..]);
            // Embedded resource names flatten folder separators to dots.
            if (file.EndsWith("assets.logo.png", StringComparison.OrdinalIgnoreCase))
                file = Path.Combine(target, "assets", "logo.png");
            var folder = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            using var source = assembly.GetManifestResourceStream(name);
            if (source == null)
                continue;
            using var destination = File.Create(file);
            source.CopyTo(destination);
        }
        return target;
    }

    void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            LoadingLayer.Visibility = Visibility.Collapsed;
            if (_ready)
                SendState();
        }
        else
        {
            LoadingLayer.Visibility = Visibility.Visible;
        }
    }

    async void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            var type = ReadString(root, "type");
            switch (type)
            {
                case "ready":
                    _ready = true;
                    SendState();
                    _ = SendCommunityAsync();
                    _ = RestoreMicrosoftAsync();
                    break;
                case "requestState":
                    SendState();
                    break;
                case "launch":
                    await LaunchAsync(ReadString(root, "version"));
                    break;
                case "window":
                    HandleWindow(ReadString(root, "action"));
                    break;
                case "drag":
                    BeginWindowDrag();
                    break;
                case "selectVersion":
                    _config.LastVersion = ReadString(root, "version");
                    _config.LastInstance = _config.LastVersion;
                    _store.Save(_config);
                    SendState();
                    break;
                case "selectInstance":
                    _config.LastInstance = ReadString(root, "instance");
                    _store.Save(_config);
                    SendState();
                    break;
                case "openFolder":
                    OpenFolder(ReadString(root, "kind"));
                    break;
                case "pingServer":
                    await PingAsync(ReadString(root, "address"), ReadString(root, "id"));
                    break;
                case "saveServer":
                    SaveServer(root);
                    break;
                case "deleteServer":
                    DeleteServer(ReadString(root, "address"));
                    break;
                case "toggleMod":
                    ToggleMod(ReadString(root, "file"), root.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean());
                    break;
                case "deleteMod":
                    DeleteMod(ReadString(root, "file"));
                    break;
                case "deleteVersion":
                    DeleteVersion(ReadString(root, "version"));
                    break;
                case "searchContent":
                    Log("searchContent " + ReadString(root, "category") + " " + ReadString(root, "query"));
                    await SearchContentAsync(root);
                    break;
                case "installContent":
                    await InstallContentAsync(root);
                    break;
                case "vanillaVersions":
                    await SendVanillaVersionsAsync();
                    break;
                case "installVanilla":
                    await InstallVanillaAsync(ReadString(root, "version"));
                    break;
                case "installMod":
                    await InstallModAsync(root);
                    break;
                case "saveSettings":
                    SaveSettings(root);
                    break;
                case "pickFolder":
                    PickFolder(ReadString(root, "target"));
                    break;
                case "pickFile":
                    PickFile(ReadString(root, "target"));
                    break;
                case "scanJava":
                    await ScanJavaAsync();
                    break;
                case "backupSaves":
                    BackupSaves();
                    break;
                case "readLog":
                    ReadLog();
                    break;
                case "systemInfo":
                    SendSystemInfo();
                    break;
                case "resetWindow":
                    ResetWindow();
                    break;
                case "loginMicrosoft":
                    await LoginMicrosoftAsync();
                    break;
                case "loginMua":
                    await LoginMuaAsync(ReadString(root, "account"), ReadString(root, "password"));
                    break;
                case "logoutAccount":
                    _onlineSession = null;
                    _config.AccountType = "offline";
                    _store.Save(_config);
                    SendState();
                    break;
                case "testSource":
                    await TestSourceAsync(ReadString(root, "name"), ReadString(root, "url"));
                    break;
                case "verifyKey":
                    VerifyKey(ReadString(root, "role"), ReadString(root, "key"));
                    break;
                case "publish":
                    await PublishAsync(root);
                    break;
                case "saveAnnouncements":
                    SaveCommunityLocal("announcements.local.json", root, "items");
                    break;
                case "saveRecommendations":
                    SaveCommunityLocal("recommendations.local.json", root, "items");
                    break;
            }
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "error",
                message = ex.Message
            });
        }
    }

    void SendState()
    {
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        _config.LastInstance = catalog.Selected;
        if (string.IsNullOrWhiteSpace(_config.LastVersion) ||
            !catalog.Instances.Any(item => item.Name.Equals(_config.LastVersion, StringComparison.OrdinalIgnoreCase)))
            _config.LastVersion = catalog.Selected;

        var mods = VersionCatalog.ReadMods(_config.Root, catalog.Selected);
        var state = new
        {
            type = "state",
            player = _config.Player,
            accountType = _config.AccountType,
            accountLabel = _config.AccountType switch
            {
                "microsoft" => "正版账号 · 在线验证",
                "mua" => "MUA 账号 · 外置登录",
                _ => "离线账号 · 本地启动"
            },
            root = _config.Root,
            java = _config.Java,
            width = _config.Width,
            height = _config.Height,
            selectedVersion = _config.LastVersion,
            selectedInstance = catalog.Selected,
            ram = _config.Ram,
            anonymous = _config.Anonymous,
            communityKey = !string.IsNullOrWhiteSpace(_config.CommunityKey),
            adminKey = !string.IsNullOrWhiteSpace(_config.AdminKey),
            skinStation = _config.SkinStation,
            cape = _config.Cape,
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
            servers = _config.Servers,
            dataSources = new
            {
                announcements = _config.DataSources.Announcements,
                servers = _config.DataSources.Servers,
                recommendations = _config.DataSources.Recommendations,
                quotes = _config.DataSources.Quotes
            },
            writeEndpoint = _config.WriteEndpoint
        };
        Send(state);
    }

    async Task SendCommunityAsync()
    {
        var items = new Dictionary<string, string>();
        var localAnn = Path.Combine(_store.DataDirectory, "announcements.local.json");
        if (File.Exists(localAnn))
            items["announcements"] = File.ReadAllText(localAnn);
        var localRecs = Path.Combine(_store.DataDirectory, "recommendations.local.json");
        if (File.Exists(localRecs))
            items["recommendations"] = File.ReadAllText(localRecs);
        var jobs = new[]
        {
            ("announcements", _config.DataSources.Announcements),
            ("recommendations", _config.DataSources.Recommendations),
            ("quotes", _config.DataSources.Quotes)
        };
        foreach (var (name, url) in jobs)
        {
            if (items.ContainsKey(name))
                continue;
            var text = await _community.ReadAsync(url, CancellationToken.None);
            if (text != null)
                items[name] = text;
        }
        if (items.Count > 0)
            Send(new
            {
                type = "community",
                payload = items
            });
    }

    async Task LoginMicrosoftAsync()
    {
        if (_microsoft == null)
        {
            Send(new
            {
                type = "authResult",
                ok = false,
                message = "登录组件未就绪。"
            });
            return;
        }
        Send(new
        {
            type = "authResult",
            pending = true,
            message = "已打开微软登录页，请完成授权…"
        });
        try
        {
            var session = await _microsoft.Authenticate(true, CancellationToken.None);
            _onlineSession = session;
            _config.AccountType = "microsoft";
            _config.Player = session.Username ?? _config.Player;
            _store.Save(_config);
            Send(new
            {
                type = "authResult",
                ok = true,
                message = $"已登录正版账号 {session.Username}"
            });
            SendState();
        }
        catch (Exception)
        {
            Send(new
            {
                type = "authResult",
                ok = false,
                message = "微软登录未完成。请检查网络后重试；已保存的账号不会被覆盖。"
            });
        }
    }

    async Task RestoreMicrosoftAsync()
    {
        if (_config.AccountType != "microsoft" || _microsoft == null)
            return;
        try
        {
            var session = await _microsoft.Authenticate(false, CancellationToken.None);
            _onlineSession = session;
            _config.Player = session.Username ?? _config.Player;
            _store.Save(_config);
            SendState();
        }
        catch
        {
            _onlineSession = null;
            _config.AccountType = "offline";
            _store.Save(_config);
            SendState();
        }
    }

    async Task LoginMuaAsync(string account, string password)
    {
        if (_mua == null)
        {
            Send(new
            {
                type = "authResult",
                ok = false,
                message = "登录组件未就绪。"
            });
            return;
        }
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrEmpty(password))
        {
            Send(new
            {
                type = "authResult",
                ok = false,
                message = "请填写 MUA 账号和密码。"
            });
            return;
        }
        Send(new
        {
            type = "authResult",
            pending = true,
            message = "正在登录 MUA…"
        });
        try
        {
            var credential = await _mua.Login(account.Trim(), password,
                profiles => Task.FromResult<MuaProfile?>(profiles.FirstOrDefault()), CancellationToken.None);
            _onlineSession = MuaAccountService.Session(credential);
            _config.AccountType = "mua";
            _config.Player = credential.Profile.Name;
            _store.Save(_config);
            Send(new
            {
                type = "authResult",
                ok = true,
                message = $"已登录 MUA 账号 {credential.Profile.Name}"
            });
            SendState();
        }
        catch (Exception)
        {
            Send(new
            {
                type = "authResult",
                ok = false,
                message = "MUA 登录失败。请检查账号、密码和网络后重试。"
            });
        }
    }

    async Task LaunchAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            version = _config.LastVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            Send(new
            {
                type = "launchResult",
                ok = false,
                message = "还没有可启动的版本。"
            });
            return;
        }

        var instanceDir = Path.Combine(_config.Root, "versions", version);
        if (!Directory.Exists(instanceDir))
        {
            Send(new
            {
                type = "launchResult",
                ok = false,
                message = "版本目录不存在：" + version
            });
            return;
        }

        try
        {
            var session = _onlineSession ?? new MSession
            {
                Username = _config.Player,
                UUID = OfflineUuid(_config.Player),
                AccessToken = "0",
                ClientToken = Guid.NewGuid().ToString("N"),
                UserType = "legacy"
            };
            BlockLaunch.AuthlibLaunch? external = null;
            if (_config.AccountType == "mua" && _onlineSession != null && _mua != null)
            {
                var metadata = await _mua.Metadata(CancellationToken.None);
                var jar = await BlockLaunch.AuthlibLaunch.Prepare(
                    Path.Combine(_store.DataDirectory, "authlib-injector"), _http, CancellationToken.None);
                external = new BlockLaunch.AuthlibLaunch(jar, metadata.Json);
            }
            var defaults = new BlockLaunch.LaunchSettings();
            var settings = new BlockLaunch.LaunchSettings
            {
                AutoMemory = false,
                RamMb = _config.Ram,
                Java = _config.Java,
                Width = _config.Width,
                Height = _config.Height,
                Isolation = defaults.Isolation,
                Visibility = defaults.Visibility,
                Priority = defaults.Priority,
                VersionInfo = "MCUGLauncher"
            };
            var process = await BlockLaunch.LaunchService.Build(
                new BlockLaunch.LaunchRequest(
                    _config.Root,
                    instanceDir,
                    version,
                    _config.Java,
                    _config.Ram,
                    _config.Width,
                    _config.Height,
                    session,
                    settings,
                    external),
                CancellationToken.None);
            process.Start();
            _config.LastVersion = version;
            _config.LastInstance = version;
            _store.Save(_config);
            Send(new
            {
                type = "launchResult",
                ok = true,
                message = "已启动 " + version
            });
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "launchResult",
                ok = false,
                message = ex.Message
            });
        }
    }

    void HandleWindow(string action)
    {
        switch (action)
        {
            case "minimize":
                WindowState = WindowState.Minimized;
                break;
            case "close":
                Close();
                break;
            case "maximize":
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                break;
        }
    }

    void OpenFolder(string kind)
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

    async Task PingAsync(string address, string id)
    {
        var result = await ServerPingService.PingAsync(address, CancellationToken.None);
        Send(new
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

    void SaveServer(JsonElement root)
    {
        var name = ReadString(root, "name");
        var address = ReadString(root, "address").Trim();
        var oldAddress = ReadString(root, "oldAddress").Trim();
        if (address.Length == 0)
        {
            Send(new
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
        Send(new
        {
            type = "serverResult",
            ok = true,
            message = (oldAddress.Length > 0 ? "已更新 " : "已添加 ") + name
        });
        SendState();
    }

    void DeleteServer(string address)
    {
        _config.Servers.RemoveAll(s => s.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
        _store.Save(_config);
        SendState();
    }

    void ToggleMod(string file, bool enabled)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.SetEnabled(mod, enabled);
        SendState();
    }

    async Task SendVanillaVersionsAsync()
    {
        try
        {
            var json = await _http.GetStringAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("versions").EnumerateArray()
                .Where(v => v.GetProperty("type").GetString() == "release")
                .Take(24)
                .Select(v => new
                {
                    id = v.GetProperty("id").GetString() ?? "",
                    type = v.GetProperty("type").GetString() ?? "release",
                    released = v.GetProperty("releaseTime").GetDateTimeOffset().ToString("yyyy-MM-dd")
                })
                .ToList();
            Send(new
            {
                type = "vanillaList",
                items
            });
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "contentError",
                message = "读取原版版本列表失败：" + ex.Message
            });
        }
    }

    async Task InstallVanillaAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return;
        Send(new
        {
            type = "toast",
            message = $"开始安装原版 {version}，请稍候…"
        });
        try
        {
            var installer = new GameInstaller(_http, _config.Root, _config.Java, Log);
            await installer.Install(new PackRequirements(version, "vanilla", ""));
            _config.LastVersion = version;
            _store.Save(_config);
            Send(new
            {
                type = "toast",
                message = $"原版 {version} 已安装。"
            });
            SendState();
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "toast",
                message = "安装失败：" + ex.Message
            });
        }
    }

    async Task SearchContentAsync(JsonElement root)
    {
        var category = ReadString(root, "category");
        var query = ReadString(root, "query");
        if (category is "vanilla")
        {
            await SendVanillaVersionsAsync();
            return;
        }
        try
        {
            var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
            var details = VersionCatalog.ReadDetails(_config.Root, catalog.Selected);
            var loader = details.Loader is "Fabric" or "Forge" or "NeoForge" or "Quilt"
                ? details.Loader.ToLowerInvariant()
                : "";
            var projectType = category switch
            {
                "modpack" => "modpack",
                "resourcepack" => "resourcepack",
                "shader" => "shader",
                _ => "mod"
            };
            var minecraft = GuessMinecraft(details.Minecraft);
            var facets = new List<string> { $"[\"project_type:{projectType}\"]" };
            Log($"search stage facets mc={details.Minecraft}->{minecraft} loader={loader} type={category}");
            if (!string.IsNullOrWhiteSpace(minecraft))
                facets.Add($"[\"versions:{minecraft}\"]");
            if (category == "mod" && !string.IsNullOrWhiteSpace(loader))
                facets.Add($"[\"categories:{loader}\"]");
            if (category == "datapack")
                facets.Add("[\"categories:datapack\"]");
            var url = "https://api.modrinth.com/v2/search?limit=12&index=downloads"
                + "&query=" + Uri.EscapeDataString(query ?? "")
                + "&facets=" + Uri.EscapeDataString("[" + string.Join(",", facets) + "]");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            Log("search stage request");
            using var response = await _http.GetAsync(url, cts.Token);
            Log("search stage response " + (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
            var items = new List<object>();
            if (doc.RootElement.TryGetProperty("hits", out var hits))
            {
                foreach (var hit in hits.EnumerateArray())
                {
                    var tags = new List<string>();
                    if (hit.TryGetProperty("categories", out var cats))
                        foreach (var cat in cats.EnumerateArray())
                            if (cat.GetString() is { } c && !tags.Contains(c))
                                tags.Add(c);
                    items.Add(new
                    {
                        id = hit.TryGetProperty("project_id", out var pid) ? pid.GetString() : "",
                        name = hit.TryGetProperty("title", out var title) ? title.GetString() : "",
                        author = hit.TryGetProperty("author", out var author) ? author.GetString() : "",
                        description = hit.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                        icon = hit.TryGetProperty("icon_url", out var icon) ? icon.GetString() : "",
                        downloads = hit.TryGetProperty("downloads", out var dl) ? dl.GetInt64() : 0,
                        tags = tags.Take(4),
                        type = category
                    });
                }
            }
            Log($"search stage parsed {items.Count}");
            Send(new
            {
                type = "contentList",
                category,
                items
            });
            Log("search stage sent");
        }
        catch (OperationCanceledException)
        {
            Send(new
            {
                type = "contentError",
                category,
                message = "读取 Modrinth 超时，请稍后重试。"
            });
        }
        catch (Exception ex)
        {
            Log("search failed " + ex);
            Send(new
            {
                type = "contentError",
                category,
                message = "读取 Modrinth 失败：" + ex.Message
            });
        }
    }

    async Task InstallContentAsync(JsonElement root)
    {
        var id = ReadString(root, "id");
        var category = ReadString(root, "category");
        if (string.IsNullOrWhiteSpace(id))
            return;
        Send(new
        {
            type = "toast",
            message = "正在解析文件…"
        });
        try
        {
            var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
            var details = VersionCatalog.ReadDetails(_config.Root, catalog.Selected);
            var loader = details.Loader is "Fabric" or "Forge" or "NeoForge" or "Quilt"
                ? details.Loader.ToLowerInvariant()
                : "";
            var minecraft = GuessMinecraft(details.Minecraft);
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(minecraft))
                query.Add("game_versions=" + Uri.EscapeDataString($"[\"{minecraft}\"]"));
            var loaderFilter = category == "datapack" ? "datapack" : category == "mod" ? loader : "";
            if (!string.IsNullOrWhiteSpace(loaderFilter))
                query.Add("loaders=" + Uri.EscapeDataString($"[\"{loaderFilter}\"]"));
            var url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(id)}/version?" + string.Join("&", query);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await _http.GetAsync(url, cts.Token);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
            string fileUrl = "";
            string fileName = "";
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var version in doc.RootElement.EnumerateArray())
                {
                    if (!version.TryGetProperty("files", out var files))
                        continue;
                    JsonElement? chosen = null;
                    foreach (var f in files.EnumerateArray())
                    {
                        if (f.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                        {
                            chosen = f;
                            break;
                        }
                        chosen ??= f;
                    }
                    if (chosen is not { } file)
                        continue;
                    if (file.TryGetProperty("url", out var u))
                        fileUrl = u.GetString() ?? "";
                    if (file.TryGetProperty("filename", out var fn))
                        fileName = fn.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(fileUrl))
                        break;
                }
            }
            if (string.IsNullOrWhiteSpace(fileUrl) || string.IsNullOrWhiteSpace(fileName))
                throw new IOException("没有找到与当前实例兼容的文件。");

            string folder;
            if (category == "modpack")
                folder = Path.Combine(_store.DataDirectory, "downloads");
            else
            {
                var instance = Path.Combine(_config.Root, "versions", catalog.Selected);
                folder = category switch
                {
                    "mod" => Path.Combine(instance, "mods"),
                    "resourcepack" => Path.Combine(instance, "resourcepacks"),
                    "shader" => Path.Combine(instance, "shaderpacks"),
                    "datapack" => Path.Combine(instance, "datapacks"),
                    _ => Path.Combine(instance, "mods")
                };
            }
            Directory.CreateDirectory(folder);
            var bytes = await _http.GetByteArrayAsync(fileUrl);
            var path = Path.Combine(folder, ContentService.SafeFile(fileName));
            await File.WriteAllBytesAsync(path, bytes);
            Send(new
            {
                type = "toast",
                message = $"已下载：{path}"
            });
            SendState();
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "toast",
                message = "下载失败：" + ex.Message
            });
        }
    }

    void DeleteMod(string file)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.Delete(mod);
        SendState();
    }

    void DeleteVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version) || version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return;
        var source = Path.Combine(_config.Root, "versions", version);
        if (!Directory.Exists(source))
        {
            Send(new
            {
                type = "toast",
                message = "版本目录不存在。"
            });
            return;
        }
        try
        {
            var trash = Path.Combine(_store.DataDirectory, "trash");
            Directory.CreateDirectory(trash);
            var target = Path.Combine(trash, $"{version}-{DateTime.Now:yyyyMMdd-HHmmss}");
            Directory.Move(source, target);
            Log($"Version moved to trash: {version} -> {target}");
            if (_config.LastVersion.Equals(version, StringComparison.OrdinalIgnoreCase))
                _config.LastVersion = "";
            _store.Save(_config);
            Send(new
            {
                type = "toast",
                message = $"版本已移到回收目录：{target}"
            });
            SendState();
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "toast",
                message = "删除版本失败：" + ex.Message
            });
        }
    }

    ModInfo? FindMod(string file)
    {
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        return VersionCatalog.ReadMods(_config.Root, catalog.Selected)
            .FirstOrDefault(item => item.File.Equals(file, StringComparison.OrdinalIgnoreCase));
    }

    async Task InstallModAsync(JsonElement root)
    {
        var id = ReadString(root, "id");
        var mode = ReadString(root, "mode");
        var path = ReadString(root, "path");
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        var target = mode == "local"
            ? path
            : Path.Combine(VersionCatalog.InstanceDirectory(_config.Root, catalog.Selected), "mods");
        if (string.IsNullOrWhiteSpace(target))
        {
            Send(new
            {
                type = "modResult",
                ok = false,
                message = "请选择保存位置。"
            });
            return;
        }
        Directory.CreateDirectory(target);
        try
        {
            var file = await new ModService().DownloadAsync(id, "1.20.1", target, CancellationToken.None);
            Send(new
            {
                type = "modResult",
                ok = true,
                message = "已下载 " + Path.GetFileName(file)
            });
            SendState();
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "modResult",
                ok = false,
                message = ex.Message
            });
        }
    }

    void SaveSettings(JsonElement root)
    {
        if (root.TryGetProperty("player", out var player))
            _config.Player = player.GetString() ?? _config.Player;
        if (root.TryGetProperty("anonymous", out var anonymous))
            _config.Anonymous = anonymous.GetBoolean();
        if (root.TryGetProperty("writeEndpoint", out var endpoint))
            _config.WriteEndpoint = endpoint.GetString() ?? "";
        if (root.TryGetProperty("dataSources", out var sources))
        {
            _config.DataSources.Announcements = ReadString(sources, "announcements");
            _config.DataSources.Servers = ReadString(sources, "servers");
            _config.DataSources.Recommendations = ReadString(sources, "recommendations");
            _config.DataSources.Quotes = ReadString(sources, "quotes");
        }
        if (root.TryGetProperty("ram", out var ram) && ram.TryGetInt32(out var ramValue))
            _config.Ram = Math.Clamp(ramValue, 512, 65536);
        if (root.TryGetProperty("root", out var gameRoot))
        {
            var value = gameRoot.GetString()?.Trim() ?? "";
            if (value.Length > 0)
                _config.Root = value;
        }
        if (root.TryGetProperty("java", out var java))
            _config.Java = java.GetString()?.Trim() ?? "";
        if (root.TryGetProperty("width", out var width) && width.TryGetInt32(out var widthValue))
            _config.Width = Math.Clamp(widthValue, 640, 7680);
        if (root.TryGetProperty("height", out var height) && height.TryGetInt32(out var heightValue))
            _config.Height = Math.Clamp(heightValue, 480, 4320);
        _store.Save(_config);
        Send(new
        {
            type = "settingsSaved"
        });
        SendState();
    }

    void PickFolder(string target)
    {
        var dialog = new OpenFolderDialog { Title = "选择文件夹" };
        if (!string.IsNullOrWhiteSpace(_config.Root))
            dialog.InitialDirectory = _config.Root;
        if (dialog.ShowDialog(this) == true)
            Send(new
            {
                type = "pickedPath",
                target,
                path = dialog.FolderName
            });
    }

    void PickFile(string target)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择文件",
            Filter = target == "java" ? "Java|java.exe;javaw.exe|所有文件|*.*" : "所有文件|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
            Send(new
            {
                type = "pickedPath",
                target,
                path = dialog.FileName
            });
    }

    Task ScanJavaAsync()
    {
        try
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(_config.Java))
                candidates.Add(_config.Java);
            foreach (var home in new[] { Environment.GetEnvironmentVariable("JAVA_HOME"), Environment.GetEnvironmentVariable("JDK_HOME") })
                if (!string.IsNullOrWhiteSpace(home))
                    candidates.Add(Path.Combine(home, "bin", "java.exe"));
            foreach (var part in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                if (part.Length > 0)
                    candidates.Add(Path.Combine(part, "java.exe"));

            var bases = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            var vendors = new[] { "Java", "Microsoft", "Eclipse Adoptium", "Zulu", "BellSoft", "Amazon Corretto" };
            foreach (var basis in bases.Where(Directory.Exists))
            {
                foreach (var vendor in vendors)
                {
                    var vendorRoot = Path.Combine(basis, vendor);
                    if (!Directory.Exists(vendorRoot))
                        continue;
                    candidates.Add(Path.Combine(vendorRoot, "bin", "java.exe"));
                    foreach (var child in Directory.GetDirectories(vendorRoot, "*", SearchOption.TopDirectoryOnly).Take(24))
                        candidates.Add(Path.Combine(child, "bin", "java.exe"));
                }
                var microsoft = Path.Combine(basis, "Microsoft");
                if (Directory.Exists(microsoft))
                    foreach (var child in Directory.GetDirectories(microsoft, "jdk*", SearchOption.TopDirectoryOnly).Take(24))
                        candidates.Add(Path.Combine(child, "bin", "java.exe"));
            }

            var list = candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .Select(path => new { Path = path, Major = 0, Architecture = "x64", Vendor = "本机", Error = "" })
                .ToList();
            Send(new
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
        catch (Exception ex)
        {
            Send(new
            {
                type = "toast",
                message = "扫描 Java 失败：" + ex.Message
            });
        }
        return Task.CompletedTask;
    }

    void BackupSaves()
    {
        try
        {
            var saves = Path.Combine(_config.Root, "saves");
            if (!Directory.Exists(saves))
            {
                Send(new
                {
                    type = "toast",
                    message = "还没有存档文件夹。"
                });
                return;
            }
            var folder = Path.Combine(_store.DataDirectory, "backups");
            Directory.CreateDirectory(folder);
            var zip = Path.Combine(folder, $"saves-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            ZipFile.CreateFromDirectory(saves, zip, CompressionLevel.Optimal, false);
            Send(new
            {
                type = "backupDone",
                path = zip
            });
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "toast",
                message = "备份失败：" + ex.Message
            });
        }
    }

    void ReadLog()
    {
        try
        {
            var path = Path.Combine(_store.DataDirectory, "launcher.log");
            var text = File.Exists(path) ? string.Join('\n', File.ReadLines(path).TakeLast(400)) : "还没有日志。启动或启动游戏后会写入这里。";
            Send(new
            {
                type = "logText",
                text
            });
        }
        catch (Exception ex)
        {
            Send(new
            {
                type = "logText",
                text = "读取日志失败：" + ex.Message
            });
        }
    }

    void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(_store.DataDirectory);
            File.AppendAllText(Path.Combine(_store.DataDirectory, "launcher.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    void SendSystemInfo()
    {
        var webview = "未知";
        try
        {
            webview = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch { }
        Send(new
        {
            type = "systemInfo",
            app = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.22.3",
            dotnet = Environment.Version.ToString(),
            webview,
            os = Environment.OSVersion.VersionString,
            root = _config.Root,
            data = _store.DataDirectory
        });
    }

    void ResetWindow()
    {
        Width = 1200;
        Height = 780;
        WindowState = WindowState.Normal;
        Send(new
        {
            type = "toast",
            message = "窗口已恢复为 1200 × 780。"
        });
    }

    async Task TestSourceAsync(string name, string url)
    {
        var result = await _community.TestAsync(url, CancellationToken.None);
        Send(new
        {
            type = "testResult",
            name,
            ok = result.Ok,
            message = result.Text,
            ms = result.Ms
        });
    }

    void VerifyKey(string role, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length < 8)
        {
            Send(new
            {
                type = "keyResult",
                role,
                ok = false,
                message = "密钥格式不正确，请向群管理员确认。"
            });
            return;
        }
        if (role == "admin")
        {
            if (string.IsNullOrWhiteSpace(_config.AdminKey))
            {
                _config.AdminKey = key.Trim();
                _store.Save(_config);
                Send(new
                {
                    type = "keyResult",
                    role,
                    ok = true,
                    message = "已设置本机管理员密钥。"
                });
                SendState();
                return;
            }
            if (!string.Equals(_config.AdminKey, key.Trim(), StringComparison.Ordinal))
            {
                Send(new
                {
                    type = "keyResult",
                    role,
                    ok = false,
                    message = "管理员密钥不匹配。"
                });
                return;
            }
        }
        else
        {
            _config.CommunityKey = key.Trim();
        }
        _store.Save(_config);
        Send(new
        {
            type = "keyResult",
            role,
            ok = true,
            message = "验证成功"
        });
        SendState();
    }

    void SaveCommunityLocal(string fileName, JsonElement root, string property)
    {
        var items = root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetRawText()
            : "[]";
        var file = Path.Combine(_store.DataDirectory, fileName);
        File.WriteAllText(file, items);
        var key = fileName.StartsWith("announcements", StringComparison.OrdinalIgnoreCase) ? "announcements" : "recommendations";
        Send(new
        {
            type = "community",
            payload = new Dictionary<string, string> { [key] = items }
        });
        Send(new
        {
            type = "saved",
            message = "本地内容已更新。"
        });
    }

    async Task PublishAsync(JsonElement root)
    {
        var title = ReadString(root, "title");
        var body = ReadString(root, "body");
        if (title.Length == 0 || body.Length == 0)
        {
            Send(new
            {
                type = "publishResult",
                ok = false,
                message = "标题和正文不能为空。"
            });
            return;
        }

        var imageData = ReadString(root, "imageData");
        var record = new
        {
            id = Guid.NewGuid().ToString("N"),
            title,
            body,
            tag = "公告",
            author = _config.Player,
            publishedAt = DateTimeOffset.Now.ToString("O"),
            expiresAt = ReadString(root, "expiresAt"),
            imageUrl = imageData.StartsWith("data:image/", StringComparison.Ordinal) ? imageData : ""
        };
        var file = Path.Combine(_store.DataDirectory, "announcements.local.json");
        var list = File.Exists(file)
            ? JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(file)) ?? []
            : [];
        list.Add(JsonSerializer.SerializeToElement(record));
        var localJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, localJson);
        Send(new
        {
            type = "community",
            payload = new Dictionary<string, string> { ["announcements"] = localJson }
        });

        if (!string.IsNullOrWhiteSpace(_config.WriteEndpoint))
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                using var response = await http.PostAsync(
                    _config.WriteEndpoint,
                    new StringContent(JsonSerializer.Serialize(record), System.Text.Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    throw new IOException($"HTTP {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                Send(new
                {
                    type = "publishResult",
                    ok = false,
                    message = "已保存到本机，但同步失败：" + ex.Message
                });
                return;
            }
        }

        Send(new
        {
            type = "publishResult",
            ok = true,
            pending = string.IsNullOrWhiteSpace(_config.WriteEndpoint),
            message = string.IsNullOrWhiteSpace(_config.WriteEndpoint) ? "已保存为待发布" : "公告已发布"
        });
    }

    void Send(object payload)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Send(payload));
            return;
        }
        Browser.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    static string GuessMinecraft(string value)
    {
        var match = System.Text.RegularExpressions.Regex.Match(value ?? "", @"\d+\.\d+(?:\.\d+)?");
        return match.Success ? match.Value : "";
    }

    static string OfflineUuid(string player)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + player));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash).ToString("N");
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
