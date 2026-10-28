using System.Text.RegularExpressions;
using CmlLib.Core.Auth;

namespace BlockLaunch;

public sealed class AccountSessionService(IMicrosoftAccountProvider microsoft, IMuaAccountProvider mua)
{
    public SavedMicrosoftAccount? ReadMicrosoft() => microsoft.ReadSavedAccount();
    public MuaCredential? ReadMua() => mua.Read();

    public async Task<MSession> LoginMicrosoft(bool interactive, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return await microsoft.Authenticate(interactive, token);
    }

    public async Task<MSession> EnsureMicrosoft(CancellationToken token, Action<string>? report = null)
    {
        try
        {
            return await LoginMicrosoft(false, token);
        }
        catch (AccountLoginException ex) when (ex.RequiresInteractive)
        {
            token.ThrowIfCancellationRequested();
            report?.Invoke(ex.Message);
            return await LoginMicrosoft(true, token);
        }
    }

    public void ForgetMicrosoft() => microsoft.Forget();

    public async Task<MuaCredential> LoginMua(string account, string password, Func<IReadOnlyList<MuaProfile>, Task<MuaProfile?>> select, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return await mua.Login(account, password, select, token);
    }

    public async Task<MuaCredential> EnsureMua(bool force, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return await mua.Ensure(force, token);
    }

    public Task<bool> ForgetMua(CancellationToken token) => mua.Forget(token);

    public AccountSession Offline(string player)
    {
        if (!Regex.IsMatch(player, "^[A-Za-z0-9_]{3,16}$"))
            throw new InvalidOperationException("离线名称必须为 3–16 位英文字母、数字或下划线。");
        return new(MSession.CreateOfflineSession(player), true);
    }

    public AccountSession Microsoft(MSession session) => new(session, false);

    public AccountSession Mua(MuaCredential credential) => new(
        mua.CreateSession(credential), false, credential.ClientToken,
        async token => (await mua.Metadata(token)).Json);
}
