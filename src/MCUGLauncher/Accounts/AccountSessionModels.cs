using CmlLib.Core.Auth;

namespace BlockLaunch;

public sealed record AccountSession(MSession Session, bool IsOffline, string ClientToken = "", Func<CancellationToken, Task<string>>? ReadExternalMetadata = null);

public record SavedMicrosoftAccount(string Username, string Uuid, string SkinUrl = "");

public record MuaProfile(string Id, string Name);

public sealed class MuaCredential
{
    public const string DefaultServer = "https://skin.mualliance.ltd/api/yggdrasil/";
    public string Server { get; set; } = DefaultServer;
    public string Account { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string ClientToken { get; set; } = "";
    public MuaProfile Profile { get; set; } = new("", "");

    public override string ToString() => "MUA credential (protected)";
}

public sealed record MuaMetadata(string Json, IReadOnlyList<string> SkinDomains);
