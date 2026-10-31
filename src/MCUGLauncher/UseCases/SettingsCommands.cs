using BlockLaunch;
using System.Text.Json;
using System.IO.Compression;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

// Handles settings use cases; no WPF controls or browser access.
internal sealed class SettingsCommands(LauncherContext context)
{
    public void SaveSettings(JsonElement root)
    {
        if (root.TryGetProperty("player", out var player))
            context.Config.Player = player.GetString() ?? context.Config.Player;
        if (root.TryGetProperty("anonymous", out var anonymous))
            context.Config.Anonymous = anonymous.GetBoolean();
        if (root.TryGetProperty("writeEndpoint", out var endpoint))
            context.Config.WriteEndpoint = endpoint.GetString() ?? "";
        if (root.TryGetProperty("dataSources", out var sources))
        {
            context.Config.DataSources.Announcements = ReadString(sources, "announcements");
            context.Config.DataSources.Servers = ReadString(sources, "servers");
            context.Config.DataSources.Recommendations = ReadString(sources, "recommendations");
            context.Config.DataSources.Quotes = ReadString(sources, "quotes");
        }
        if (root.TryGetProperty("ram", out var ram) && ram.TryGetInt32(out var ramValue))
            context.Config.Ram = Math.Clamp(ramValue, 512, 65536);
        if (root.TryGetProperty("root", out var gameRoot))
        {
            var value = gameRoot.GetString()?.Trim() ?? "";
            if (value.Length > 0)
                context.Config.Root = value;
        }
        if (root.TryGetProperty("java", out var java))
            context.Config.Java = java.GetString()?.Trim() ?? "";
        if (root.TryGetProperty("width", out var width) && width.TryGetInt32(out var widthValue))
            context.Config.Width = Math.Clamp(widthValue, 640, 7680);
        if (root.TryGetProperty("height", out var height) && height.TryGetInt32(out var heightValue))
            context.Config.Height = Math.Clamp(heightValue, 480, 4320);
        context.Store.Save(context.Config);
        context.Send(new
        {
            type = "settingsSaved"
        });
        context.RefreshState();
    }

    public Task ScanJavaAsync()
    {
        try
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(context.Config.Java))
                candidates.Add(context.Config.Java);
            foreach (var home in new[] { Environment.GetEnvironmentVariable("JAVA_HOME"), Environment.GetEnvironmentVariable("JDK_HOME") })
                if (!string.IsNullOrWhiteSpace(home))
                    candidates.Add(Path.Combine(home, "bin", "java.exe"));
            foreach (var part in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                if (part.Length > 0)
                    candidates.Add(Path.Combine(part, "java.exe"));

            var bases = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            var vendors = new[] { "Java", "Microsoft", "Eclipse Adoptium", "Zulu", "BellSoft", "Amazon Corretto" };
            foreach (var basis in bases.Where(Directory.Exists))
            {
                foreach (var vendor in vendors)
                {
                    var vendorRoot = Path.Combine(basis, vendor);
                    if (!Directory.Exists(vendorRoot))
                        continue;
                    candidates.Add(Path.Combine(vendorRoot, "bin", "java.exe"));
                    foreach (var child in Directory.GetDirectories(vendorRoot, "*", SearchOption.TopDirectoryOnly).Take(24))
                        candidates.Add(Path.Combine(child, "bin", "java.exe"));
                }
                var microsoft = Path.Combine(basis, "Microsoft");
                if (Directory.Exists(microsoft))
                    foreach (var child in Directory.GetDirectories(microsoft, "jdk*", SearchOption.TopDirectoryOnly).Take(24))
                        candidates.Add(Path.Combine(child, "bin", "java.exe"));
            }

            var list = candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .Select(path => new { Path = path, Major = 0, Architecture = "x64", Vendor = "本机", Error = "" })
                .ToList();
            context.Send(new
            {
                type = "javaList",
                items = list.Select(item => new
                {
                    path = item.Path,
                    major = item.Major,
                    arch = item.Architecture,
                    vendor = item.Vendor,
                    error = item.Error
                })
            });
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "toast",
                message = "扫描 Java 失败：" + ex.Message
            });
        }
        return Task.CompletedTask;
    }

    public void BackupSaves()
    {
        try
        {
            var saves = Path.Combine(context.Config.Root, "saves");
            if (!Directory.Exists(saves))
            {
                context.Send(new
                {
                    type = "toast",
                    message = "还没有存档文件夹。"
                });
                return;
            }
            var folder = Path.Combine(context.Store.DataDirectory, "backups");
            Directory.CreateDirectory(folder);
            var zip = Path.Combine(folder, $"saves-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            ZipFile.CreateFromDirectory(saves, zip, CompressionLevel.Optimal, false);
            context.Send(new
            {
                type = "backupDone",
                path = zip
            });
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "toast",
                message = "备份失败：" + ex.Message
            });
        }
    }
}
