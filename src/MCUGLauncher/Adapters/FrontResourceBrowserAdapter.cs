using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class FrontResourceBrowserAdapter(ResourceManagementService resources, ResourceBookmarkStore? bookmarks = null) : IFrontResourceBrowser, IFrontResourceCollections
{
    public async Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token)
    {
        var page = query.FavoritesOnly
            ? await resources.SearchFavorites(Store.Query(query.Text, kind: query.Kind).Select(item => item.Project), Convert(query), token)
            : await resources.Search(Convert(query), token);
        return new(page.Items.Select(item => new FrontResourceItem(item.Id, item.Title, item.Description, item.Author, item.Downloads)
            { IconUrl = item.IconUrl, Tags = item.Tags }).ToList(), page.Total);
    }

    public async Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token)
    {
        var versions = await resources.Versions(project, Convert(query), token);
        return versions.Select(version => new FrontResourceVersion(version.Id, version.Label)).ToList();
    }

    static ResourceSearchQuery Convert(FrontResourceQuery query) =>
        new(query.Text, query.GameVersion, query.Loader, query.Kind, query.Offset, query.Sort);

    ResourceBookmarkStore Store => bookmarks ?? throw new InvalidOperationException("当前宿主未配置资源收藏存储。");

    public FrontResourceBookmark? ReadBookmark(string projectId)
    {
        var item = Store.Read().SingleOrDefault(value => value.Project.Id == projectId);
        return item == null ? null : new(string.Join(", ", item.Labels), item.Folder);
    }

    public void SaveBookmark(FrontResourceItem item, string kind, string labels, string folder) => Store.Save(
        new(item.Id, "", item.Title, item.Description, item.Author, item.Downloads) { IconUrl = item.IconUrl, Tags = item.Tags }, kind, labels, folder);

    public void RemoveBookmark(string projectId) => Store.Remove(projectId);
}
