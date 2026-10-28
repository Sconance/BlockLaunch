using BlockLaunch.Contracts;

internal sealed class DemoResourceBrowser : IFrontResourceBrowser
{
    public async Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token)
    {
        await Task.Delay(450, token);
        if (query.Text.Equals("error", StringComparison.OrdinalIgnoreCase))
            throw new IOException("模拟网络错误；修改关键词后重试。");
        var projects = Enumerable.Range(1, 32).Select(index => new FrontResourceItem(
            "demo-" + index,
            $"示例{query.Kind}项目 {index:00}",
            "模拟数据，用于验证资源卡片、筛选、分页和详情读取，不对应真实下载内容。",
            "Preview Author", 100000 - index * 1200L));
        var filtered = projects.Where(project => query.Text.Length == 0 || project.Title.Contains(query.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (query.Sort == "updated")
            filtered.Reverse();
        return new(filtered.Skip(query.Offset).Take(15).ToList(), filtered.Count);
    }

    public async Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token)
    {
        await Task.Delay(300, token);
        return new[]
        {
            new FrontResourceVersion(project + "-stable", $"1.0.0 · release · {query.GameVersion} {query.Loader}（模拟）"),
            new FrontResourceVersion(project + "-beta", "1.1.0 · beta（模拟）")
        };
    }
}
