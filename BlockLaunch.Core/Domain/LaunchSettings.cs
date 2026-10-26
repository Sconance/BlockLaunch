using System.Runtime.InteropServices;
namespace BlockLaunch;
public sealed class LaunchSettings
{
    public bool AutoMemory { get; set; } = true;
    public int RamMb { get; set; } = 4096;
    public string Java { get; set; } = "";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool FullScreen { get; set; }
    public string Isolation { get; set; } = "独立实例";
    public string WindowTitle { get; set; } = "";
    public string VersionInfo { get; set; } = "BlockLaunch";
    public string Visibility { get; set; } = "保持显示";
    public string Priority { get; set; } = "Normal";
    public bool TrimLauncher { get; set; }
    public string JvmArguments { get; set; } = "";
    public string Server { get; set; } = "";
    public static IReadOnlyList<string> SplitArguments(string text)
    {
        var result = new List<string>(); var current = new System.Text.StringBuilder(); bool quote = false;
        foreach (var c in text) { if (c == '"') { quote = !quote; continue; } if (char.IsWhiteSpace(c) && !quote) { if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); } } else current.Append(c); }
        if (quote) throw new ArgumentException("JVM 参数的双引号没有配对。");
        if (current.Length > 0) result.Add(current.ToString());
        if (result.Any(x => !x.StartsWith('-') || x.StartsWith("-Xmx", StringComparison.OrdinalIgnoreCase) || x.StartsWith("-Xms", StringComparison.OrdinalIgnoreCase) || x is "-jar" or "-cp" or "-classpath")) throw new ArgumentException("JVM 参数必须以 - 开头；内存请使用内存设置，不能覆盖主类与 classpath。");
        return result;
    }
    public void Validate()
    {
        if (RamMb < 512 || RamMb > 131072 || Width < 320 || Width > 16384 || Height < 240 || Height > 16384) throw new ArgumentException("内存或窗口大小不在有效范围。");
        if (Java.Length > 0 && !File.Exists(Java)) throw new ArgumentException("指定的 Java 文件不存在。");
        if (Isolation is not ("独立实例" or "共享根目录")) throw new ArgumentException("无效的隔离选项。");
        if (Visibility is not ("保持显示" or "启动后最小化" or "启动后隐藏")) throw new ArgumentException("无效的可见性选项。");
        if (Priority is not ("Normal" or "BelowNormal" or "AboveNormal" or "High")) throw new ArgumentException("无效的进程优先级。");
        SplitArguments(JvmArguments);
    }
}
public static class MemoryPolicy
{
    [StructLayout(LayoutKind.Sequential)] struct MemoryStatus { public uint Length, Load; public ulong Total, Available, TotalPage, AvailablePage, TotalVirtual, AvailableVirtual, Extended; }
    [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    public static (ulong Total, ulong Available) Read() { var state = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() }; return GlobalMemoryStatusEx(ref state) ? (state.Total, state.Available) : (8UL << 30, 4UL << 30); }
    public static int Recommend() { var m = Read(); return (int)Math.Clamp((long)(m.Available / 1024 / 1024 * 0.6) / 256 * 256, 1024, Math.Min(8192, Math.Max(1024, (long)(m.Total / 1024 / 1024) - 2048))); }
}
