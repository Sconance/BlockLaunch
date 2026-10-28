using CmlLib.Core.Auth;

namespace BlockLaunch;

public interface IMicrosoftAccountProvider
{
    SavedMicrosoftAccount? ReadSavedAccount();
    Task<MSession> Authenticate(bool interactive, CancellationToken token);
    void Forget();
}

public interface IMuaAccountProvider
{
    MuaCredential? Read();
    Task<MuaCredential> Login(string account, string password, Func<IReadOnlyList<MuaProfile>, Task<MuaProfile?>> select, CancellationToken token);
    Task<MuaCredential> Ensure(bool forceRefresh, CancellationToken token);
    Task<bool> Forget(CancellationToken token);
    Task<MuaMetadata> Metadata(CancellationToken token);
    MSession CreateSession(MuaCredential credential);
}
