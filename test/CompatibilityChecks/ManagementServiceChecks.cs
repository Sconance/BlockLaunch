using BlockLaunch;
using System.Net;
using System.Text;

internal static class ManagementServiceChecks
{
    public static async Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCUG-management-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var handler = new FixtureHandler();
            using var http = new HttpClient(handler);
            var versions = new VersionManagementService(http);
            var resources = new ResourceManagementService(http);
            var root = Path.Combine(directory, "game");
            AtomicFile.WriteJson(Path.Combine(root, "versions", "ready", "ready.json"), new { id = "ready", type = "release" });
            AtomicFile.WriteJson(Path.Combine(root, "versions", "broken", "broken.json"), new { id = "broken" });
            AtomicFile.WriteJson(Path.Combine(root, ".blocklaunch-installations", "broken.json"), new VersionInstallation { Version = "broken", Ready = false });
            Check(versions.ProfileKeys(root).SetEquals(["ready", "broken"]) && versions.LocalVersions(root).SetEquals(["ready"]), "version discovery preserves incomplete profiles without marking them installed");

            var online = await versions.Read(root, _ => Task.FromResult(new List<VersionRow> { new("remote", "remote", "release") }), CancellationToken.None);
            Check(online.Root == root && online.Versions.Count == 3 && online.Versions.Any(row => row.Id == "broken" && row.Label.Contains("需修复")), "online versions merge local-only profiles and repair labels");
            var offline = await versions.Read(root, _ => Task.FromException<List<VersionRow>>(new IOException("offline fixture")), CancellationToken.None);
            Check(offline.OnlineError == "offline fixture" && offline.Versions.Count == 2 && offline.LocalVersions.SetEquals(["ready"]), "offline failure returns local versions and separate warning");

            var descriptor = versions.Descriptor(root, "ready");
            descriptor.DisplayName = "Custom version";
            descriptor.Favorite = true;
            versions.Save(root, descriptor);
            Check(versions.VersionLabel(root, "ready", versions.LocalVersions(root)) == "★ Custom version", "custom names and favorite labels survive extraction");
            Check(versions.InstanceDirectory(root, "ready", new()) == Path.Combine(root, "instances", "ready"), "default isolated instance directory is retained");
            Check(versions.InstanceDirectory(root, "ready", new() { Isolation = "共享根目录" }) == root, "shared directory follows global defaults");
            descriptor.Launch = new() { Isolation = "独立实例" };
            versions.Save(root, descriptor);
            Check(versions.InstanceDirectory(root, "ready", new() { Isolation = "共享根目录" }) != root, "instance isolation overrides global defaults");
            versions.SaveIdentity(root, "ready", "Renamed", "Description", "常用版本", true);
            Check(versions.Descriptor(root, "ready").DisplayName == "Renamed" && versions.Descriptor(root, "ready").Category == "常用版本", "shared version identity updates metadata");
            var launchSettings = new LaunchSettings { AutoMemory = false, RamMb = 3072 };
            versions.SaveLaunch(root, "ready", launchSettings);
            launchSettings.RamMb = 8192;
            Check(versions.Descriptor(root, "ready").Launch?.RamMb == 3072, "version launch settings save a detached validated snapshot");
            await Fails<ArgumentException>(() => Task.Run(() => versions.SaveLaunch(root, "ready", new() { RamMb = 1 })));
            Check(versions.Descriptor(root, "ready").Launch?.RamMb == 3072, "invalid version launch settings preserve stored configuration");
            versions.SaveLaunch(root, "ready", null);
            Check(versions.Descriptor(root, "ready").Launch == null, "version launch settings can resume following global defaults");
            var remote = versions.Target(root, "remote", online.Versions);
            Check(remote == new InstanceTarget("remote", "remote", "vanilla"), "uninstalled official version resolves without creating an instance");
            Check(!Directory.Exists(Path.Combine(root, "instances", "remote")), "resolving remote versions does not write metadata");

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Fails<OperationCanceledException>(() => versions.Read(root, _ => throw new InvalidOperationException("must not read online"), cancelled.Token));
            Check(handler.Requests == 0, "cancelled version query stops before network access");

            var fabric = await versions.StableFabric("remote", online.Versions, CancellationToken.None);
            Check(fabric.Version == "remote" && fabric.Loader == "fabric" && fabric.LoaderVersion == "stable-loader", "Fabric preparation chooses the stable loader and preserves selected game version");
            var beforeInvalidVersion = handler.Requests;
            await Fails<IOException>(() => versions.StableFabric("broken", online.Versions, CancellationToken.None));
            Check(handler.Requests == beforeInvalidVersion, "Fabric rejects non-official profiles before querying loaders");
            var loaders = await versions.Loaders("remote", "fabric", CancellationToken.None);
            Check(loaders.Any(loader => loader.Version == "stable-loader"), "shared version module provides Fabric loader list");

            var query = new ResourceSearchQuery("sample", "1.21.1", "fabric", "mod", Channel: "beta");
            var page = await resources.Search(query, CancellationToken.None);
            Check(page.Total == 0 && handler.LastUri!.Query.Contains("index=relevance"), "resource search forwards query and sorting to provider");
            var releases = await resources.Versions("fixture", query, CancellationToken.None);
            Check(releases.Count == 1 && releases[0].VersionType == "beta", "resource versions combine compatibility and release-channel filtering");
            var favorites = new List<ContentProject> { new("fixture", "fixture", "Fixture", "", "", 1), new("popular", "popular", "Popular", "", "", 100) };
            var favoritePage = await resources.SearchFavorites(favorites, query with { Sort = "downloads", Offset = 1 }, CancellationToken.None);
            Check(favoritePage.Total == 2 && favoritePage.Items.Single().Id == "fixture", "favorite compatibility filtering precedes sorting and pagination");

            var content = new ContentVersion { Id = "resource", ProjectId = "fixture", Files = [new() { Filename = "fixture.jar", Url = "https://cdn.modrinth.com/fixture.jar", Primary = true, Hashes = new() { ["sha512"] = new string('a', 128) } }] };
            var request = resources.PrepareDownload(root, Path.Combine(directory, "downloads"), "mod", content);
            Check(request.Kind == "resource" && request.Content == content && request.Destination == Path.Combine(directory, "downloads"), "resource queue request preserves selected content and destination");
            await Fails<IOException>(() => Task.FromResult(resources.PrepareDownload(root, root, "mod", content)));
            await Fails<IOException>(() => Task.FromResult(resources.PrepareDownload(root, Path.Combine(root, "downloads"), "mod", content)));

            var world = Path.Combine(root, "instances", "ready", "saves", "world");
            Directory.CreateDirectory(world);
            File.WriteAllText(Path.Combine(world, "level.dat"), "fixture");
            Check(resources.ValidateWorld(Path.Combine(root, "instances", "ready"), world) == world, "datapack destination accepts a world inside instance saves");
            await Fails<IOException>(() => Task.FromResult(resources.ValidateWorld(Path.Combine(root, "instances", "other"), world)));

            var target = new ResourceInstallTarget(root, "ready", Path.Combine(root, "instances", "ready"), "1.21.1", "vanilla");
            var before = handler.Requests;
            await Fails<IOException>(() => resources.PrepareInstallation(content, target, "mod", true, CancellationToken.None));
            await Fails<IOException>(() => resources.PrepareInstallation(content, target with { Version = "broken" }, "mod", false, CancellationToken.None));
            await Fails<IOException>(() => resources.Import(target, "mods", [], false, CancellationToken.None));
            await Fails<IOException>(() => resources.Restore(target.Directory, true, CancellationToken.None));
            await Fails<OperationCanceledException>(() => resources.SearchFavorites(favorites, query, cancelled.Token));
            Check(handler.Requests == before, "running game, incomplete installation and vanilla mod import are rejected before network access");

            var preview = new ContentUpdatePreview(new([], new()), "fixture-fingerprint", [], 0);
            var update = resources.PrepareUpdate(preview, target.Directory, target.GameVersion, target.Loader);
            Check(update.Kind == "content" && update.PreparedPlan == preview.Plan && update.ContentFingerprint == preview.Fingerprint && update.Destination == target.Directory, "update request retains prepared plan, fingerprint and original destination");

            var source = Path.Combine(directory, "fixture.zip");
            File.WriteAllText(source, "fixture bytes");
            Check(await resources.Import(target, "resourcepacks", [source], false, CancellationToken.None) == 1, "local resource import uses explicit instance target");
            var local = resources.ListLocal(target.Directory).Single();
            await Fails<IOException>(() => { resources.Toggle(local, true); return Task.CompletedTask; });
            resources.Toggle(local, false);
            var disabled = resources.ListLocal(target.Directory).Single();
            Check(disabled.Path.EndsWith(".disabled"), "local resource toggle preserves disabled-file convention");
            resources.MoveToBackup(target.Directory, disabled, false);
            Check(resources.ListLocal(target.Directory).Count == 0, "local resource removal retains backup behavior");
        }
        finally
        {
            Directory.Delete(directory, true);
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
            Console.WriteLine("PASS expected " + typeof(TException).Name);
            return;
        }
        throw new InvalidOperationException("Expected " + typeof(TException).Name);
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }

    sealed class FixtureHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            LastUri = request.RequestUri;
            if (LastUri?.Host == "meta.fabricmc.net" && LastUri.AbsolutePath == "/v2/versions/loader/remote")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [{"loader":{"stable":false,"version":"beta-loader"}},{"loader":{"stable":true,"version":"stable-loader"}}]
                        """, Encoding.UTF8, "application/json")
                });
            if (LastUri?.Host != "api.modrinth.com")
                throw new InvalidOperationException("Unexpected fixture destination");
            var json = LastUri.AbsolutePath == "/v2/search" ? "{\"hits\":[],\"total_hits\":0}" : LastUri.AbsolutePath.EndsWith("/version") ? """
                [
                    {"id":"stable","project_id":"fixture","version_type":"release","game_versions":["1.21.1"],"loaders":["fabric"]},
                    {"id":"beta","project_id":"fixture","version_type":"beta","game_versions":["1.21.1"],"loaders":["fabric"]},
                    {"id":"incompatible","project_id":"fixture","version_type":"beta","game_versions":["1.20.1"],"loaders":["forge"]}
                ]
                """ : throw new InvalidOperationException("Unexpected fixture path");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
