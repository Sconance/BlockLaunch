namespace BlockLaunch;

public static class LaunchScriptBuilder
{
    public static string Build(string executable, string root, string instance, string account, bool powershell, string? settingsDirectory = null)
    {
        if (account is not ("offline" or "microsoft" or "mua"))
            throw new ArgumentException("无效的脚本账号类型。");
        foreach (var value in new[] { executable, root, instance }.Concat(settingsDirectory == null ? Array.Empty<string>() : new[] { settingsDirectory }))
            if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Contains('"'))
                throw new ArgumentException("脚本参数不能为空，也不能包含控制字符或双引号。");
        static string QuotePowerShell(string text) => "'" + text.Replace("'", "''") + "'";
        static string QuoteCommandPrompt(string text) => "\"" + text.Replace("%", "%%") + "\"";
        var configuration = settingsDirectory == null ? "" : " --settings-dir " +
            (powershell ? QuotePowerShell(settingsDirectory) : QuoteCommandPrompt(settingsDirectory));
        return powershell
            ? "# 账号授权由启动器完成，脚本不保存令牌。\r\n& " + QuotePowerShell(executable) + " --root " + QuotePowerShell(root) + " --instance " + QuotePowerShell(instance) + " --account " + account + configuration + "\r\n"
            : "@echo off\r\nchcp 65001 >nul\r\nsetlocal DisableDelayedExpansion\r\n" + QuoteCommandPrompt(executable) + " --root " + QuoteCommandPrompt(root) + " --instance " + QuoteCommandPrompt(instance) + " --account " + account + configuration + "\r\n";
    }
}
