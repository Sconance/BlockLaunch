using System.Net.Http;

namespace BlockLaunch;

public sealed class PackUpgradeService(HttpClient http)
{
    public Task<PackUpgradePreview> Prepare(string root, string key, string directory, string archive,
        string java, NetworkSettings network, Action<string> progress, CancellationToken token) =>
        PackUpgrade.Prepare(http, root, key, directory, archive,
            requirements => new GameInstaller(http, root, java, progress, token, network).Install(requirements),
            false, progress, token);

    public Task<string> Commit(PackUpgradePreview preview, IEnumerable<string> keepOld,
        LaunchSettings settings, Action<string> progress, CancellationToken token) =>
        PackUpgrade.Commit(preview, keepOld, settings, progress, token);
}
