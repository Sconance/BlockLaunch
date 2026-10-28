namespace BlockLaunch;

public record VersionRow(string Id, string Label, string Type, DateTimeOffset? Published = null);

public record InstanceTarget(string Version, string GameVersion, string Loader);

public record VersionListSnapshot(string Root, List<VersionRow> Versions, HashSet<string> LocalVersions, string? OnlineError);
