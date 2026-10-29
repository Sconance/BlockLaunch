namespace BlockLaunch;

/// <summary>Loads online metadata on demand. Local startup never waits for this request.</summary>
internal sealed class VersionCatalogController(
    Func<string> currentRoot, Func<CancellationToken, Task<List<VersionRow>>> fetch,
    Action<List<VersionRow>> apply, Action changed, Action<string> log, TimeSpan? timeout = null) : IDisposable
{
    CancellationTokenSource? cancellation;
    Task? pending;
    string? requestRoot;
    string? loadedRoot;
    internal bool CatalogLoading
    {
        get; private set;
    }
    internal string? CatalogError
    {
        get; private set;
    }

    internal Task EnsureVersionCatalog(bool force = false)
    {
        var root = currentRoot();
        if (pending is { IsCompleted: false } && requestRoot == root)
            return pending;
        if (!force && loadedRoot == root)
            return Task.CompletedTask;
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(12));
        requestRoot = root;
        pending = Load(root, cancellation);
        return pending;
    }

    async Task Load(string root, CancellationTokenSource request)
    {
        CatalogLoading = true;
        CatalogError = null;
        changed();
        try
        {
            var token = request.Token;
            var rows = await fetch(token).WaitAsync(token);
            request.Token.ThrowIfCancellationRequested();
            if (currentRoot() != root || cancellation != request)
                return;
            apply(rows);
            loadedRoot = root;
        }
        catch (Exception ex)
        {
            if (currentRoot() != root || cancellation != request)
                return;
            CatalogError = ex is OperationCanceledException ? "读取超时，可点击重试；本地游戏仍可启动。" : "在线版本读取失败，可点击重试；本地游戏仍可启动。";
            log(CatalogError + " " + ex.GetType().Name);
        }
        finally
        {
            if (cancellation == request)
            {
                CatalogLoading = false;
                changed();
            }
        }
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }
}
