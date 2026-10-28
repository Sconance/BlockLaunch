using BlockLaunch;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class ContentUpdateControllerChecks
{
    public static async Task Run()
    {
        using (var fixture = new Fixture())
        {
            var target = fixture.UI.Target;
            var owner = new object();
            await fixture.Controller.Open(target, "fixture", owner);
            Check(fixture.UI.Selection is { Shown: true } && fixture.UI.Project == "fixture" && ReferenceEquals(fixture.UI.Owner, owner), "update selection receives project filter and original presentation owner");
            fixture.UI.Target = target with { Root = Path.Combine(fixture.Directory, "other"), Directory = Path.Combine(fixture.Directory, "other-instance"), Version = "other" };
            await fixture.Preview();
            var queued = fixture.UI.Queued.Single();
            Check(queued.Target == target && queued.Request.Destination == target.Directory && queued.Request.GameVersion == target.GameVersion && queued.Request.Loader == target.Loader, "approved update retains target snapshot when UI selection changes");
            Check(queued.Request.PreparedPlan!.Versions.Single().Id == "new" && queued.Request.ContentFingerprint == await ContentUpdates.Fingerprint(target.Directory, CancellationToken.None), "approved update retains resolved plan and content fingerprint");
            Check(fixture.UI.Selection!.Closed && fixture.UI.Runs.SequenceEqual(new[] { "检查兼容更新", "解析更新与依赖" }) && fixture.UI.Errors.Count == 0, "successful confirmation queues before closing selection through existing task adapter");
        }

        using (var fixture = new Fixture())
        {
            await fixture.Controller.CheckAll();
            fixture.UI.Selection!.Accept = false;
            await fixture.Preview();
            Check(fixture.UI.Captures == 1 && fixture.UI.Selection.Preview != null && fixture.UI.Queued.Count == 0 && !fixture.UI.Selection.Closed, "declining preview keeps selection open and does not enqueue");
        }

        using (var fixture = new Fixture())
        {
            fixture.UI.IsBusy = true;
            await fixture.Controller.CheckAll();
            await fixture.Controller.Open(fixture.UI.Target);
            await fixture.Controller.Restore(fixture.UI.Target.Directory);
            Check(fixture.UI.Captures == 0 && fixture.UI.Selection == null && fixture.UI.RestorePrompts == 0 && fixture.Handler.Requests == 0, "busy task adapter blocks checking, direct opening and restore confirmation");
        }

        using (var fixture = new Fixture())
        {
            fixture.UI.CaptureError = new IOException("synthetic target error");
            await fixture.Controller.CheckAll();
            Check(fixture.UI.Errors.Single().Message == "synthetic target error" && fixture.UI.Selection == null, "target resolution failure is presented through interaction interface");
        }

        using (var fixture = new Fixture())
        {
            fixture.UI.IsGameRunning = true;
            await fixture.Controller.Open(fixture.UI.Target);
            Check(fixture.UI.Errors.Single() is IOException && fixture.UI.Selection == null && fixture.Handler.Requests == 0, "running game blocks update check before HTTP and selection UI");
        }

        using (var fixture = new Fixture())
        {
            fixture.Cancellation.Cancel();
            await fixture.Controller.Open(fixture.UI.Target);
            Check(fixture.UI.Cancelled == 1 && fixture.UI.Selection == null && fixture.Handler.Requests == 0, "cancelled check does not open update selection");
        }

        using (var fixture = new Fixture())
        {
            await fixture.Controller.Open(fixture.UI.Target);
            fixture.Cancellation.Cancel();
            await fixture.Preview();
            Check(fixture.UI.Cancelled == 1 && fixture.UI.Selection!.Preview == null && fixture.UI.Queued.Count == 0, "cancelled dependency resolution cannot reach confirmation or enqueue");
        }

        using (var fixture = new Fixture())
        {
            await fixture.Controller.Open(fixture.UI.Target);
            fixture.UI.IsGameRunning = true;
            await fixture.Preview();
            Check(fixture.UI.Errors.Single() is IOException && fixture.UI.Selection!.Preview == null && fixture.UI.Queued.Count == 0, "preview rechecks live game-running state instead of initial check state");
        }

        using (var fixture = new Fixture())
        {
            await fixture.Controller.Open(fixture.UI.Target);
            await fixture.UI.Selection!.Choose([]);
            Check(fixture.UI.Errors.Single() is IOException && fixture.UI.Selection.Preview == null && fixture.UI.Queued.Count == 0, "empty update selection fails without confirmation or queue mutation");
        }

        using (var fixture = new Fixture())
        {
            File.WriteAllText(Path.Combine(fixture.UI.Target.Directory, ".blocklaunch-content.json"), "invalid JSON");
            await fixture.Controller.Open(fixture.UI.Target);
            Check(fixture.UI.Errors.Single() is JsonException && fixture.UI.Selection == null, "failed check does not create a selection from partial results");
        }

        using (var fixture = new Fixture())
        {
            var imported = await fixture.SeedRestore();
            fixture.UI.AcceptRestore = false;
            await fixture.Controller.Restore(fixture.UI.Target.Directory);
            Check(File.Exists(imported) && fixture.UI.Runs.Count == 0 && fixture.UI.Restored == 0, "declining restore preserves files and skips task execution");
            fixture.UI.AcceptRestore = true;
            await fixture.Controller.Restore(fixture.UI.Target.Directory);
            Check(!File.Exists(imported) && fixture.UI.Restored == 1 && fixture.UI.Errors.Count == 0, "successful restore invokes completion adapter only after transaction rollback");
        }

        using (var fixture = new Fixture())
        {
            var imported = await fixture.SeedRestore();
            File.WriteAllText(imported, "user edit");
            await fixture.Controller.Restore(fixture.UI.Target.Directory);
            Check(File.ReadAllText(imported) == "user edit" && fixture.UI.Restored == 0 && fixture.UI.Errors.Single() is IOException, "changed installed file blocks restore and success refresh");
        }

        using (var fixture = new Fixture())
        {
            var imported = await fixture.SeedRestore();
            fixture.Cancellation.Cancel();
            await fixture.Controller.Restore(fixture.UI.Target.Directory);
            Check(File.Exists(imported) && fixture.UI.Cancelled == 1 && fixture.UI.Restored == 0, "cancelled restore preserves installed files and skips success notification");
        }
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }

    sealed class Fixture : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "MCUG-update-controller-" + Guid.NewGuid().ToString("N"));
        public CancellationTokenSource Cancellation { get; } = new();
        public Handler Handler { get; } = new();
        public Interaction UI { get; }
        public ContentUpdateController Controller { get; }
        readonly HttpClient http;
        readonly ResourceManagementService resources;

        public Fixture()
        {
            var root = Path.Combine(Directory, "game");
            var directory = Path.Combine(root, "instances", "fixture");
            AtomicFile.WriteJson(Path.Combine(root, "versions", "fixture", "fixture.json"), new { id = "fixture" });
            System.IO.Directory.CreateDirectory(Path.Combine(directory, "mods"));
            File.WriteAllText(Path.Combine(directory, "mods", "test.jar"), "original bytes");
            var managed = new[] { new ManagedContent { ProjectId = "fixture", VersionId = "old", Name = "Fixture", RelativePath = "mods/test.jar", Sha512 = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes("original bytes"))) } };
            File.WriteAllText(Path.Combine(directory, ".blocklaunch-content.json"), JsonSerializer.Serialize(managed, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
            http = new HttpClient(Handler);
            resources = new ResourceManagementService(http);
            UI = new Interaction(new(root, "fixture", directory, "1.21.1", "fabric"), Cancellation.Token);
            Controller = new ContentUpdateController(resources, UI);
        }

        public Task Preview() => UI.Selection!.Choose(UI.Updates.Where(update => update.Latest != null).Select(update => update.Latest!).ToList());

        public async Task<string> SeedRestore()
        {
            var source = Path.Combine(Directory, "import.zip");
            File.WriteAllText(source, "import bytes");
            await resources.Import(UI.Target, "resourcepacks", [source], false, CancellationToken.None);
            return Path.Combine(UI.Target.Directory, "resourcepacks", "import.zip");
        }

        public void Dispose()
        {
            http.Dispose();
            Cancellation.Dispose();
            System.IO.Directory.Delete(Directory, true);
        }
    }

    sealed class Interaction(ResourceInstallTarget target, CancellationToken token) : IContentUpdateInteraction
    {
        public bool IsBusy { get; set; }
        public bool IsGameRunning { get; set; }
        public ResourceInstallTarget Target { get; set; } = target;
        public Exception? CaptureError { get; set; }
        public int Captures { get; private set; }
        public int Cancelled { get; private set; }
        public List<string> Runs { get; } = [];
        public List<Exception> Errors { get; } = [];
        public IReadOnlyList<ContentUpdate> Updates { get; private set; } = [];
        public string? Project { get; private set; }
        public object? Owner { get; private set; }
        public Selection? Selection { get; private set; }
        public List<(ResourceInstallTarget Target, DownloadRequest Request)> Queued { get; } = [];
        public bool AcceptRestore { get; set; } = true;
        public int RestorePrompts { get; private set; }
        public int Restored { get; private set; }

        public ResourceInstallTarget CaptureTarget()
        {
            Captures++;
            if (CaptureError != null)
                throw CaptureError;
            return Target;
        }

        public async Task Run(string name, Func<CancellationToken, Task> action)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            Runs.Add(name);
            try
            {
                await action(token);
            }
            catch (OperationCanceledException)
            {
                Cancelled++;
            }
            catch (Exception exception)
            {
                Errors.Add(exception);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public IContentUpdateSelection CreateSelection(IReadOnlyList<ContentUpdate> updates, string? project, object? owner, Func<IContentUpdateSelection, IReadOnlyList<ContentVersion>, Task> preview)
        {
            Updates = updates;
            Project = project;
            Owner = owner;
            Selection = new Selection(preview);
            return Selection;
        }

        public void EnqueueUpdate(ResourceInstallTarget target, string name, DownloadRequest request) => Queued.Add((target, request));

        public bool ConfirmRestore()
        {
            RestorePrompts++;
            return AcceptRestore;
        }

        public void RestoreCompleted() => Restored++;

        public void ShowError(string message) => Errors.Add(new IOException(message));
    }

    sealed class Selection(Func<IContentUpdateSelection, IReadOnlyList<ContentVersion>, Task> preview) : IContentUpdateSelection
    {
        public bool Shown { get; private set; }
        public bool Closed { get; private set; }
        public bool Accept { get; set; } = true;
        public ContentUpdatePreview? Preview { get; private set; }
        public void Show() => Shown = true;
        public void Close() => Closed = true;
        public bool ConfirmPreview(ContentUpdatePreview value)
        {
            Preview = value;
            return Accept;
        }
        public Task Choose(IReadOnlyList<ContentVersion> roots) => preview(this, roots);
    }

    sealed class Handler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (request.RequestUri?.Host != "api.modrinth.com")
                throw new InvalidOperationException("Unexpected fixture host");
            Requests++;
            var json = request.RequestUri.AbsolutePath switch
            {
                "/v2/project/fixture/version" => """
                    [{"id":"new","project_id":"fixture","name":"Fixture","version_number":"new","version_type":"release","game_versions":["1.21.1"],"loaders":["fabric"],"files":[{"filename":"test.jar","url":"https://cdn.modrinth.com/test.jar","size":10,"primary":true}]}]
                    """,
                "/v2/project/fixture" => """{"project_type":"mod","client_side":"required"}""",
                _ => throw new InvalidOperationException("Unexpected fixture path")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
