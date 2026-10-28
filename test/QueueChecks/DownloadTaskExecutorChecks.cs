using BlockLaunch;
using System.Net;
using System.Security.Cryptography;

internal static class DownloadTaskExecutorChecks
{
    public static async Task Run()
    {
        using (var fixture = new Fixture())
        {
            var request = fixture.Resource();
            var result = await fixture.Run(request);
            Check(result.State == OperationState.Completed, "resource executor completes through the existing coordinator");
            Check(File.ReadAllBytes(ResourceDownloads.PathFor(request.Destination, "mod", request.Content!)).SequenceEqual(Fixture.Bytes), "resource executor downloads and verifies fixture bytes");
            Check(fixture.RefreshCount == 0, "standalone resource downloads do not refresh installed versions");
        }

        using (var fixture = new Fixture())
        {
            var request = fixture.Resource();
            request.Destination = Path.Combine(fixture.GameRoot, "downloads");
            await Fails<IOException>(() => fixture.Run(request));
            Check(fixture.Handler.Requests == 0, "resource destination inside game root is rejected before network access");
        }

        using (var fixture = new Fixture())
        {
            await Fails<IOException>(() => fixture.Run(new() { Kind = "unknown" }));
            Check(fixture.Handler.Requests == 0 && fixture.RefreshCount == 0, "unsupported task does not download or refresh");
        }

        using (var fixture = new Fixture())
        {
            var file = Path.Combine(fixture.Directory, "changed.mrpack");
            File.WriteAllText(file, "changed source");
            await Fails<IOException>(() => fixture.Run(new() { Kind = "import", File = file, SourceHash = "different-hash" }));
            Check(fixture.Handler.Requests == 0, "changed import source is rejected before pack installation");
        }

        using (var fixture = new Fixture())
        {
            var request = fixture.Content();
            fixture.RefreshError = new IOException("synthetic UI refresh failure");
            var result = await fixture.Run(request);
            Check(result.State == OperationState.Completed && File.Exists(Path.Combine(request.Destination, "mods", "fixture.jar")), "refresh failure cannot make committed content retryable");
            Check(fixture.Logs.Any(message => message.Contains("列表刷新暂不可用")), "refresh failure is reported separately from installation");
        }

        using (var fixture = new Fixture())
        {
            fixture.CurrentRoot = Path.Combine(fixture.Directory, "other-game");
            var request = fixture.Content();
            var result = await fixture.Run(request);
            Check(result.State == OperationState.Completed && File.Exists(Path.Combine(request.Destination, "mods", "fixture.jar")) && fixture.RefreshCount == 0, "task keeps its original destination when another root is selected");
        }

        using (var fixture = new Fixture())
        {
            var request = fixture.Content();
            request.BackupInstanceDirectory = request.Destination;
            fixture.BackupError = new IOException("synthetic backup failure");
            await Fails<IOException>(() => fixture.Run(request));
            Check(fixture.BackupRoot == fixture.GameRoot && fixture.Handler.Requests == 0 && fixture.RefreshCount == 0, "backup callback receives task root and failure blocks installation");
        }

        using (var fixture = new Fixture())
        {
            var result = await fixture.Run(fixture.Resource(), () => fixture.Queue.Cancel(true));
            Check(result.State == OperationState.Paused && fixture.Handler.Requests == 0, "paused execution observes coordinator cancellation before download");
        }
    }

    static async Task Fails<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException("Expected " + typeof(TException).Name);
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    sealed class Fixture : IDisposable
    {
        public static readonly byte[] Bytes = { 4, 8, 15, 16, 23, 42 };
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "MCUG-download-executor-" + Guid.NewGuid().ToString("N"));
        public string GameRoot => Path.Combine(Directory, "game");
        public string CurrentRoot { get; set; }
        public Handler Handler { get; } = new();
        public OperationCoordinator Queue { get; }
        public int RefreshCount { get; private set; }
        public Exception? RefreshError { get; set; }
        public Exception? BackupError { get; set; }
        public string? BackupRoot { get; private set; }
        public List<string> Logs { get; } = new();
        readonly HttpClient http;
        readonly DownloadTaskExecutor executor;

        public Fixture()
        {
            CurrentRoot = GameRoot;
            http = new HttpClient(Handler);
            Queue = new OperationCoordinator(Path.Combine(Directory, "tasks"));
            executor = new DownloadTaskExecutor(new(http, new(), ProtectWorlds, () => new(),
                root => string.Equals(root, CurrentRoot, StringComparison.OrdinalIgnoreCase), Refresh, Logs.Add));
        }

        Task ProtectWorlds(string root, string directory, CancellationToken token)
        {
            BackupRoot = root;
            return BackupError == null ? Task.CompletedTask : Task.FromException(BackupError);
        }

        Task Refresh(string root, string? installed)
        {
            RefreshCount++;
            return RefreshError == null ? Task.CompletedTask : Task.FromException(RefreshError);
        }

        public DownloadRequest Resource() => new()
        {
            Kind = "resource",
            ResourceKind = "mod",
            Destination = Path.Combine(Directory, "downloads"),
            Content = Version()
        };

        public DownloadRequest Content() => new()
        {
            Kind = "content",
            Destination = Path.Combine(GameRoot, "instances", "fixture"),
            PreparedPlan = new(new() { Version() }, new() { ["fixture-project"] = "mod" })
        };

        static ContentVersion Version() => new()
        {
            Id = "fixture-version",
            ProjectId = "fixture-project",
            Name = "Synthetic resource",
            Files = new()
            {
                new()
                {
                    Filename = "fixture.jar",
                    Primary = true,
                    Url = "https://cdn.modrinth.com/fixture.jar",
                    Size = Bytes.Length,
                    Hashes = new() { ["sha512"] = Convert.ToHexString(SHA512.HashData(Bytes)) }
                }
            }
        };

        public Task<OperationRecord> Run(DownloadRequest request, Action? before = null)
        {
            var record = Queue.Enqueue("synthetic executor check", GameRoot, "fixture", request);
            return Queue.Run(record.Name, record.Root, record.Target, context =>
            {
                before?.Invoke();
                return executor.Execute(record, context);
            }, record);
        }

        public void Dispose()
        {
            http.Dispose();
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, true);
        }
    }

    sealed class Handler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri?.Host != "cdn.modrinth.com")
                throw new InvalidOperationException("Unexpected fixture destination");
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Fixture.Bytes) });
        }
    }
}
