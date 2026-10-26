namespace BlockLaunch;
public class Preferences
{
    public int SchemaVersion { get; set; } = 1;
    public string Root { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlockLaunch", "minecraft");
    public string Java { get; set; } = "";
    public string Player { get; set; } = "Player";
    public int Ram { get; set; } = 4096;
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public string LastVersion { get; set; } = "";
    public List<string> GameRoots { get; set; } = [];
    public LaunchSettings Launch { get; set; } = new();
    public string Accent { get; set; } = "蓝色";
    public string BackgroundPath { get; set; } = "";
    public double BackgroundOpacity { get; set; } = 0.22;
    public string BackgroundFit { get; set; } = "裁剪填充";
    public List<string> BackgroundImages { get; set; } = [];
    public string OfflineSkin { get; set; } = "";
    public bool PreferMicrosoft { get; set; }
    public bool PreferMua { get; set; }
    public NetworkSettings Network { get; set; } = new();
    public string ResourceDownloadDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "BlockLaunch");
    public bool DownloadsPaused { get; set; }
    public string PausedDownloadId { get; set; } = "";
    public bool ProtectSaves { get; set; } = true;
    public int SaveBackupCount { get; set; } = 5;
    public bool DarkMode { get; set; }
    public List<string> ExtraJavaPaths { get; set; } = [];
    public List<ServerFavorite> Servers { get; set; } = [];
}
public sealed class SettingsStore(string directory)
{
    public string? Notice { get; private set; }
    public Preferences Load()
    {
        var file = Path.Combine(directory, "settings.json"); if (!File.Exists(file)) return new() { SchemaVersion = 2 };
        try
        {
            var prefs = AtomicFile.ReadJson<Preferences>(file);
            using (var original = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file)))
                if (!original.RootElement.TryGetProperty("Launch", out _)) prefs.Launch = new LaunchSettings { AutoMemory = false, RamMb = prefs.Ram, Java = prefs.Java, Width = prefs.Width, Height = prefs.Height };
            if (prefs.SchemaVersion > 2) throw new NotSupportedException("配置来自更高版本，已保留原文件，请使用对应启动器。");
            if (string.IsNullOrWhiteSpace(prefs.Root) || prefs.Ram < 512 || prefs.Width < 320 || prefs.Height < 240) throw new InvalidDataException("配置值无效。");
            prefs.Root = Path.GetFullPath(prefs.Root);
            if (prefs.SchemaVersion < 2)
            {
                var backup = file + ".v1.backup"; if (!File.Exists(backup)) File.Copy(file, backup);
                prefs.SchemaVersion = 2; Save(prefs); Notice = "配置已迁移至 v2，原配置保留为 settings.json.v1.backup。";
            }
            return prefs;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException or ArgumentException)
        {
            var backup = file + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); File.Move(file, backup);
            var prefs = new Preferences { SchemaVersion = 2 }; Save(prefs); Notice = "损坏的配置已单独备份，已加载默认设置。"; return prefs;
        }
    }
    public void Save(Preferences prefs)
    {
        var file = Path.Combine(directory, "settings.json");
        if (File.Exists(file))
        {
            try { if (AtomicFile.ReadJson<Preferences>(file).SchemaVersion > 2) throw new NotSupportedException("不能覆盖更高版本的配置。"); }
            catch (System.Text.Json.JsonException) { throw new InvalidDataException("原配置损坏，请先重新打开启动器恢复配置。"); }
        }
        prefs.SchemaVersion = 2; AtomicFile.WriteJson(file, prefs);
    }
}
