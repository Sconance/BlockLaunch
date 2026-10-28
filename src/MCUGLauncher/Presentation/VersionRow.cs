namespace BlockLaunch;

public record VersionRow(string Id, string Label, string Type, DateTimeOffset? Published = null);
