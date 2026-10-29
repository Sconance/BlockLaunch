namespace BlockLaunch.Contracts;

public sealed record FrontInstance(string Key, string Name, string Description, string Category, bool Favorite,
    string Directory, LaunchSettings Launch, bool UsesGlobalSettings)
{
    public string Minecraft { get; init; } = "";
    public string Loader { get; init; } = "";
}
public sealed record FrontWorld(string Name, string Directory);

public interface IFrontVersions
{
    FrontInstance ReadInstance(string key);
    void SaveInstanceIdentity(string key, string name, string description, string category, bool favorite);
    void SaveInstanceLaunch(string key, LaunchSettings? settings);
    void EnqueueInstanceLoader(string key, string loader, string loaderVersion);
    void EnqueueInstanceRepair(string key);
    Task<string> CloneInstance(string key, string name, CloneOptions options, CancellationToken token);
    void ExportLaunchScript(string key, string target);
    Task ExportInstance(string key, string target, ExportOptions options, CancellationToken token);
    Task ImportContent(string key, string kind, string[] files, CancellationToken token);
    IReadOnlyList<FrontWorld> ReadWorlds(string key);
    string ReadGameLog(string key);
    void OpenInstanceDirectory(string key, string child);
}
