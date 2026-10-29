namespace BlockLaunch.Contracts;

public sealed record FrontAppearance(bool DarkMode, string Accent, string BackgroundPath,
    double Opacity, string Fit, IReadOnlyList<string> Images);

public interface IFrontPreferences
{
    Preferences ReadPreferences();
    void SaveNetwork(NetworkSettings network);
    void SaveDownloadLimit(int kibPerSecond);
    void SaveAppearance(FrontAppearance appearance);
    void SaveBackupPolicy(bool enabled, int count);
    void SaveGlobalLaunch(LaunchSettings settings);
}
