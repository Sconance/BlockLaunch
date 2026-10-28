using System.Net.Http;

namespace BlockLaunch;

internal sealed record GameLaunchSelection(
    string Root,
    string Version,
    bool IsInstalled,
    string InstanceDirectory,
    string GameVersion,
    string SettingsDirectory,
    string OfflineSkin,
    LaunchSettings DefaultSettings,
    string? ServerAddress,
    HttpClient Http);

internal sealed record GameLaunchResult(
    GameSession Session,
    DiagnosticContext Diagnostics,
    LaunchSettings Settings);
