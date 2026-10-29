using System.IO;

namespace BlockLaunch;

public static class GameLogReader
{
    public static string ReadLatest(string directory)
    {
        var path = Path.Combine(directory, "logs", "latest.log");
        if (!File.Exists(path))
            return "当前实例尚无 logs/latest.log。";
        var truncated = new FileInfo(path).Length > 512 * 1024;
        return (truncated ? "仅显示末尾 512 KiB：\n" : "") + SharedLogs.ReadTail(path, 512 * 1024);
    }
}
