namespace BlockLaunch;

public sealed record ResourceSearchQuery(string Query, string GameVersion, string Loader, string Kind, int Offset = 0, string Sort = "relevance", string Channel = "");

public sealed record ResourceInstallTarget(string Root, string Version, string Directory, string GameVersion, string Loader);
