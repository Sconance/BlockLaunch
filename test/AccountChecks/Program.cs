using BlockLaunch;
using CmlLib.Core.Auth;

foreach (var address in new[] { "https://login.live.com/oauth20_authorize.srf", "https://login.microsoftonline.com/common/oauth2/v2.0/authorize" })
    Check(MicrosoftAuthorizationPolicy.IsAuthorization(new Uri(address)), "official authorization address");
foreach (var address in new[] { "http://login.live.com/", "https://login.live.com.evil.test/", "https://user@login.live.com/", "https://example.com/" })
    Check(!MicrosoftAuthorizationPolicy.IsAuthorization(new Uri(address)), "untrusted authorization address rejected");

var microsoft = new FakeMicrosoft();
var mua = new FakeMua();
var accounts = new AccountSessionService(microsoft, mua);
Check(accounts.Offline("Fixture_1").IsOffline, "offline identity");
foreach (var name in new[] { "ab", "invalid name", "12345678901234567" })
{
    try
    {
        accounts.Offline(name);
        throw new Exception("invalid offline identity accepted");
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("PASS invalid offline identity rejected");
    }
}
Check(accounts.ReadMicrosoft()?.Username == "Fixture", "saved Microsoft account comes from provider");
await accounts.EnsureMicrosoft(CancellationToken.None);
Check(microsoft.Attempts.SequenceEqual([false, true]), "silent refresh requests injected interactive provider when needed");
using var cancellation = new CancellationTokenSource();
cancellation.Cancel();
var attempts = microsoft.Attempts.Count;
try
{
    await accounts.EnsureMicrosoft(cancellation.Token);
    throw new Exception("cancelled login accepted");
}
catch (OperationCanceledException)
{
    Check(microsoft.Attempts.Count == attempts, "cancellation does not invoke provider");
}
accounts.ForgetMicrosoft();
Check(microsoft.Forgotten, "Microsoft removal delegates to provider");
Check(accounts.ReadMua()?.Profile.Name == "MuaFixture", "saved MUA account comes from provider");
Check(accounts.Mua(mua.Credential).Session.Username == "MuaFixture", "MUA session uses provider without UI");
Check(await accounts.ForgetMua(CancellationToken.None) && mua.Forgotten, "MUA removal delegates to provider");
Console.WriteLine("ALL_ACCOUNT_CHECKS_PASSED");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
    Console.WriteLine("PASS " + message);
}

sealed class FakeMicrosoft : IMicrosoftAccountProvider
{
    public List<bool> Attempts { get; } = [];
    public bool Forgotten { get; private set; }
    public SavedMicrosoftAccount? ReadSavedAccount() => new("Fixture", "fixture-id");
    public Task<MSession> Authenticate(bool interactive, CancellationToken token)
    {
        Attempts.Add(interactive);
        return interactive ? Task.FromResult(MSession.CreateOfflineSession("Fixture"))
            : Task.FromException<MSession>(new AccountLoginException("fixture refresh requires login", true));
    }
    public void Forget() => Forgotten = true;
}

sealed class FakeMua : IMuaAccountProvider
{
    public MuaCredential Credential { get; } = new() { Profile = new("fixture-id", "MuaFixture") };
    public bool Forgotten { get; private set; }
    public MuaCredential? Read() => Credential;
    public Task<MuaCredential> Login(string account, string password, Func<IReadOnlyList<MuaProfile>, Task<MuaProfile?>> select, CancellationToken token) => throw new NotSupportedException();
    public Task<MuaCredential> Ensure(bool forceRefresh, CancellationToken token) => Task.FromResult(Credential);
    public Task<bool> Forget(CancellationToken token)
    {
        Forgotten = true;
        return Task.FromResult(true);
    }
    public Task<MuaMetadata> Metadata(CancellationToken token) => Task.FromResult(new MuaMetadata("{}", []));
    public MSession CreateSession(MuaCredential credential) => MSession.CreateOfflineSession(credential.Profile.Name);
}
