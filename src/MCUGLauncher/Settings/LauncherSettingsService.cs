using System.IO;

namespace BlockLaunch;

public sealed record LauncherSettingsInput(
    string Root, string Java, string Player, string Ram, string Width, string Height,
    string LastVersion, bool PreferMicrosoft, bool PreferMua);

public sealed class LauncherSettingsService(SettingsStore store)
{
    public void Save(Preferences prefs, LauncherSettingsInput input)
    {
        if (!int.TryParse(input.Ram, out var ram) || ram < 512 || ram > 131072)
            throw new ArgumentException("内存必须在 512–131072 MB 之间。");
        if (!int.TryParse(input.Width, out var width) || !int.TryParse(input.Height, out var height)
            || width < 320 || height < 240 || width > 16384 || height > 16384)
            throw new ArgumentException("请输入有效的窗口尺寸。");
        var root = GameDefaults.ResolveRoot(input.Root);
        var java = input.Java.Trim().Trim('"');
        if (java.Length > 0 && !File.Exists(java))
            throw new ArgumentException("指定的 Java 文件不存在。");
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
        store.Save(prefs);
    }
}
