namespace BlockLaunch;

internal sealed record RunningGameRow(Guid Id, string Version, string Directory);

/// <summary>Presentation port. No WPF objects or process handles cross this boundary.</summary>
internal interface IRunningGamesView
{
    event Action<Guid>? CloseRequested;
    event Action<Guid>? LogRequested;
    void Render(IReadOnlyList<RunningGameRow> games);
    void Show();
    void ShowLog(string version, string content);
}
