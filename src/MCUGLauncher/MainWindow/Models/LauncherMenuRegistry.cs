using System.Windows;
using System.Windows.Media;

namespace BlockLaunch;

public enum LauncherMenuArea
{
    Top,
    Download,
    Settings,
    More,
    ResourceDetails,
    Home,
    VersionSettings,
    InstanceContent
}

public sealed record LauncherMenuContext(MainWindow Window, string? InstanceId = null, ContentProject? Resource = null);

public sealed record LauncherMenuPage(string Id, string Title, Func<LauncherMenuContext, FrameworkElement> CreateContent)
{
    public string Glyph { get; init; } = "◇";
    public ImageSource? Icon
    {
        get; init;
    }
    public int Order { get; init; } = 100;
}

public sealed class LauncherMenuRegistry
{
    readonly Dictionary<LauncherMenuArea, List<LauncherMenuPage>> pages = new();

    public event Action<LauncherMenuArea>? Changed;

    public IReadOnlyList<LauncherMenuPage> Pages(LauncherMenuArea area) =>
        pages.TryGetValue(area, out var items)
            ? items.OrderBy(page => page.Order).ThenBy(page => page.Title, StringComparer.Ordinal).ToArray()
            : [];

    public void Register(LauncherMenuArea area, LauncherMenuPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (!Enum.IsDefined(typeof(LauncherMenuArea), area))
            throw new ArgumentOutOfRangeException(nameof(area));
        if (string.IsNullOrWhiteSpace(page.Id) || string.IsNullOrWhiteSpace(page.Title) || page.CreateContent == null)
            throw new ArgumentException("菜单需要唯一 ID、显示名称和页面工厂。", nameof(page));
        if (!pages.TryGetValue(area, out var items))
            pages[area] = items = [];
        if (items.Any(existing => string.Equals(existing.Id, page.Id, StringComparison.Ordinal)))
            throw new ArgumentException("此菜单区域已存在同名 ID：" + page.Id, nameof(page));
        items.Add(page);
        Changed?.Invoke(area);
    }
}
