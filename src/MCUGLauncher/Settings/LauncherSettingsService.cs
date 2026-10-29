using System.IO;
using System.Text.Json;

namespace BlockLaunch;

public sealed record LauncherSettingsInput(
    string Root, string Java, string Player, string Ram, string Width, string Height,
    string LastVersion, bool PreferMicrosoft, bool PreferMua);

public sealed class LauncherSettingsService(SettingsStore store, Func<bool>? isGameRunning = null)
{
    public void Save(Preferences prefs, LauncherSettingsInput input)
    {
        EnsureCanSave();
        if (!int.TryParse(input.Ram, out var ram) || ram < 512 || ram > 131072)
            throw new ArgumentException("内存必须在 512–131072 MB 之间。");
        if (!int.TryParse(input.Width, out var width) || !int.TryParse(input.Height, out var height)
            || width < 320 || height < 240 || width > 16384 || height > 16384)
            throw new ArgumentException("请输入有效的窗口尺寸。");
        var root = GameDefaults.ResolveRoot(input.Root);
        var java = input.Java.Trim().Trim('"');
        if (java.Length > 0 && !File.Exists(java))
            throw new ArgumentException("指定的 Java 文件不存在。");
        var pending = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(prefs))!;
        pending.Root = root;
        pending.Java = java;
        pending.Player = input.Player.Trim();
        pending.Ram = ram;
        pending.Width = width;
        pending.Height = height;
        pending.LastVersion = input.LastVersion;
        pending.PreferMicrosoft = input.PreferMicrosoft;
        pending.PreferMua = input.PreferMua;
        pending.Launch.RamMb = ram;
        pending.Launch.Java = java;
        pending.Launch.Width = width;
        pending.Launch.Height = height;
        if (!pending.GameRoots.Contains(root, StringComparer.OrdinalIgnoreCase))
            pending.GameRoots.Add(root);
        store.Save(pending);
        prefs.SchemaVersion = pending.SchemaVersion;
        prefs.Root = root;
        prefs.Java = java;
        prefs.Player = input.Player.Trim();
        prefs.Ram = ram;
        prefs.Width = width;
        prefs.Height = height;
        prefs.LastVersion = input.LastVersion;
        prefs.PreferMicrosoft = input.PreferMicrosoft;
        prefs.PreferMua = input.PreferMua;
        prefs.Launch.RamMb = ram;
        prefs.Launch.Java = java;
        prefs.Launch.Width = width;
        prefs.Launch.Height = height;
        if (!prefs.GameRoots.Contains(root, StringComparer.OrdinalIgnoreCase))
            prefs.GameRoots.Add(root);
    }

    public void EnsureCanSave()
    {
        if (isGameRunning?.Invoke() == true)
            throw new InvalidOperationException("请先关闭游戏再更改设置。");
    }

    public void SaveNetwork(Preferences prefs, NetworkSettings network)
    {
        EnsureCanSave();
        var value = JsonSerializer.Deserialize<NetworkSettings>(JsonSerializer.Serialize(network))!;
        value.ProxyAddress = value.ProxyAddress.Trim();
        value.Validate();
        Persist(prefs, pending => pending.Network = value);
        prefs.Network = value;
    }

    public void SaveDownloadLimit(Preferences prefs, int kibPerSecond)
    {
        EnsureCanSave();
        if (kibPerSecond is < 0 or > 1048576)
            throw new ArgumentException("下载限速必须在 0–1048576 KiB/s 之间。");
        var network = JsonSerializer.Deserialize<NetworkSettings>(JsonSerializer.Serialize(prefs.Network))!;
        network.LimitKibPerSecond = kibPerSecond;
        SaveNetwork(prefs, network);
    }

    public void SaveAppearance(Preferences prefs, bool darkMode, string accent, string backgroundPath,
        double opacity, string fit, IReadOnlyList<string> images)
    {
        EnsureCanSave();
        if (!double.IsFinite(opacity) || opacity is < 0 or > 0.65)
            throw new ArgumentException("背景透明度必须在 0–0.65 之间。");
        if (accent is not ("蓝色" or "地大蓝" or "青绿" or "石墨" or "紫罗兰"))
            throw new ArgumentException("主题色无效。");
        if (fit is not ("裁剪填充" or "完整显示" or "拉伸"))
            throw new ArgumentException("背景显示方式无效。");
        var selected = backgroundPath.Trim();
        if (selected.Length > 0 && (!File.Exists(selected) || new FileInfo(selected).Length > 40 * 1024 * 1024))
            throw new ArgumentException("背景图片不存在或大于 40 MB。");
        var choices = images.Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
        void Apply(Preferences target)
        {
            target.DarkMode = darkMode;
            target.Accent = accent;
            target.BackgroundPath = selected;
            target.BackgroundOpacity = opacity;
            target.BackgroundFit = fit;
            target.BackgroundImages = choices.ToList();
        }
        Persist(prefs, Apply);
        Apply(prefs);
    }

    public void SaveBackupPolicy(Preferences prefs, bool enabled, int count)
    {
        EnsureCanSave();
        if (count is < 1 or > 100)
            throw new ArgumentException("保留数量应为 1–100。");
        Persist(prefs, pending => { pending.ProtectSaves = enabled; pending.SaveBackupCount = count; });
        prefs.ProtectSaves = enabled;
        prefs.SaveBackupCount = count;
    }

    public void SaveGlobalLaunch(Preferences prefs, LaunchSettings settings)
    {
        EnsureCanSave();
        var value = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(settings))!;
        value.Validate();
        void Apply(Preferences target)
        {
            target.Launch = value;
            target.Java = value.Java;
            target.Ram = value.RamMb;
            target.Width = value.Width;
            target.Height = value.Height;
        }
        Persist(prefs, Apply);
        Apply(prefs);
    }

    void Persist(Preferences prefs, Action<Preferences> update)
    {
        var pending = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(prefs))!;
        update(pending);
        store.Save(pending);
        prefs.SchemaVersion = pending.SchemaVersion;
    }
}
