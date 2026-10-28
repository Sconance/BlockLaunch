namespace BlockLaunch;

/// <summary>Per-dialog data and presentation lifetime, without a window or workspace service locator.</summary>
internal sealed record VersionSettingsPageContext(
    string Key,
    string Root,
    InstanceCatalog Catalog,
    InstanceDescriptor Descriptor,
    PackRequirements Requirements,
    VersionSettingsView View,
    Func<string> DataDirectory,
    Action<string> Render);
