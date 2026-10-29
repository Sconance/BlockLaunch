namespace BlockLaunch;

public interface IGameLaunchInteraction
{
    bool IsGameRunning { get; }
    GameSession? CurrentSession { get; }
    Task SaveSettings();
    Task<AccountSession> Authenticate();
    GameLaunchSelection CaptureSelection();
    bool ConfirmPreflight(ModScan scan);
    void Report(string message);
    void QueueOutput(string line);
    void Started(GameLaunchResult result);
    void Log(string message);
}
