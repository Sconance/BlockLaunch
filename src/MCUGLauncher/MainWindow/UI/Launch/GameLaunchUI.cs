using System.Windows;

namespace BlockLaunch;

public partial class MainWindow : IGameLaunchInteraction
{
    internal Task Login() => MicrosoftLogin(true);

    Task Launch() => gameLaunchController.Launch(CurrentToken);

    async void LoginClick(object s, RoutedEventArgs e) => await Run(Login);

    async void LaunchClick(object s, RoutedEventArgs e) => await Run(Launch);

    void StopClick(object s, RoutedEventArgs e) => gameLaunchController.Stop();

    bool IGameLaunchInteraction.IsGameRunning => game is { HasExited: false };

    GameSession? IGameLaunchInteraction.CurrentSession => gameSession;

    async Task IGameLaunchInteraction.SaveSettings()
    {
        var rootBefore = prefs.Root;
        SavePreferences();
        if (!string.Equals(rootBefore, prefs.Root, StringComparison.OrdinalIgnoreCase))
        {
            SetupLauncher();
            await Refresh();
            throw new InvalidOperationException("目录已切换，请重新选择版本后启动。");
        }
    }

    async Task<AccountSession> IGameLaunchInteraction.Authenticate()
    {
        if (AccountModeControl.SelectedIndex == 1)
        {
            await EnsureMicrosoftLogin();
            return accountSessions.Microsoft(online!);
        }
        if (AccountModeControl.SelectedIndex == 2)
        {
            await EnsureMuaLogin();
            return accountSessions.Mua(muaAccount!);
        }
        return accountSessions.Offline(prefs.Player);
    }

    GameLaunchSelection IGameLaunchInteraction.CaptureSelection()
    {
        var id = Selected();
        var installed = LocalVersions().Contains(id);
        return new(prefs.Root, id, installed, installed ? Instance() : "", installed ? Target().GameVersion : "",
            settingsDir, prefs.OfflineSkin, prefs.Launch, nextServerAddress, http);
    }

    bool IGameLaunchInteraction.ConfirmPreflight(ModScan scan) => ConfirmModScan(scan, "启动前检查");

    void IGameLaunchInteraction.Report(string message)
    {
        OperationStatusLabel.Text = message;
        operations.Current?.Report(message);
    }

    void IGameLaunchInteraction.QueueOutput(string line) => QueueGameLine(line);

    void IGameLaunchInteraction.Started(GameLaunchResult result)
    {
        game = result.Session.Process;
        gameSession = result.Session;
        lastGameDirectory = result.Diagnostics.Directory;
        recentDiagnosticContext = result.Diagnostics;
        _ = ObserveGame(result.Session, result.Diagnostics);
        _ = gameWindowBehaviorController.Apply(result.Session.Process, result.Settings, result.Session);
    }

    void IGameLaunchInteraction.Log(string message) => Log(message);
}
