using System.Net.Http;

namespace BlockLaunch;

/// <summary>Immutable search input keeps asynchronous queries independent of WPF controls.</summary>
internal sealed record ResourceFavoriteQuery(string Query, string Kind, InstanceTarget Target, int Channel, int Sort, int Offset);

internal static class ResourceFavoriteSearch
{
    internal static async Task<SearchPage> Search(ResourceBookmarkStore? store, HttpClient http, ResourceFavoriteQuery request, CancellationToken token)
    {
        var kind = request.Kind;
        var candidates = store?.Query(request.Query, kind: kind).Select(i => i.Project).ToList() ?? [];
        var target = request.Target;
        if (target.GameVersion.Length > 0 || target.Loader.Length > 0 || request.Channel > 0)
        {
            var channel = request.Channel;
            using var gate = new SemaphoreSlim(4);
            var matches = await Task.WhenAll(candidates.Select(async project =>
            {
                await gate.WaitAsync(token);
                try
                {
                    var versions = await new ContentService(http, token).Versions(project.Id, target.GameVersion, target.Loader, kind);
                    return versions.Any(v => channel == 0 || v.VersionType == new[] { "", "release", "beta", "alpha" }[channel]) ? project : null;
                }
                finally
                {
                    gate.Release();
                }
            }));
            candidates = matches.OfType<ContentProject>().ToList();
        }

        if (request.Sort == 1)
            candidates = candidates.OrderByDescending(p => p.Downloads).ToList();
        return new(candidates.Skip(request.Offset).Take(15).ToList(), candidates.Count);
    }
}
