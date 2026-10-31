using System.Text.Json;
using BlockLaunch;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;

namespace MCUGLauncher.Commands;

/// <summary>
/// 内容与版本命令。
/// </summary>
/// <remarks>
/// 依赖 <see cref="ConfigStore"/>、<see cref="AppConfig"/> 读写配置与实例，<see cref="HttpClient"/> 访问 Mojang/Modrinth，<see cref="UiBridge"/> 回推，<see cref="VersionCatalog"/>、<c>GameInstaller</c>、<c>ContentService</c> 完成安装与下载；构造时注入 <c>SendState</c> 回调。网络与安装均异步执行，异常转换为 contentError/toast。
/// </remarks>
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
    /// 读取 Mojang 版本清单并返回最近的原版版本。
    /// </summary>
    /// <remarks>
    /// 只取 type=release 的前 24 条；失败回推 contentError。
    /// </remarks>
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
    /// 安装指定原版版本。
    /// </summary>
    /// <param name="version">要安装的原版版本号</param>
    /// <remarks>
    /// 调用 GameInstaller 下载并安装；成功后写入 LastVersion 并刷新状态。
    /// </remarks>
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
    /// 在 Modrinth 搜索模组/整合包/资源包/光影。
    /// </summary>
    /// <param name="root">包含 category 与 query 的消息</param>
    /// <remarks>
    /// 结合当前实例的加载器与 Minecraft 版本做 facets 过滤；请求超时 12 秒；结果回推 contentList。
    /// </remarks>
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
    /// 下载并安装 Modrinth 项目文件。
    /// </summary>
    /// <param name="root">包含 id 与 category 的消息</param>
    /// <remarks>
    /// 按 category 解析目标目录（mods/resourcepacks/shaderpacks/datapacks），整合包下载到数据目录 downloads；完成后刷新状态。
    /// </remarks>
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
    /// 启用或禁用本地模组。
    /// </summary>
    /// <param name="file">模组文件名</param>
    /// <param name="enabled">true 启用，false 禁用</param>
    /// <remarks>
    /// 按文件名在当前实例 mods 中查找并调用 VersionCatalog.SetEnabled。
    /// </remarks>
    public void ToggleMod(string file, bool enabled)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.SetEnabled(mod, enabled);
        SendState();
    }


    /// <summary>
    /// 删除本地模组文件。
    /// </summary>
    /// <param name="file">要删除的模组文件名</param>
    /// <remarks>
    /// 目标不存在时静默返回。
    /// </remarks>
    public void DeleteMod(string file)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.Delete(mod);
        SendState();
    }


    /// <summary>
    /// 删除整个版本目录。
    /// </summary>
    /// <param name="version">要删除的版本目录名</param>
    /// <remarks>
    /// 拒绝空名称与包含非法文件名字符的版本。
    /// </remarks>
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
    /// 按文件名查找本地模组。
    /// </summary>
    /// <param name="file">模组文件名</param>
    /// <returns>匹配的模组信息；未找到时为 null</returns>
    /// <remarks>
    /// 返回匹配的 ModInfo，未找到返回 null。
    /// </remarks>
    public ModInfo? FindMod(string file)
    {
        var catalog = VersionCatalog.Load(_config.Root, _config.LastInstance);
        return VersionCatalog.ReadMods(_config.Root, catalog.Selected)
            .FirstOrDefault(item => item.File.Equals(file, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// 按项目 id 安装 Mod 到实例或指定目录。
    /// </summary>
    /// <param name="root">包含 id、mode、path 的消息</param>
    /// <remarks>
    /// mode=instance 时安装到当前实例 mods，否则使用 path 指定目录。
    /// </remarks>
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
