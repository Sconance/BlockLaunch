using System.IO;
using System.Text;

namespace BlockLaunch;

public static class LaunchScriptExporter
{
    public static void Export(string executable, string root, string instance, string account, string target, string? settingsDirectory = null)
    {
        var extension = Path.GetExtension(target);
        if (!extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("启动脚本必须保存为 .cmd 或 .ps1。");
        if (!File.Exists(executable) || !Path.GetExtension(executable).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            throw new IOException("请从 Windows 启动器可执行文件导出脚本，不能使用 dotnet 或测试宿主路径。");
        var powershell = extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase);
        var script = LaunchScriptBuilder.Build(executable, root, instance, account, powershell, settingsDirectory);
        File.WriteAllText(target, script, new UTF8Encoding(powershell));
    }
}
