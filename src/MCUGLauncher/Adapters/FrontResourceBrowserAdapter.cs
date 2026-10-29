using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class FrontResourceBrowserAdapter(ResourceManagementService resources, ResourceBookmarkStore? bookmarks = null) : IFrontResourceBrowser, IFrontResourceCollections, IFrontResourcePresentation
{
    public async Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token)
    {
        var page = query.FavoritesOnly
            ? await resources.SearchFavorites(Store.Query(query.Text, kind: query.Kind).Select(item => item.Project), Convert(query), token)
            : await resources.Search(Convert(query), token);
        var saved = bookmarks?.Read().Select(item => item.Project.Id).ToHashSet() ?? [];
        var items = page.Items.Select(item => new FrontResourceItem(item.Id, item.Title, item.Description, item.Author, item.Downloads)
            { IconUrl = item.IconUrl, Tags = item.Tags, IsFavorite = saved.Contains(item.Id) });
        return new((query.Sort == "favorites" ? items.OrderByDescending(item => item.IsFavorite) : items).ToList(), page.Total);
    }

    public async Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token)
    {
        var versions = await resources.Versions(project, Convert(query), token);
        return versions.Select(version => new FrontResourceVersion(version.Id, version.Label)
        {
            Facts = $"游戏版本：{string.Join("、", version.GameVersions)}\n加载器：{string.Join("、", version.Loaders)}\n发布时间：{version.DatePublished.ToLocalTime():yyyy/MM/dd HH:mm}\n文件大小：{version.Files.Sum(file => file.Size) / 1048576d:0.##} MB",
            DependencySummary = $"必需依赖：{version.Dependencies.Count(item => item.DependencyType == "required")} 项\n可选依赖：{version.Dependencies.Count(item => item.DependencyType == "optional")} 项\n\n独立文件下载不自动安装依赖。使用实例内安装功能时解析必需依赖。",
            Dependencies = version.Dependencies.Count == 0 ? "所选版本没有声明依赖。" : string.Join("\n\n", version.Dependencies.Select(item =>
                (item.DependencyType == "required" ? "必需" : item.DependencyType == "optional" ? "可选" : item.DependencyType)
                    + " · " + (item.ProjectId ?? item.VersionId ?? "未提供项目标识")))
        }).ToList();
    }

    public Task<FrontResourcePresentation> ReadPresentation(string project, CancellationToken token) => resources.ReadPresentation(project, token);

    static ResourceSearchQuery Convert(FrontResourceQuery query) =>
        new(query.Text, query.GameVersion, query.Loader, query.Kind, query.Offset, query.Sort == "favorites" ? "relevance" : query.Sort, query.Channel);

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
