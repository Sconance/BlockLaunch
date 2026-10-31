using System.Diagnostics;
using System.Reflection;
using System.IO.Compression;
using System.Text.Json;
using BlockLaunch;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Windows;

namespace MCUGLauncher.Commands;

/// <summary>
/// 设置与系统命令：保存设置、选择目录/文件、Java 扫描、存档备份、日志/系统信息、
/// 社区数据保存、发布源测试与公告发布。
/// </summary>
sealed class SettingsCommands(
    ConfigStore store,
    AppConfig config,
    UiBridge bridge,
    HttpClient http,
    CommunityService community,
    Action stateChanged,
    Action resetWindow,
    Window owner)
{
    readonly ConfigStore _store = store;
    readonly AppConfig _config = config;
    readonly UiBridge _bridge = bridge;
    readonly HttpClient _http = http;
    readonly CommunityService _community = community;
    readonly Action _stateChanged = stateChanged;
    readonly Action _resetWindow = resetWindow;
    readonly Window _owner = owner;

    void SendState() => _stateChanged();

    void Log(string message) => AppLog.Write(_store, message);

    public void SaveSettings(JsonElement root)
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


    public void PickFolder(string target)
    {
        var dialog = new OpenFolderDialog { Title = "选择文件夹" };
        if (!string.IsNullOrWhiteSpace(_config.Root))
            dialog.InitialDirectory = _config.Root;
        if (dialog.ShowDialog(_owner) == true)
            _bridge.Send(new
            {
                type = "pickedPath",
                target,
                path = dialog.FolderName
            });
    }


    public void PickFile(string target)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择文件",
            Filter = target == "java" ? "Java|java.exe;javaw.exe|所有文件|*.*" : "所有文件|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(_owner) == true)
            _bridge.Send(new
            {
                type = "pickedPath",
                target,
                path = dialog.FileName
            });
    }


    public Task ScanJavaAsync()
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


    public void BackupSaves()
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


    public void ReadLog()
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


    public void SendSystemInfo()
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


    public void ResetWindow()
    {
        _resetWindow();
        _bridge.Send(new
        {
            type = "toast",
            message = "窗口已恢复为 1200 × 780。"
        });
    }


    public async Task TestSourceAsync(string name, string url)
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


    public void VerifyKey(string role, string key)
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


    public void SaveCommunityLocal(string fileName, JsonElement root, string property)
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


    public async Task PublishAsync(JsonElement root)
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
}
