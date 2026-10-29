namespace BlockLaunch;

public static class LaunchScriptBuilder
{
    public static string Build(string executable, string root, string instance, string account, bool powershell)
    {
        static string QuotePowerShell(string text) => "'" + text.Replace("'", "''") + "'";
        static string QuoteCommandPrompt(string text) => "\"" + text.Replace("%", "%%") + "\"";

        return powershell
            ? "# 账号授权由启动器完成，脚本不保存令牌。\r\n& " + QuotePowerShell(executable) + " --root " + QuotePowerShell(root) + " --instance " + QuotePowerShell(instance) + " --account " + account + "\r\n"
            : "@echo off\r\nchcp 65001 >nul\r\nsetlocal DisableDelayedExpansion\r\n" + QuoteCommandPrompt(executable) + " --root " + QuoteCommandPrompt(root) + " --instance " + QuoteCommandPrompt(instance) + " --account " + account + "\r\n";
    }
}
