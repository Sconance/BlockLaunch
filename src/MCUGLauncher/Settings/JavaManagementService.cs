using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class JavaManagementService(Preferences preferences)
{
    public async Task<IReadOnlyList<FrontJava>> Scan(CancellationToken token) => Convert(await JavaDiscovery.Scan(preferences.Root, preferences.ExtraJavaPaths, token));

    public async Task<IReadOnlyList<FrontJava>> Search(string directory, CancellationToken token)
    {
        var roots = string.IsNullOrWhiteSpace(directory) ? JavaDiscovery.FixedDriveRoots() : new[] { Path.GetFullPath(directory) };
        var found = await Task.Run(() => JavaDiscovery.SearchDirectories(roots, token), token);
        return Convert(await JavaDiscovery.Scan(preferences.Root, preferences.ExtraJavaPaths.Concat(found.Paths), token));
    }

    public static string[] ValidatePaths(IEnumerable<string> paths)
    {
        var result = paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (result.Length > 150 || result.Any(path => !File.Exists(path) || Path.GetFileName(path).ToLowerInvariant() is not ("java.exe" or "javaw.exe")))
            throw new ArgumentException("最多保存 150 个存在的 java.exe / javaw.exe 文件路径。");
        return result;
    }

    static IReadOnlyList<FrontJava> Convert(IEnumerable<JavaInstallation> values) => values.Select(value =>
        new FrontJava(value.Path, value.Major, value.Architecture, value.Vendor, value.Error)).ToArray();
}
