using BlockLaunch;
using BlockLaunch.Contracts;
using BlockLaunch.Front;
using System.Net;
using System.Text;

internal static class FrontResourceBrowserChecks
{
    public static async Task Run()
    {
        using (var handler = new Handler())
        using (var http = new HttpClient(handler))
        using (var model = new ResourceBrowserViewModel(new FrontResourceBrowserAdapter(new ResourceManagementService(http))))
        {
            model.QueryText = "fixture";
            model.GameVersion = "1.21.1";
            model.Loader = "fabric";
            await model.Search();
            Check(model.Items.Single().Id == "fixture" && !model.IsBusy && model.CanNext, "Front search uses existing resource service through contract adapter");
            model.GameVersion = "changed-filter";
            await model.Select(model.Items.Single());
            Check(model.Versions.Single().Id == "fixture-version" && handler.LastUri!.Query.Contains("1.21.1") && !handler.LastUri.Query.Contains("changed-filter"), "detail query uses displayed search snapshot, not edited filters");
        }

        var pending = new PendingBrowser();
        using (var model = new ResourceBrowserViewModel(pending))
        {
            model.QueryText = "old";
            var oldSearch = model.Search();
            Check(model.IsBusy && model.Items.Count == 0 && !model.CanNext, "loading clears stale rows and disables pagination");
            model.QueryText = "new";
            var newSearch = model.Search();
            pending.Searches[1].Result.SetResult(new([Item("new")], 31));
            await newSearch;
            pending.Searches[0].Result.SetResult(new([Item("old")], 1));
            await oldSearch;
            Check(model.Items.Single().Id == "new" && model.CanNext && !model.IsBusy, "late uncooperative search cannot overwrite newer results");
            model.QueryText = "edited";
            var next = model.Next();
            Check(pending.Searches[2].Query.Text == "new" && pending.Searches[2].Query.Offset == 15, "pagination retains submitted filters and fixed page offset");
            pending.Searches[2].Result.SetResult(new([Item("next")], 31));
            await next;
            Check(model.CanPrevious, "later page enables previous navigation");

            var firstDetail = model.Select(Item("first"));
            var secondDetail = model.Select(Item("second"));
            pending.Details[1].Result.SetResult([new("second-version", "Second")]);
            await secondDetail;
            pending.Details[0].Result.SetResult([new("first-version", "First")]);
            await firstDetail;
            Check(model.Selected!.Id == "second" && model.Versions.Single().Id == "second-version" && !model.IsDetailBusy, "late detail response cannot overwrite selected project");

            var deselected = model.Select(Item("third"));
            await model.Select(null);
            pending.Details[2].Result.SetResult([new("third-version", "Third")]);
            await deselected;
            Check(model.Selected == null && model.Versions.Count == 0 && !model.IsDetailBusy, "deselect cancels detail state without stale version rendering");

            var detailFailure = model.Select(Item("failed-detail"));
            pending.Details[3].Result.SetException(new IOException("detail fixture failure"));
            await detailFailure;
            Check(model.DetailStatus.Contains("detail fixture failure") && !model.IsDetailBusy && model.Versions.Count == 0, "detail errors clear loading state without showing stale versions");

            var cancelledDetail = model.Select(Item("cancelled-detail"));
            model.Cancel();
            pending.Details[4].Result.SetResult([new("cancelled-version", "Cancelled")]);
            await cancelledDetail;
            Check(model.DetailStatus.Contains("取消") && model.Versions.Count == 0 && !model.IsDetailBusy, "cancelled detail response is not rendered");

            var cancelled = model.Search();
            model.Cancel();
            pending.Searches[3].Result.SetResult(new([Item("cancelled")], 1));
            await cancelled;
            Check(!model.IsBusy && model.Items.Count == 0 && model.Status.Contains("取消"), "cancelled response cannot display results even when provider ignores cancellation");

            var failed = model.Search();
            pending.Searches[4].Result.SetException(new IOException("fixture failure"));
            await failed;
            Check(model.Status.Contains("fixture failure") && !model.IsBusy && !model.CanNext, "search error is displayed and can be retried");

            var empty = model.Search();
            pending.Searches[5].Result.SetResult(new([], 0));
            await empty;
            Check(model.Status.Contains("没有匹配") && model.Items.Count == 0, "empty results use explicit empty state");
            var disposed = model.Search();
            model.Dispose();
            pending.Searches[6].Result.SetResult(new([Item("disposed")], 1));
            await disposed;
            Check(model.Items.Count == 0, "closed Front does not render late search results");
        }

        using (var handler = new Handler())
        using (var http = new HttpClient(handler))
        {
            var downloads = new Downloads();
            using var model = new ResourceBrowserViewModel(new FrontResourceBrowserAdapter(new ResourceManagementService(http)), downloads);
            model.GameVersion = "1.21.1";
            await model.Search();
            await model.Select(model.Items.Single());
            model.SelectedVersion = model.Versions.Single();
            model.GameVersion = "edited-after-query";
            model.Destination = "fixture-output";
            Check(model.CanDownload, "download enabled only after a compatible version is selected");
            await model.DownloadSelected();
            Check(downloads.Calls == 1 && downloads.Query?.GameVersion == "1.21.1"
                && downloads.Project == "fixture" && downloads.Version == "fixture-version" && downloads.Destination == "fixture-output",
                "resource download passes displayed query snapshot and selected version to module");
            downloads.Fail = true;
            await model.DownloadSelected();
            Check(model.DownloadStatus.Contains("fixture rejection") && model.CanDownload, "download rejection is visible and can retry");
            await model.Select(null);
            Check(!model.CanDownload, "deselection clears downloadable version");
            model.Dispose();
            await model.DownloadSelected();
            Check(downloads.Calls == 2, "disposed resource page does not enqueue downloads");
        }
        Console.WriteLine("ALL_FRONT_RESOURCE_BROWSER_CHECKS_PASSED");
    }

    sealed class Downloads : IFrontResourceDownloads
    {
        public string DownloadDirectory => "fixture-output";
        public int Calls;
        public bool Fail;
        public string Project = "";
        public string Version = "";
        public string Destination = "";
        public FrontResourceQuery? Query;

        public Task EnqueueResource(string project, string version, FrontResourceQuery query, string destination, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            if (Fail)
                throw new IOException("fixture rejection");
            Project = project;
            Version = version;
            Query = query;
            Destination = destination;
            return Task.CompletedTask;
        }
    }

    static FrontResourceItem Item(string id) => new(id, id, "Fixture description", "Fixture author", 10);

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }

    sealed class PendingBrowser : IFrontResourceBrowser
    {
        public List<(FrontResourceQuery Query, TaskCompletionSource<FrontResourcePage> Result)> Searches { get; } = [];
        public List<(string Project, TaskCompletionSource<IReadOnlyList<FrontResourceVersion>> Result)> Details { get; } = [];

        public Task<FrontResourcePage> Search(FrontResourceQuery query, CancellationToken token)
        {
            var pending = new TaskCompletionSource<FrontResourcePage>();
            Searches.Add((query, pending));
            return pending.Task;
        }

        public Task<IReadOnlyList<FrontResourceVersion>> Versions(string project, FrontResourceQuery query, CancellationToken token)
        {
            var pending = new TaskCompletionSource<IReadOnlyList<FrontResourceVersion>>();
            Details.Add((project, pending));
            return pending.Task;
        }
    }

    sealed class Handler : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            LastUri = request.RequestUri;
            if (LastUri?.Host != "api.modrinth.com")
                throw new InvalidOperationException("Unexpected fixture host");
            var json = LastUri.AbsolutePath switch
            {
                "/v2/search" => """
                    {"hits":[{"project_id":"fixture","slug":"fixture","title":"Fixture","description":"Fixture description","author":"Fixture author","downloads":10}],"total_hits":31}
                    """,
                "/v2/project/fixture/version" => """
                    [{"id":"fixture-version","project_id":"fixture","version_number":"1.0","version_type":"release","game_versions":["1.21.1"],"loaders":["fabric"]}]
                    """,
                _ => throw new InvalidOperationException("Unexpected fixture path")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
