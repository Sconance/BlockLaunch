using System.Net.Http;
using System.Text.Json;

namespace MCUGLauncher.Services;

public sealed class ModService
{
    static readonly HttpClient DefaultHttp = new() { Timeout = TimeSpan.FromSeconds(60) };
    readonly HttpClient http;

    public ModService(HttpClient? http = null) => this.http = http ?? DefaultHttp;

    public async Task<string> DownloadAsync(string projectId, string gameVersion, string targetDirectory, CancellationToken token, string loader = "fabric")
    {
        if (string.IsNullOrWhiteSpace(gameVersion) || loader is not ("fabric" or "forge" or "neoforge" or "quilt"))
            throw new ArgumentException("缺少有效的游戏版本或模组加载器。 ");
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("缺少模组标识。");

        var uri = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}/version" +
                  $"?game_versions=%5B%22{Uri.EscapeDataString(gameVersion)}%22%5D&loaders=%5B%22{Uri.EscapeDataString(loader)}%22%5D";
        using var response = await http.GetAsync(uri, token);
        if (!response.IsSuccessStatusCode)
            throw new IOException($"模组源返回 HTTP {(int)response.StatusCode}");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            throw new IOException("没有找到适配当前游戏版本的模组文件。");

        var version = doc.RootElement[0];
        if (!version.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array || files.GetArrayLength() == 0)
            throw new IOException("模组源没有返回文件。");
        var file = files.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("primary", out var primary) && primary.ValueKind == JsonValueKind.True);
        if (file.ValueKind != JsonValueKind.Object)
            file = files[0];
        if (!file.TryGetProperty("url", out var urlValue) || urlValue.GetString() is not { Length: > 0 } url)
            throw new IOException("模组文件地址为空。");
        var fileName = file.TryGetProperty("filename", out var nameValue) && nameValue.GetString() is { Length: > 0 } name
            ? name
            : Path.GetFileName(new Uri(url).AbsolutePath);
        if (!fileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
            fileName += ".jar";

        if (Path.GetFileName(fileName) != fileName || fileName.Contains('/') || fileName.Contains('\\'))
            throw new IOException("模组源返回了无效的文件名。");
        Directory.CreateDirectory(targetDirectory);
        var path = Path.Combine(targetDirectory, fileName);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".part";
        try
        {
            await using (var source = await http.GetStreamAsync(url, token))
            await using (var destination = File.Create(temp))
                await source.CopyToAsync(destination, token);
            File.Move(temp, path, true);
            return path;
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }
}
