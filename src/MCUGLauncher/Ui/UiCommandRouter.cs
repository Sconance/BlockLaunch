using System.Text.Json;

namespace MCUGLauncher.Ui;

/// <summary>
/// WebView2 消息路由：按 <c>type</c> 把前端命令分发到注册的处理器。
/// 主窗口只负责注册命令，不再维护一大段 <c>switch</c>。
/// </summary>
sealed class UiCommandRouter
{
    readonly Dictionary<string, Func<JsonElement, Task>> _routes = new(StringComparer.Ordinal);

    /// <summary>注册异步命令。</summary>
    public void Register(string type, Func<JsonElement, Task> handler) => _routes[type] = handler;

    /// <summary>注册同步命令。</summary>
    public void Register(string type, Action<JsonElement> handler) =>
        Register(type, root =>
        {
            handler(root);
            return Task.CompletedTask;
        });

    /// <summary>分发一条消息；未注册的 type 会被忽略。</summary>
    public async Task DispatchAsync(JsonElement root)
    {
        var type = UiBridge.ReadString(root, "type");
        if (_routes.TryGetValue(type, out var handler))
            await handler(root);
    }
}
