namespace BlockLaunch;

/// <summary>Immutable UI selection snapshot; no controls or live workspace references.</summary>
internal sealed record ResourceInstallSelection(string Root, string Key, string Directory,
    PackRequirements Requirements, string Kind);
