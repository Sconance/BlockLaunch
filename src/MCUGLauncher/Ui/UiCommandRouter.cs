using System.Text.Json;

namespace MCUGLauncher.Ui;

/// <summary>
/// WebView2 消息路由。
/// </summary>
/// <remarks>
/// 依赖 <c>UiBridge.ReadString</c> 解析 type；处理器由 MainWindow.ConfigureCommands 注册，本身不持有窗口或服务引用。
/// </remarks>
sealed class UiCommandRouter
{
    readonly Dictionary<string, Func<JsonElement, Task>> _routes = new(StringComparer.Ordinal);

    /// <summary>
    /// 注册一条命令处理器。
    /// </summary>
    /// <remarks>
    /// 同名 type 后注册的覆盖先注册的；同步处理器会自动包装为 Task。
    /// </remarks>
    public void Register(string type, Func<JsonElement, Task> handler) => _routes[type] = handler;

    /// <summary>注册同步命令。</summary>
    public void Register(string type, Action<JsonElement> handler) =>
        Register(type, root =>
        {
            handler(root);
            return Task.CompletedTask;
        });

    /// <summary>
    /// 按 type 分发一条消息。
    /// </summary>
    /// <param name="root">完整的消息 JSON 根元素</param>
    /// <remarks>
    /// 未注册的 type 静默忽略；处理器异常由调用方（OnWebMessage）统一回推 error。
    /// </remarks>
    public async Task DispatchAsync(JsonElement root)
    {
        var type = UiBridge.ReadString(root, "type");
        if (_routes.TryGetValue(type, out var handler))
            await handler(root);
    }
}
