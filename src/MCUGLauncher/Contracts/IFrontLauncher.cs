namespace BlockLaunch.Contracts;

public sealed record FrontAccounts(string Selected, string OfflineName, string MicrosoftName, string MuaName);
public sealed record FrontProfile(string Id, string Label, bool Installed)
{
    public string Type { get; init; } = "release";
    public string Group => Installed ? "已安装" : Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase) ? "预览版本" : "正式版本";
}
public sealed record FrontProfiles(string Root, IReadOnlyList<FrontProfile> Items, string Warning);
public sealed record FrontLoader(string Id, string Label);
public sealed record FrontIdentity(string Id, string Name);
public sealed record FrontLocalContent(string Path, string Name, string Detail, string State);

public interface IFrontResourceDownloads
{
    string DownloadDirectory { get; }
    Task EnqueueResource(string project, string version, FrontResourceQuery query, string destination, CancellationToken token);
}

public interface IFrontLauncher : IFrontResourceDownloads
{
    bool IsBusy { get; }
    bool IsGameRunning { get; }
    string Status { get; }
    IReadOnlyList<string> Logs { get; }
    Task Initialize(CancellationToken token);
    FrontAccounts ReadAccounts();
    Task LoginMicrosoft(CancellationToken token);
    Task LoginMua(string account, string password, Func<IReadOnlyList<FrontIdentity>, Task<FrontIdentity?>> select, CancellationToken token);
    void SelectAccount(string kind, string offlineName);
    Task ForgetAccount(string kind, CancellationToken token);
    Task<FrontProfiles> ReadProfiles(CancellationToken token);
    Task<IReadOnlyList<FrontLoader>> ReadLoaders(string version, string loader, CancellationToken token);
    void EnqueueGame(string version, string loader, string loaderVersion);
    Task ImportPack(string file, CancellationToken token);
    Task Launch(string version, Func<string, bool> confirmPreflight, CancellationToken token);
    void StopGame();
    IReadOnlyList<FrontLocalContent> ReadContent(string version);
    void ToggleContent(string version, string path);
    Task Tick();
    Task Shutdown();
}
