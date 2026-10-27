using System.Net.Http;

namespace BlockLaunch;

internal sealed record PlayerIdentityState(
    int AccountMode,
    MuaCredential? MuaAccount,
    MuaAccountService MuaAccounts,
    SavedMicrosoftAccount? MicrosoftAccount,
    string? OnlineUsername,
    string PlayerName,
    string OfflineSkin,
    string SettingsDirectory,
    HttpClient Http);
