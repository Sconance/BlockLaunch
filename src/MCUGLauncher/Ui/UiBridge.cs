using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

namespace MCUGLauncher.Ui;

/// <summary>
/// MainWindow 到 WebView2 的消息出口：统一 camelCase 序列化与 UI 线程切换。
/// </summary>
sealed class UiBridge(WebView2 browser, Dispatcher dispatcher)
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>把对象序列化后推给前端；可在任意线程调用。</summary>
    public void Send(object payload)
    {
        if (!dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => Send(payload));
            return;
        }
        browser.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(payload, Options));
    }

    /// <summary>读取消息里的字符串字段，缺失或类型不符时返回空串。</summary>
    public static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
