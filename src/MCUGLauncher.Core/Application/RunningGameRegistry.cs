using System.Diagnostics;

namespace BlockLaunch;

public sealed record RunningGame(Guid Id, string Root, string Directory, string Version, string LogFile, GameSession Session, DateTime StartedUtc);

/// <summary>Owns independent process lifetimes, not UI selection or account state.</summary>
public sealed class RunningGameRegistry
{
    readonly object gate = new();
    readonly List<RunningGame> games = [];

    public IReadOnlyList<RunningGame> Active
    {
        get
        {
            lock (gate)
                return games.Where(g => !g.Session.Completion.IsCompleted).ToArray();
        }
    }

    public bool UsesDirectory(string directory) => Active.Any(g => SamePath(g.Directory, directory));
    public bool UsesRoot(string root) => Active.Any(g => SamePath(g.Root, root));

    public void RequireAvailable(string directory)
    {
        if (UsesDirectory(directory))
            throw new IOException("该实例正在运行。请先退出该实例，或选择另一个独立实例。");
    }

    public RunningGame Start(string root, string directory, string version, string logDirectory,
        Process process, Action<string> output, IEnumerable<string> secrets)
    {
        lock (gate)
        {
            RequireAvailable(directory);
            var id = Guid.NewGuid();
            var log = Path.Combine(logDirectory, id.ToString("N") + ".log");
            var session = new GameSession(process, log, output, secrets);
            // Do not publish a session until Process.Start succeeds.
            session.Start();
            var game = new RunningGame(id, Path.GetFullPath(root), Path.GetFullPath(directory), version, log, session, DateTime.UtcNow);
            games.Add(game);
            return game;
        }
    }

    public void Release(Guid id)
    {
        lock (gate)
            games.RemoveAll(g => g.Id == id);
    }

    static bool SamePath(string left, string right) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)).Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), StringComparison.OrdinalIgnoreCase);
}
