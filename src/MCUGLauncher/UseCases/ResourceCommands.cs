using BlockLaunch;
using System.Text.Json;
using MCUGLauncher.Services;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

// Handles resource use cases; no WPF controls or browser access.
internal sealed class ResourceCommands(LauncherContext context)
{
    public async Task SendVanillaVersionsAsync()
    {
        try
        {
            var json = await context.Http.GetStringAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
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
            context.Send(new
            {
                type = "vanillaList",
                items
            });
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "contentError",
                message = "读取原版版本列表失败：" + ex.Message
            });
        }
    }

    public async Task InstallVanillaAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return;
        context.Send(new
        {
            type = "toast",
            message = $"开始安装原版 {version}，请稍候…"
        });
        try
        {
            var installer = new GameInstaller(context.Http, context.Config.Root, context.Config.Java, context.Log);
            await installer.Install(new PackRequirements(version, "vanilla", ""));
            context.Config.LastVersion = version;
            context.Store.Save(context.Config);
            context.Send(new
            {
                type = "toast",
                message = $"原版 {version} 已安装。"
            });
            context.RefreshState();
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "toast",
                message = "安装失败：" + ex.Message
            });
        }
    }

    public async Task SearchContentAsync(JsonElement root)
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
            var catalog = VersionCatalog.Load(context.Config.Root, context.Config.LastInstance);
            var details = VersionCatalog.ReadDetails(context.Config.Root, catalog.Selected);
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
            var minecraft = details.Minecraft.Trim();
            var facets = new List<string> { $"[\"project_type:{projectType}\"]" };
            context.Log($"search stage facets mc={details.Minecraft}->{minecraft} loader={loader} type={category}");
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
            context.Log("search stage request");
            using var response = await context.Http.GetAsync(url, cts.Token);
            context.Log("search stage response " + (int)response.StatusCode);
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
            context.Log($"search stage parsed {items.Count}");
            context.Send(new
            {
                type = "contentList",
                category,
                items
            });
            context.Log("search stage sent");
        }
        catch (OperationCanceledException)
        {
            context.Send(new
            {
                type = "contentError",
                category,
                message = "读取 Modrinth 超时，请稍后重试。"
            });
        }
        catch (Exception ex)
        {
            context.Log("search failed " + ex);
            context.Send(new
            {
                type = "contentError",
                category,
                message = "读取 Modrinth 失败：" + ex.Message
            });
        }
    }

    public async Task InstallContentAsync(JsonElement root)
    {
        var id = ReadString(root, "id");
        var category = ReadString(root, "category");
        if (string.IsNullOrWhiteSpace(id))
            return;
        context.Send(new
        {
            type = "toast",
            message = "正在解析文件…"
        });
        try
        {
            var catalog = VersionCatalog.Load(context.Config.Root, context.Config.LastInstance);
            var details = VersionCatalog.ReadDetails(context.Config.Root, catalog.Selected);
            var loader = details.Loader is "Fabric" or "Forge" or "NeoForge" or "Quilt"
                ? details.Loader.ToLowerInvariant()
                : "";
            var minecraft = details.Minecraft.Trim();
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(minecraft))
                query.Add("game_versions=" + Uri.EscapeDataString($"[\"{minecraft}\"]"));
            var loaderFilter = category == "datapack" ? "datapack" : category == "mod" ? loader : "";
            if (!string.IsNullOrWhiteSpace(loaderFilter))
                query.Add("loaders=" + Uri.EscapeDataString($"[\"{loaderFilter}\"]"));
            var url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(id)}/version?" + string.Join("&", query);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await context.Http.GetAsync(url, cts.Token);
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
                folder = Path.Combine(context.Store.DataDirectory, "downloads");
            else
            {
                var instance = Path.Combine(context.Config.Root, "versions", catalog.Selected);
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
            var bytes = await context.Http.GetByteArrayAsync(fileUrl);
            var path = Path.Combine(folder, ContentService.SafeFile(fileName));
            await File.WriteAllBytesAsync(path, bytes);
            context.Send(new
            {
                type = "toast",
                message = $"已下载：{path}"
            });
            context.RefreshState();
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "toast",
                message = "下载失败：" + ex.Message
            });
        }
    }

    public async Task InstallModAsync(JsonElement root)
    {
        var id = ReadString(root, "id");
        var mode = ReadString(root, "mode");
        var path = ReadString(root, "path");
        var catalog = VersionCatalog.Load(context.Config.Root, context.Config.LastInstance);
        var target = mode == "local"
            ? path
            : Path.Combine(VersionCatalog.InstanceDirectory(context.Config.Root, catalog.Selected), "mods");
        if (string.IsNullOrWhiteSpace(target))
        {
            context.Send(new
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
            var details = VersionCatalog.ReadDetails(context.Config.Root, catalog.Selected);
            var minecraft = details.Minecraft;
            var loader = details.Loader.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(minecraft) || loader is not ("fabric" or "forge" or "neoforge" or "quilt"))
                throw new InvalidOperationException("请先选择已安装模组加载器的目标实例。");
            var file = await new ModService(context.Http).DownloadAsync(id, minecraft, target, context.Token, loader);
            context.Send(new
            {
                type = "modResult",
                ok = true,
                message = "已下载 " + Path.GetFileName(file)
            });
            context.RefreshState();
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "modResult",
                ok = false,
                message = ex.Message
            });
        }
    }
}
