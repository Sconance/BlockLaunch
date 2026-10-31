using System.Diagnostics;
using System.Net.Http;

namespace MCUGLauncher.Services;

/// <summary>
/// 提供 社区Service 相关功能。
/// </summary>
public sealed class CommunityService
{
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public async Task<(bool Ok, string Text, int Ms)> TestAsync(string url, CancellationToken token)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return (false, "地址需以 https:// 或 http:// 开头", 0);

        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
            watch.Stop();
            return response.IsSuccessStatusCode
                ? (true, $"连接正常 · {watch.ElapsedMilliseconds} ms", (int)watch.ElapsedMilliseconds)
                : (false, $"HTTP {(int)response.StatusCode} · {response.ReasonPhrase}", (int)watch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            return (false, "连接超时（8s）", 8000);
        }
        catch (HttpRequestException ex)
        {
            return (false, ex.Message, (int)watch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 读取。
    /// </summary>
    public async Task<string?> ReadAsync(string url, CancellationToken token)
    {
        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadAsStringAsync(token);
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            return null;
        }
    }
}
