using System.Text.Json;
using BlockLaunch;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;

namespace MCUGLauncher.Commands;

/// <summary>
/// 内容与版本命令：原版版本列表/安装、Modrinth 搜索与下载、本地模组启停/删除、版本删除。
/// </summary>
sealed class ContentCommands(
    ConfigStore store,
    AppConfig config,
    UiBridge bridge,
    HttpClient http,
    Action stateChanged)
{
    readonly ConfigStore _store = store;
    readonly AppConfig _config = config;
    readonly UiBridge _bridge = bridge;
    readonly HttpClient _http = http;
    readonly Action _stateChanged = stateChanged;

    void SendState() => _stateChanged();

    void Log(string message) => AppLog.Write(_store, message);

    static string GuessMinecraft(string value)
    {
        var match = System.Text.RegularExpressions.Regex.Match(value ?? "", @"\d+\.\d+(?:\.\d+)?");
        return match.Success ? match.Value : "";
    }

    /// <summary>
    /// 发送VanillaVersions。
    /// </summary>
    public async Task SendVanillaVersionsAsync()
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
            _bridge.Send(new
            {
                type = "vanillaList",
                items
            });
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "contentError",
                message = "读取原版版本列表失败：" + ex.Message
            });
        }
    }


    /// <summary>
    /// 安装Vanilla。
    /// </summary>
    public async Task InstallVanillaAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return;
        _bridge.Send(new
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
            _bridge.Send(new
            {
                type = "toast",
                message = $"原版 {version} 已安装。"
            });
            SendState();
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "toast",
                message = "安装失败：" + ex.Message
            });
        }
    }


    /// <summary>
    /// 搜索内容。
    /// </summary>
    public async Task SearchContentAsync(JsonElement root)
    {
        var category = UiBridge.ReadString(root, "category");
        var query = UiBridge.ReadString(root, "query");
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
            _bridge.Send(new
            {
                type = "contentList",
                category,
                items
            });
            Log("search stage sent");
        }
        catch (OperationCanceledException)
        {
            _bridge.Send(new
            {
                type = "contentError",
                category,
                message = "读取 Modrinth 超时，请稍后重试。"
            });
        }
        catch (Exception ex)
        {
            Log("search failed " + ex);
            _bridge.Send(new
            {
                type = "contentError",
                category,
                message = "读取 Modrinth 失败：" + ex.Message
            });
        }
    }


    /// <summary>
    /// 安装内容。
    /// </summary>
    public async Task InstallContentAsync(JsonElement root)
    {
        var id = UiBridge.ReadString(root, "id");
        var category = UiBridge.ReadString(root, "category");
        if (string.IsNullOrWhiteSpace(id))
            return;
        _bridge.Send(new
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
            _bridge.Send(new
            {
                type = "toast",
                message = $"已下载：{path}"
            });
            SendState();
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "toast",
                message = "下载失败：" + ex.Message
            });
        }
    }


    /// <summary>
    /// 切换模组。
    /// </summary>
    public void ToggleMod(string file, bool enabled)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.SetEnabled(mod, enabled);
        SendState();
    }


    /// <summary>
    /// 删除模组。
    /// </summary>
    public void DeleteMod(string file)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.Delete(mod);
        SendState();
    }


    /// <summary>
    /// 删除版本。
    /// </summary>
    public void DeleteVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version) || version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return;
        var source = Path.Combine(_config.Root, "versions", version);
        if (!Directory.Exists(source))
        {
            _bridge.Send(new
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
            _bridge.Send(new
            {
                type = "toast",
                message = $"版本已移到回收目录：{target}"
            });
            SendState();
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "toast",
                message = "删除版本失败：" + ex.Message
            });
        }
    }


    /// <summary>
    /// 查找模组。
    /// </summary>
    public ModInfo? FindMod(string file)
    {
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        return VersionCatalog.ReadMods(_config.Root, catalog.Selected)
            .FirstOrDefault(item => item.File.Equals(file, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// 安装模组。
    /// </summary>
    public async Task InstallModAsync(JsonElement root)
    {
        var id = UiBridge.ReadString(root, "id");
        var mode = UiBridge.ReadString(root, "mode");
        var path = UiBridge.ReadString(root, "path");
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        var target = mode == "local"
            ? path
            : Path.Combine(VersionCatalog.InstanceDirectory(_config.Root, catalog.Selected), "mods");
        if (string.IsNullOrWhiteSpace(target))
        {
            _bridge.Send(new
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
            _bridge.Send(new
            {
                type = "modResult",
                ok = true,
                message = "已下载 " + Path.GetFileName(file)
            });
            SendState();
        }
        catch (Exception ex)
        {
            _bridge.Send(new
            {
                type = "modResult",
                ok = false,
                message = ex.Message
            });
        }
    }
}
