using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public sealed class ResourceBrowserViewModel(IFrontResourceBrowser browser, IFrontResourceDownloads? downloads = null, IFrontResourceInstaller? installer = null) : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;
    CancellationTokenSource? searchCancellation;
    CancellationTokenSource? detailCancellation;
    FrontResourceQuery? currentQuery;
    bool disposed;
    string queryText = "";
    string gameVersion = "跟随实例";
    string loader = "跟随实例";
    string kind = "mod";
    string sort = "relevance";
    string channel = "";
    string source = "modrinth";
    bool favoritesOnly;
    string favoriteLabels = "";
    string favoriteFolder = "默认收藏夹";
    string favoriteStatus = "";
    string projectBody = "";
    string projectWebsite = "";
    IReadOnlyList<string> gallery = [];
    string status = "输入关键词搜索，或直接浏览资源。";
    string detailStatus = "选择项目查看兼容版本。";
    bool isBusy;
    bool isDetailBusy;
    bool isDownloading;
    FrontResourceVersion? selectedVersion;
    string destination = "";
    string downloadStatus = "资源下载到游戏目录之外，不自动安装到实例。";
    readonly CancellationTokenSource lifetime = new();
    int total;
    int offset;
    FrontResourceItem? selected;
    IReadOnlyList<FrontResourceItem> items = [];
    IReadOnlyList<FrontResourceVersion> versions = [];
    IReadOnlyList<FrontInstallTarget> targets = [];
    FrontInstallTarget? selectedTarget;

    public string QueryText { get => queryText; set => Set(ref queryText, value); }
    public string GameVersion { get => gameVersion; set => Set(ref gameVersion, value); }
    public string Loader { get => loader; set => Set(ref loader, value); }
    public string Kind { get => kind; set => Set(ref kind, value); }
    public string Sort { get => sort; set => Set(ref sort, value); }
    public string Channel { get => channel; set => Set(ref channel, value); }
    public string Source { get => source; set => Set(ref source, value); }
    public Func<FrontInstance?>? CurrentInstance { get; set; }
    public bool FavoritesOnly { get => favoritesOnly; set => Set(ref favoritesOnly, value); }
    public string FavoriteLabels { get => favoriteLabels; set => Set(ref favoriteLabels, value); }
    public string FavoriteFolder { get => favoriteFolder; set => Set(ref favoriteFolder, value); }
    public string FavoriteStatus { get => favoriteStatus; private set => Set(ref favoriteStatus, value); }
    public string ProjectBody { get => projectBody; private set => Set(ref projectBody, value); }
    public string ProjectWebsite { get => projectWebsite; private set => Set(ref projectWebsite, value); }
    public IReadOnlyList<string> Gallery { get => gallery; private set => Set(ref gallery, value); }
    public string Status { get => status; private set => Set(ref status, value); }
    public string DetailStatus { get => detailStatus; private set => Set(ref detailStatus, value); }
    public bool IsBusy { get => isBusy; private set => Set(ref isBusy, value); }
    public bool IsDetailBusy { get => isDetailBusy; private set => Set(ref isDetailBusy, value); }
    public IReadOnlyList<FrontResourceItem> Items { get => items; private set => Set(ref items, value); }
    public IReadOnlyList<FrontResourceVersion> Versions { get => versions; private set => Set(ref versions, value); }
    public IReadOnlyList<FrontInstallTarget> Targets { get => targets; private set => Set(ref targets, value); }
    public FrontInstallTarget? SelectedTarget { get => selectedTarget; set => Set(ref selectedTarget, value); }
    public FrontResourceItem? Selected { get => selected; private set => Set(ref selected, value); }
    public FrontResourceVersion? SelectedVersion { get => selectedVersion; set => Set(ref selectedVersion, value); }
    public string Destination { get => destination; set => Set(ref destination, value); }
    public string DownloadStatus { get => downloadStatus; private set => Set(ref downloadStatus, value); }
    public bool CanDownload => downloads != null && Selected != null && SelectedVersion != null && currentQuery != null && !IsBusy && !IsDetailBusy && !isDownloading && !disposed;
    public bool CanInstall => installer != null && Selected != null && SelectedVersion != null && SelectedTarget != null && currentQuery != null && !IsBusy && !IsDetailBusy && !isDownloading && !disposed;
    public bool CanPrevious => !IsBusy && currentQuery != null && offset > 0;
    public bool CanNext => !IsBusy && currentQuery != null && Items.Count > 0 && offset + Items.Count < total;

    public Task Search()
    {
        if (disposed)
            return Task.CompletedTask;
        if (Source != "modrinth")
        {
            Cancel();
            currentQuery = null;
            Items = [];
            total = 0;
            Status = "CurseForge 需要官方 API Key，当前未启用。请使用 Modrinth 或手动下载后导入。";
            NotifyPaging();
            return Task.CompletedTask;
        }
        var game = GameVersion.Trim();
        var selectedLoader = Loader.Trim();
        FrontInstance? instance;
        try { instance = game == "跟随实例" || selectedLoader == "跟随实例" ? CurrentInstance?.Invoke() : null; }
        catch (Exception exception)
        {
            Cancel();
            Status = "无法读取当前实例，请刷新实例列表或将筛选改为全部：" + exception.Message;
            return Task.CompletedTask;
        }
        game = game == "跟随实例" ? instance?.Minecraft ?? "" : game is "全部" ? "" : game;
        selectedLoader = selectedLoader == "跟随实例" ? instance?.Loader ?? "" : selectedLoader is "全部加载器" or "全部" ? "" : selectedLoader;
        if (selectedLoader == "vanilla")
            selectedLoader = "";
        return Load(new(QueryText.Trim(), game, selectedLoader, Kind, Sort: Sort, FavoritesOnly: FavoritesOnly, Channel: Channel));
    }

    public Task Previous() => CanPrevious ? Load(currentQuery! with { Offset = Math.Max(0, offset - 15) }) : Task.CompletedTask;

    public Task Next() => CanNext ? Load(currentQuery! with { Offset = offset + 15 }) : Task.CompletedTask;

    async Task Load(FrontResourceQuery query)
    {
        if (disposed)
            return;
        searchCancellation?.Cancel();
        CancelDetails();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        searchCancellation = cancellation;
        IsBusy = true;
        currentQuery = null;
        Selected = null;
        Items = [];
        Versions = [];
        SelectedVersion = null;
        total = 0;
        offset = query.Offset;
        Status = "正在搜索 Modrinth…";
        DetailStatus = "选择项目查看兼容版本。";
        NotifyPaging();
        try
        {
            var page = await browser.Search(query, cancellation.Token);
            if (disposed || !ReferenceEquals(searchCancellation, cancellation))
                return;
            cancellation.Token.ThrowIfCancellationRequested();
            currentQuery = query;
            Items = page.Items;
            total = page.Total;
            Status = Items.Count == 0 ? "没有匹配的资源，请调整关键词或筛选条件。" : $"{offset + 1}–{offset + Items.Count} / {total:N0} 个项目";
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(searchCancellation, cancellation) && !disposed)
                Status = "搜索已取消或超时，可以重新搜索。";
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(searchCancellation, cancellation) && !disposed)
                Status = "搜索失败：" + exception.Message;
        }
        finally
        {
            if (ReferenceEquals(searchCancellation, cancellation))
            {
                searchCancellation = null;
                IsBusy = false;
                NotifyPaging();
            }
            cancellation.Dispose();
        }
    }

    public async Task Select(FrontResourceItem? item)
    {
        CancelDetails();
        Selected = item;
        Versions = [];
        SelectedVersion = null;
        DetailStatus = "选择项目查看兼容版本。";
        ProjectBody = ProjectWebsite = "";
        Gallery = [];
        if (disposed || item == null || currentQuery == null)
            return;
        var query = currentQuery;
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        detailCancellation = cancellation;
        IsDetailBusy = true;
        DetailStatus = "正在读取兼容版本…";
        try
        {
            if (browser is IFrontResourceCollections collections)
            {
                var bookmark = collections.ReadBookmark(item.Id);
                FavoriteLabels = bookmark?.Labels ?? "";
                FavoriteFolder = bookmark?.Folder ?? "默认收藏夹";
                FavoriteStatus = bookmark == null ? "尚未收藏" : "已收藏";
            }
            if (installer != null)
            {
                var available = await installer.ReadResourceTargets(query.Kind, cancellation.Token);
                if (disposed || !ReferenceEquals(detailCancellation, cancellation))
                    return;
                Targets = available;
                SelectedTarget = Targets.FirstOrDefault();
            }
            var result = await browser.Versions(item.Id, query, cancellation.Token);
            if (disposed || !ReferenceEquals(detailCancellation, cancellation))
                return;
            cancellation.Token.ThrowIfCancellationRequested();
            Versions = result;
            SelectedVersion = result.FirstOrDefault();
            DetailStatus = result.Count == 0 ? "没有符合条件的兼容版本。" : $"{result.Count} 个兼容版本";
            if (browser is IFrontResourcePresentation presentation)
                try
                {
                    var content = await presentation.ReadPresentation(item.Id, cancellation.Token);
                    if (!disposed && ReferenceEquals(detailCancellation, cancellation))
                    {
                        ProjectBody = content.Body;
                        ProjectWebsite = content.Website;
                        Gallery = content.Gallery;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception) { if (!disposed && ReferenceEquals(detailCancellation, cancellation)) ProjectBody = "项目介绍暂不可用，仍可选择版本和下载。"; }
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(detailCancellation, cancellation) && !disposed)
                DetailStatus = "读取已取消或超时。";
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(detailCancellation, cancellation) && !disposed)
                DetailStatus = "读取失败：" + exception.Message;
        }
        finally
        {
            if (ReferenceEquals(detailCancellation, cancellation))
            {
                detailCancellation = null;
                IsDetailBusy = false;
            }
            cancellation.Dispose();
        }
    }

    public void Cancel()
    {
        searchCancellation?.Cancel();
        detailCancellation?.Cancel();
    }

    public async Task DownloadSelected()
    {
        if (!CanDownload)
            return;
        var project = Selected!.Id;
        var version = SelectedVersion!.Id;
        var query = currentQuery!;
        var target = Destination.Trim();
        try
        {
            isDownloading = true;
            NotifyActions();
            await downloads!.EnqueueResource(project, version, query, target, lifetime.Token);
            if (!disposed)
                DownloadStatus = "已加入真实下载队列。";
        }
        catch (OperationCanceledException)
        {
            if (!disposed)
                DownloadStatus = "加入队列已取消。";
        }
        catch (Exception exception)
        {
            if (!disposed)
                DownloadStatus = "加入队列失败：" + exception.Message;
        }
        finally
        {
            isDownloading = false;
            NotifyActions();
        }
    }

    public async Task InstallSelected(Func<string, bool> confirm)
    {
        if (!CanInstall || installer == null)
            return;
        try
        {
            isDownloading = true;
            NotifyActions();
            var enqueued = await installer.EnqueueResourceInstallation(Selected!.Id, SelectedVersion!.Id, currentQuery!, SelectedTarget!.Id, SelectedTarget.World, confirm, lifetime.Token);
            if (!disposed)
                DownloadStatus = enqueued ? "已加入实例安装队列。" : "已取消安装。";
        }
        catch (OperationCanceledException)
        {
            if (!disposed)
                DownloadStatus = "加入安装队列已取消。";
        }
        catch (Exception exception)
        {
            if (!disposed)
                DownloadStatus = "加入安装队列失败：" + exception.Message;
        }
        finally
        {
            isDownloading = false;
            NotifyActions();
        }
    }

    public void SaveFavorite(bool remove)
    {
        try
        {
            if (disposed || Selected == null || currentQuery == null || browser is not IFrontResourceCollections collections)
                throw new InvalidOperationException("请选择支持收藏的资源。");
            if (remove)
                collections.RemoveBookmark(Selected.Id);
            else
                collections.SaveBookmark(Selected, currentQuery.Kind, FavoriteLabels, FavoriteFolder);
            FavoriteStatus = remove ? "已取消收藏；仅收藏列表可重新搜索刷新。" : "收藏与标签已保存。";
            Items = Items.Select(item => item.Id == Selected.Id ? item with { IsFavorite = !remove } : item).ToArray();
        }
        catch (Exception exception)
        {
            FavoriteStatus = "收藏操作失败：" + exception.Message;
        }
    }

    public void ToggleFavorite(FrontResourceItem item)
    {
        try
        {
            if (browser is not IFrontResourceCollections collections)
                return;
            var saved = collections.ReadBookmark(item.Id);
            if (saved == null)
                collections.SaveBookmark(item, Kind, "", "默认收藏夹");
            else
                collections.RemoveBookmark(item.Id);
            Items = Items.Where(value => !FavoritesOnly || value.Id != item.Id || saved == null)
                .Select(value => value.Id == item.Id ? value with { IsFavorite = saved == null } : value).ToArray();
            FavoriteStatus = saved == null ? "已收藏" : "已取消收藏";
        }
        catch (Exception exception) { Status = "收藏操作失败：" + exception.Message; }
    }

    void CancelDetails()
    {
        var cancellation = detailCancellation;
        detailCancellation = null;
        cancellation?.Cancel();
        IsDetailBusy = false;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        lifetime.Cancel();
        Cancel();
        lifetime.Dispose();
    }

    void NotifyPaging()
    {
        PropertyChanged?.Invoke(this, new(nameof(CanPrevious)));
        PropertyChanged?.Invoke(this, new(nameof(CanNext)));
    }

    void NotifyActions()
    {
        PropertyChanged?.Invoke(this, new(nameof(CanDownload)));
        PropertyChanged?.Invoke(this, new(nameof(CanInstall)));
    }

    void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new(property));
        if (property != nameof(CanDownload))
            NotifyActions();
    }
}
