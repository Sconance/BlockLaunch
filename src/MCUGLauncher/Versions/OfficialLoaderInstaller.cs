using System.Diagnostics;
using System.IO;

namespace BlockLaunch;

public static class OfficialLoaderInstaller
{
    public static async Task Run(string root, string java, string file, Action<string> report, CancellationToken token)
    {
        if (!File.Exists(file) || !Path.GetExtension(file).Equals(".jar", StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(file).Contains("installer", StringComparison.OrdinalIgnoreCase))
            throw new IOException("请选择官方网站提供的 installer.jar 安装器。");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(root);
        var marker = Path.Combine(root, "launcher_profiles.json");
        if (!File.Exists(marker))
            await File.WriteAllTextAsync(marker, "{\"profiles\":{}}", token);
        var start = new ProcessStartInfo(string.IsNullOrWhiteSpace(java) ? "java" : java)
        {
            UseShellExecute = false, WorkingDirectory = root, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add("-jar");
        start.ArgumentList.Add(Path.GetFullPath(file));
        start.ArgumentList.Add("--installClient");
        start.ArgumentList.Add(root);
        using var installer = Process.Start(start) ?? throw new IOException("安装器无法启动。");
        async Task Drain(StreamReader reader)
        {
            var last = DateTime.MinValue;
            while (await reader.ReadLineAsync() is { } line)
                if ((DateTime.UtcNow - last).TotalSeconds >= 1)
                {
                    report("安装器：" + line[..Math.Min(line.Length, 220)]);
                    last = DateTime.UtcNow;
                }
        }
        var output = Drain(installer.StandardOutput);
        var error = Drain(installer.StandardError);
        try
        {
            await installer.WaitForExitAsync(token);
        }
        catch (OperationCanceledException)
        {
            if (!installer.HasExited)
                installer.Kill(true);
            await installer.WaitForExitAsync();
            throw;
        }
        finally
        {
            await Task.WhenAll(output, error);
        }
        if (installer.ExitCode != 0)
            throw new IOException("官方安装器返回失败，请查看运行日志。");
    }
}
