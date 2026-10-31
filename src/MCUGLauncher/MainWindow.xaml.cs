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
using MCUGLauncher.Authentication;
using MCUGLauncher.Commands;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace MCUGLauncher;

public partial class MainWindow : Window
{
    readonly ConfigStore _store = new();
    readonly CommunityService _community = new();
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    readonly WindowChromeController _chrome;
    readonly UiBridge _bridge;
    readonly AccountSession _accounts;
    readonly UiCommandRouter _router;
    readonly CommunityFeed _feed;
    readonly LaunchCoordinator _launch;
    readonly ServerCommands _servers;
    readonly ContentCommands _content;
    AppConfig _config;
    bool _ready;
    string _uiDirectory = "";

    public MainWindow()
    {
        InitializeComponent();
        _chrome = new WindowChromeController(this);
        _bridge = new UiBridge(Browser, Dispatcher);
        _config = _store.Load();
        _accounts = new AccountSession(_store, _config, _bridge, _http);
        _accounts.Changed += SendState;
        _feed = new CommunityFeed(_community, _store, _config, _bridge);
        _launch = new LaunchCoordinator(_store, _config, _bridge, _accounts, _http);
        _servers = new ServerCommands(_store, _config, _bridge, SendState);
        _content = new ContentCommands(_store, _config, _bridge, _http, SendState);
        _router = new UiCommandRouter();
        ConfigureCommands();
        Loaded += OnLoaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _chrome.Initialize();
    }

    void BeginWindowDrag() => _chrome.BeginDrag();

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
            _uiDirectory = EmbeddedUiCache.Extract();
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
            await _router.DispatchAsync(doc.RootElement);
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "error",
                message = ex.Message
            });
        }
    }

    void ConfigureCommands()
    {
        _router.Register("ready", ignored =>
        {
            _ready = true;
            SendState();
            _ = SendCommunityAsync();
            _ = _accounts.RestoreMicrosoftAsync();
        });
        _router.Register("requestState", _ => SendState());
        _router.Register("launch", root => LaunchAsync(UiBridge.ReadString(root, "version")));
        _router.Register("window", root => HandleWindow(UiBridge.ReadString(root, "action")));
        _router.Register("drag", _ => BeginWindowDrag());
        _router.Register("selectVersion", root =>
        {
            _config.LastVersion = UiBridge.ReadString(root, "version");
            _config.LastInstance = _config.LastVersion;
            _store.Save(_config);
            SendState();
        });
        _router.Register("selectInstance", root =>
        {
            _config.LastInstance = UiBridge.ReadString(root, "instance");
            _store.Save(_config);
            SendState();
        });
        _router.Register("openFolder", root => OpenFolder(UiBridge.ReadString(root, "kind")));
        _router.Register("pingServer", root => PingAsync(UiBridge.ReadString(root, "address"), UiBridge.ReadString(root, "id")));
        _router.Register("saveServer", root => SaveServer(root));
        _router.Register("deleteServer", root => DeleteServer(UiBridge.ReadString(root, "address")));
        _router.Register("toggleMod", root => ToggleMod(UiBridge.ReadString(root, "file"),
            root.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean()));
        _router.Register("deleteMod", root => DeleteMod(UiBridge.ReadString(root, "file")));
        _router.Register("deleteVersion", root => DeleteVersion(UiBridge.ReadString(root, "version")));
        _router.Register("searchContent", async root =>
        {
            Log("searchContent " + UiBridge.ReadString(root, "category") + " " + UiBridge.ReadString(root, "query"));
            await SearchContentAsync(root);
        });
        _router.Register("installContent", root => InstallContentAsync(root));
        _router.Register("vanillaVersions", _ => SendVanillaVersionsAsync());
        _router.Register("installVanilla", root => InstallVanillaAsync(UiBridge.ReadString(root, "version")));
        _router.Register("installMod", root => InstallModAsync(root));
        _router.Register("saveSettings", root => SaveSettings(root));
        _router.Register("pickFolder", root => PickFolder(UiBridge.ReadString(root, "target")));
        _router.Register("pickFile", root => PickFile(UiBridge.ReadString(root, "target")));
        _router.Register("scanJava", _ => ScanJavaAsync());
        _router.Register("backupSaves", _ => BackupSaves());
        _router.Register("readLog", _ => ReadLog());
        _router.Register("systemInfo", _ => SendSystemInfo());
        _router.Register("resetWindow", _ => ResetWindow());
        _router.Register("loginMicrosoft", _ => _accounts.LoginMicrosoftAsync());
        _router.Register("loginMua", root => _accounts.LoginMuaAsync(
            UiBridge.ReadString(root, "account"), UiBridge.ReadString(root, "password")));
        _router.Register("logoutAccount", _ => _accounts.Logout());
        _router.Register("testSource", root => TestSourceAsync(UiBridge.ReadString(root, "name"), UiBridge.ReadString(root, "url")));
        _router.Register("verifyKey", root => VerifyKey(UiBridge.ReadString(root, "role"), UiBridge.ReadString(root, "key")));
        _router.Register("publish", root => PublishAsync(root));
        _router.Register("saveAnnouncements", root => SaveCommunityLocal("announcements.local.json", root, "items"));
        _router.Register("saveRecommendations", root => SaveCommunityLocal("recommendations.local.json", root, "items"));
    }

    void SendState() => _bridge.Send(LauncherStateBuilder.Build(_config));

    async Task SendCommunityAsync() => await _feed.SendAsync();

    async Task LaunchAsync(string version) => await _launch.LaunchAsync(version);

    void HandleWindow(string action) => _chrome.Handle(action);

    void OpenFolder(string kind) => _servers.OpenFolder(kind);

    async Task PingAsync(string address, string id) => await _servers.PingAsync(address, id);

    void SaveServer(JsonElement root) => _servers.SaveServer(root);

    void DeleteServer(string address) => _servers.DeleteServer(address);










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
            _config.DataSources.Announcements = UiBridge.ReadString(sources, "announcements");
            _config.DataSources.Servers = UiBridge.ReadString(sources, "servers");
            _config.DataSources.Recommendations = UiBridge.ReadString(sources, "recommendations");
            _config.DataSources.Quotes = UiBridge.ReadString(sources, "quotes");
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
        _bridge.Send(new
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
            _bridge.Send(new
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
            _bridge.Send(new
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
            _bridge.Send(new
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
            _bridge.Send(new
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
                _bridge.Send(new
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
            _bridge.Send(new
            {
                type = "backupDone",
                path = zip
            });
        }
        catch (Exception ex)
        {
            _bridge.Send(new
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
            _bridge.Send(new
            {
                type = "logText",
                text
            });
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "logText",
                text = "读取日志失败：" + ex.Message
            });
        }
    }

    void Log(string message) => AppLog.Write(_store, message);

    void SendSystemInfo()
    {
        var webview = "未知";
        try
        {
            webview = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch { }
        _bridge.Send(new
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
        _chrome.Reset();
        _bridge.Send(new
        {
            type = "toast",
            message = "窗口已恢复为 1200 × 780。"
        });
    }

    async Task TestSourceAsync(string name, string url)
    {
        var result = await _community.TestAsync(url, CancellationToken.None);
        _bridge.Send(new
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
            _bridge.Send(new
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
                _bridge.Send(new
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
                _bridge.Send(new
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
        _bridge.Send(new
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
        _bridge.Send(new
        {
            type = "community",
            payload = new Dictionary<string, string> { [key] = items }
        });
        _bridge.Send(new
        {
            type = "saved",
            message = "本地内容已更新。"
        });
    }

    async Task PublishAsync(JsonElement root)
    {
        var title = UiBridge.ReadString(root, "title");
        var body = UiBridge.ReadString(root, "body");
        if (title.Length == 0 || body.Length == 0)
        {
            _bridge.Send(new
            {
                type = "publishResult",
                ok = false,
                message = "标题和正文不能为空。"
            });
            return;
        }

        var imageData = UiBridge.ReadString(root, "imageData");
        var record = new
        {
            id = Guid.NewGuid().ToString("N"),
            title,
            body,
            tag = "公告",
            author = _config.Player,
            publishedAt = DateTimeOffset.Now.ToString("O"),
            expiresAt = UiBridge.ReadString(root, "expiresAt"),
            imageUrl = imageData.StartsWith("data:image/", StringComparison.Ordinal) ? imageData : ""
        };
        var file = Path.Combine(_store.DataDirectory, "announcements.local.json");
        var list = File.Exists(file)
            ? JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(file)) ?? []
            : [];
        list.Add(JsonSerializer.SerializeToElement(record));
        var localJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, localJson);
        _bridge.Send(new
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
                _bridge.Send(new
                {
                    type = "publishResult",
                    ok = false,
                    message = "已保存到本机，但同步失败：" + ex.Message
                });
                return;
            }
        }

        _bridge.Send(new
        {
            type = "publishResult",
            ok = true,
            pending = string.IsNullOrWhiteSpace(_config.WriteEndpoint),
            message = string.IsNullOrWhiteSpace(_config.WriteEndpoint) ? "已保存为待发布" : "公告已发布"
        });
    }




    Task SendVanillaVersionsAsync() => _content.SendVanillaVersionsAsync();
    Task InstallVanillaAsync(string version) => _content.InstallVanillaAsync(version);
    Task SearchContentAsync(JsonElement root) => _content.SearchContentAsync(root);
    Task InstallContentAsync(JsonElement root) => _content.InstallContentAsync(root);
    void ToggleMod(string file, bool enabled) => _content.ToggleMod(file, enabled);
    void DeleteMod(string file) => _content.DeleteMod(file);
    void DeleteVersion(string version) => _content.DeleteVersion(version);
    ModInfo? FindMod(string file) => _content.FindMod(file);
    Task InstallModAsync(JsonElement root) => _content.InstallModAsync(root);
}
