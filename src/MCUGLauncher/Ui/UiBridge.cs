using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

namespace MCUGLauncher.Ui;

/// <summary>
/// C# 到 WebView2 的消息出口。
/// </summary>
/// <remarks>
/// 依赖 <see cref="Microsoft.Web.WebView2.Wpf.WebView2"/> 与 <see cref="System.Windows.Threading.Dispatcher"/>；Send 可在任意线程调用，内部切回 UI 线程。
/// </remarks>
sealed class UiBridge(WebView2 browser, Dispatcher dispatcher)
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 序列化并发送一条消息给前端。
    /// </summary>
    /// <param name="payload">任意可被 System.Text.Json 序列化的对象</param>
    /// <remarks>
    /// 使用 camelCase；WebView2 尚未初始化时静默丢弃；payload 为匿名对象或记录。
    /// </remarks>
    public void Send(object payload)
    {
        if (!dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => Send(payload));
            return;
        }
        browser.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(payload, Options));
    }

    /// <summary>
    /// 读取消息中的字符串字段。
    /// </summary>
    /// <param name="root">消息 JSON 根元素</param>
    /// <param name="name">字段名</param>
    /// <returns>字段值；缺失或非字符串时为空串</returns>
    /// <remarks>
    /// 字段缺失或类型不是字符串时返回空串。
    /// </remarks>
    public static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
