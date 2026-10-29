using System.Windows;

namespace BlockLaunch;

/// <summary>Owns an embedded tool page's content and its cancellation/event lifetime.</summary>
internal sealed class SettingsPageContent(UIElement content, Action? cleanup = null) : IDisposable
{
    internal UIElement Content { get; } = content;
    bool disposed;
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        cleanup?.Invoke();
    }

}
