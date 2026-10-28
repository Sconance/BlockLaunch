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
}
