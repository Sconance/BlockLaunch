using MCUGLauncher.Services;

namespace MCUGLauncher.Commands;

/// <summary>命令类的轻量日志出口，写入启动器数据目录下的 launcher.log。</summary>
static class AppLog
{
    /// <summary>
    /// 写入。
    /// </summary>
    public static void Write(ConfigStore store, string message)
    {
        try
        {
            Directory.CreateDirectory(store.DataDirectory);
            File.AppendAllText(
                Path.Combine(store.DataDirectory, "launcher.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 日志失败不应影响主流程。
        }
    }
}
