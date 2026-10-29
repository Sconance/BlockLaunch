using System.IO;

namespace BlockLaunch;

public sealed record LaunchScriptRequest(string Root, string Instance, string Account);

public sealed record LauncherStartupOptions(string? SettingsDirectory, LaunchScriptRequest? Launch)
{
    public static LauncherStartupOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            var option = args[index];
            if (option is not ("--settings-dir" or "--root" or "--instance" or "--account")
                || index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1])
                || args[index + 1].StartsWith("--", StringComparison.Ordinal)
                || args[index + 1].Any(char.IsControl)
                || !values.TryAdd(option, args[index + 1]))
                throw new ArgumentException("参数无效或重复。用法：MCUGLauncher.Front [--settings-dir <配置目录>] [--root <游戏目录> --instance <实例标识> --account <offline|microsoft|mua>]。");
        }
        var settings = values.TryGetValue("--settings-dir", out var directory) ? Path.GetFullPath(directory) : null;
        if (!values.ContainsKey("--root") && !values.ContainsKey("--instance") && !values.ContainsKey("--account"))
            return new(settings, null);
        if (!values.TryGetValue("--root", out var root) || !values.TryGetValue("--instance", out var instance)
            || !values.TryGetValue("--account", out var account) || account is not ("offline" or "microsoft" or "mua")
            || instance is "." or ".." || instance.IndexOfAny("\\/:*?\"<>|".ToCharArray()) >= 0)
            throw new ArgumentException("脚本启动需要有效的游戏目录、实例标识和账号类型。");
        return new(settings, new(Path.GetFullPath(root), instance, account));
    }
}
