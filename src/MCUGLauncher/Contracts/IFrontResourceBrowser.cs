namespace BlockLaunch.Contracts;

public sealed record FrontResourceQuery(string Text, string GameVersion, string Loader, string Kind, int Offset = 0, string Sort = "relevance", bool FavoritesOnly = false, string Channel = "");

public sealed record FrontResourceItem(string Id, string Title, string Description, string Author, long Downloads)
{
    public string IconUrl { get; init; } = "";
    public string Tags { get; init; } = "";
    public bool IsFavorite { get; init; }
    public string DisplayTitle => (IsFavorite ? "★ " : "") + Title;
    public string Meta => $"{Author}  ·  {Downloads:N0} 次下载 · Modrinth" + (Tags.Length > 0 ? "\n" + Tags : "");
}

public sealed record FrontResourcePage(IReadOnlyList<FrontResourceItem> Items, int Total);

public sealed record FrontResourceVersion(string Id, string Label)
{
    public string Facts { get; init; } = "";
    public string Dependencies { get; init; } = "";
    public string DependencySummary { get; init; } = "";
}
public sealed record FrontResourcePresentation(string Body, IReadOnlyList<string> Gallery, string Website);
public interface IFrontResourcePresentation
{
    Task<FrontResourcePresentation> ReadPresentation(string project, CancellationToken token);
}
public sealed record FrontResourceBookmark(string Labels, string Folder);

public interface IFrontResourceCollections
{
    FrontResourceBookmark? ReadBookmark(string projectId);
    void SaveBookmark(FrontResourceItem item, string kind, string labels, string folder);
    void RemoveBookmark(string projectId);
}

public interface IFrontResourceBrowser
{
    Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token);
    Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token);
}
