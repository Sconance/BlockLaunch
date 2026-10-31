using MCUGLauncher.Models;
using MCUGLauncher.Services;

namespace MCUGLauncher.Ui;

/// <summary>
/// 社区数据聚合。
/// </summary>
/// <remarks>
/// 依赖 <see cref="CommunityService"/>、<see cref="ConfigStore"/>、<see cref="AppConfig"/> 与 <see cref="UiBridge"/>；网络请求异步执行，本地文件优先。
/// </remarks>
sealed class CommunityFeed(CommunityService service, ConfigStore store, AppConfig config, UiBridge bridge)
{
    /// <summary>
    /// 读取并推送社区数据。
    /// </summary>
    /// <remarks>
    /// 先读本地 announcements/recommendations，缺失的再从配置的数据源拉取；全部为空时不发送消息。
    /// </remarks>
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
