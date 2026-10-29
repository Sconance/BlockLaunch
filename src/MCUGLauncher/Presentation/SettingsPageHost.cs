namespace BlockLaunch;

/// <summary>Lazy page ownership, separate from navigation selection and business operations.</summary>
internal sealed class SettingsPageHost(SettingsNavigationView view) : IDisposable
{
    readonly Dictionary<string, Func<SettingsPageContent>> factories = new();
    SettingsPageContent? current;
    string? selected;
    string? selectedContext;
    internal void Register(string id, Func<SettingsPageContent> factory) => factories[id] = factory;

    internal void Show(string id, string context = "")
    {
        if (selected == id && selectedContext == context)
            return;
        view.ShowContent(null);
        current?.Dispose();
        current = null;
        selected = id;
        selectedContext = context;
        try
        {
            current = factories[id]();
        }
        catch (Exception ex) { current = new(FeatureUiElements.Hint("无法打开设置栏目：" + ex.Message)); }
        view.ShowContent(current.Content);
    }

    public void Dispose()
    {
        view.ShowContent(null);
        current?.Dispose();
        current = null;
        selected = null;
        selectedContext = null;
    }
}
