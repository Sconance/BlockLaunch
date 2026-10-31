using MCUGLauncher.Models;
using MCUGLauncher.Services;

namespace MCUGLauncher.Ui;

/// <summary>
/// 社区数据聚合：优先读取本地已保存的公告/推荐，其余从配置的数据源拉取后推给前端。
/// </summary>
sealed class CommunityFeed(CommunityService service, ConfigStore store, AppConfig config, UiBridge bridge)
{
    /// <summary>
    /// 发送。
    /// </summary>
    public async Task SendAsync()
    {
        var items = new Dictionary<string, string>();
        var localAnnouncements = Path.Combine(store.DataDirectory, "announcements.local.json");
        if (File.Exists(localAnnouncements))
            items["announcements"] = File.ReadAllText(localAnnouncements);
        var localRecommendations = Path.Combine(store.DataDirectory, "recommendations.local.json");
        if (File.Exists(localRecommendations))
            items["recommendations"] = File.ReadAllText(localRecommendations);

        var jobs = new[]
        {
            ("announcements", config.DataSources.Announcements),
            ("recommendations", config.DataSources.Recommendations),
            ("quotes", config.DataSources.Quotes)
        };
        foreach (var (name, url) in jobs)
        {
            if (items.ContainsKey(name))
                continue;
            var text = await service.ReadAsync(url, CancellationToken.None);
            if (text != null)
                items[name] = text;
        }

        if (items.Count > 0)
            bridge.Send(new
            {
                type = "community",
                payload = items
            });
    }
}
