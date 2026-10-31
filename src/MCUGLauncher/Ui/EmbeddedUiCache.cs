using System.Reflection;
using BlockLaunch;

namespace MCUGLauncher.Ui;

/// <summary>
/// 把编译进程序集的 Ui 资源解压到按版本隔离的本地缓存目录，
/// 供 WebView2 通过虚拟主机映射读取。
/// </summary>
static class EmbeddedUiCache
{
    const string ResourcePrefix = "MCUGLauncher.Ui.";

    /// <summary>解压资源并返回缓存目录；每次启动覆盖写入，保证资源与程序一致。</summary>
    public static string Extract()
    {
        var target = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MCUGLauncher",
            "ui-" + ProductInfo.Version);
        Directory.CreateDirectory(target);

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith(ResourcePrefix)))
        {
            var file = Path.Combine(target, name[ResourcePrefix.Length..]);
            // 内嵌资源名会把目录分隔符压成点，assets/logo.png 需要还原目录结构。
            if (file.EndsWith("assets.logo.png", StringComparison.OrdinalIgnoreCase))
                file = Path.Combine(target, "assets", "logo.png");

            var folder = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            using var source = assembly.GetManifestResourceStream(name);
            if (source == null)
                continue;
            using var destination = File.Create(file);
            source.CopyTo(destination);
        }
        return target;
    }
}
