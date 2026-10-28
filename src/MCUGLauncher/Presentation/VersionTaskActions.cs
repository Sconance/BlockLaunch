using System.Net.Http;

namespace BlockLaunch;

/// <summary>Task execution capabilities shared by version editors; no account or global UI access.</summary>
internal sealed record VersionTaskActions(
    Func<bool> IsBusy,
    Action RequireStopped,
    Func<Func<Task>, string, Task> Run,
    Func<CancellationToken> GetToken,
    Func<HttpClient> GetHttp);
