using System.Net.Http;
using System.Text.Json;

namespace MCUGLauncher.Services;

/// <summary>
/// Modrinth 模组下载。
/// </summary>
/// <remarks>
/// 依赖 <see cref="System.Net.Http.HttpClient"/>；按项目 id 与游戏版本选择文件并写入目标目录。
/// </remarks>
public sealed class ModService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    /// <summary>
    /// 下载模组文件。
    /// </summary>
    /// <param name="projectId">Modrinth 项目 id</param>
    /// <param name="gameVersion">目标 Minecraft 版本</param>
    /// <param name="targetDirectory">保存目录</param>
    /// <param name="token">取消令牌</param>
    /// <returns>下载文件的完整路径</returns>
    /// <remarks>
    /// 返回写入后的完整路径；网络或文件错误以异常抛出。
    /// </remarks>
    public async Task<string> DownloadAsync(string projectId, string gameVersion, string targetDirectory, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("缺少模组标识。");

        var uri = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}/version" +
                  $"?game_versions=%5B%22{Uri.EscapeDataString(gameVersion)}%22%5D&loaders=%5B%22fabric%22%5D";
        using var response = await Http.GetAsync(uri, token);
        if (!response.IsSuccessStatusCode)
            throw new IOException($"模组源返回 HTTP {(int)response.StatusCode}");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            throw new IOException("没有找到适配当前游戏版本的模组文件。");

        var version = doc.RootElement[0];
        if (!version.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
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

        var path = Path.Combine(targetDirectory, fileName);
        var temp = path + ".part";
        await using (var source = await Http.GetStreamAsync(url, token))
        await using (var destination = File.Create(temp))
            await source.CopyToAsync(destination, token);
        File.Move(temp, path, true);
        return path;
    }
}
