namespace BlockLaunch.Contracts;

public sealed record FrontResourceQuery(string Text, string GameVersion, string Loader, string Kind, int Offset = 0, string Sort = "relevance");

public sealed record FrontResourceItem(string Id, string Title, string Description, string Author, long Downloads);

public sealed record FrontResourcePage(IReadOnlyList<FrontResourceItem> Items, int Total);

public sealed record FrontResourceVersion(string Id, string Label);

public interface IFrontResourceBrowser
{
    Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token);
    Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token);
}
