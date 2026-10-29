using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class FrontResourceBrowserAdapter(ResourceManagementService resources) : IFrontResourceBrowser
{
    public async Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token)
    {
        var page = await resources.Search(Convert(query), token);
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
}
