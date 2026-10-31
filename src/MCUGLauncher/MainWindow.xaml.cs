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

/// <summary>
/// 提供 Main窗口 相关功能。
/// </summary>
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
    readonly SettingsCommands _settings;
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
        _settings = new SettingsCommands(_store, _config, _bridge, _http, _community, SendState, _chrome.Reset, this);
        _router = new UiCommandRouter();
        ConfigureCommands();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// 执行 OnSourceInitialized。
    /// </summary>
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

    void Log(string message) => AppLog.Write(_store, message);

    Task SendVanillaVersionsAsync() => _content.SendVanillaVersionsAsync();
    Task InstallVanillaAsync(string version) => _content.InstallVanillaAsync(version);
    Task SearchContentAsync(JsonElement root) => _content.SearchContentAsync(root);
    Task InstallContentAsync(JsonElement root) => _content.InstallContentAsync(root);
    void ToggleMod(string file, bool enabled) => _content.ToggleMod(file, enabled);
    void DeleteMod(string file) => _content.DeleteMod(file);
    void DeleteVersion(string version) => _content.DeleteVersion(version);
    ModInfo? FindMod(string file) => _content.FindMod(file);
    Task InstallModAsync(JsonElement root) => _content.InstallModAsync(root);

    void SaveSettings(JsonElement root) => _settings.SaveSettings(root);
    void PickFolder(string target) => _settings.PickFolder(target);
    void PickFile(string target) => _settings.PickFile(target);
    void ScanJavaAsync() => _settings.ScanJavaAsync();
    void BackupSaves() => _settings.BackupSaves();
    void ReadLog() => _settings.ReadLog();
    void SendSystemInfo() => _settings.SendSystemInfo();
    void ResetWindow() => _settings.ResetWindow();
    Task TestSourceAsync(string name, string url) => _settings.TestSourceAsync(name, url);
    void VerifyKey(string role, string key) => _settings.VerifyKey(role, key);
    void SaveCommunityLocal(string fileName, JsonElement root, string property) => _settings.SaveCommunityLocal(fileName, root, property);
    Task PublishAsync(JsonElement root) => _settings.PublishAsync(root);
}
