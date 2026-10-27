using System.IO;
using System.Windows;

namespace BlockLaunch;

public partial class MainWindow
{
    void SavePreferences()
    {
        if (!int.TryParse(Memory.Text, out var ram) || ram < 512 || ram > 131072)
            throw new Exception("内存必须在 512–131072 MB 之间。");
        if (!int.TryParse(GameWidth.Text, out var w) || !int.TryParse(GameHeight.Text, out var h) || w < 320 || h < 240 || w > 16384 || h > 16384)
            throw new Exception("请输入有效的窗口尺寸。");
        var root = GameDefaults.ResolveRoot(GameRoot.Text);
        GameRoot.Text = root;
        var java = JavaPath.Text.Trim().Trim('"');
        if (java.Length > 0 && !File.Exists(java))
            throw new Exception("指定的 Java 文件不存在。");
        prefs.Root = root;
        prefs.Java = java;
        prefs.Player = PlayerName.Text.Trim();
        prefs.Ram = ram;
        prefs.Width = w;
        prefs.Height = h;
        prefs.LastVersion = Versions.SelectedValue as string ?? "";
        prefs.PreferMicrosoft = AccountMode.SelectedIndex == 1;
        prefs.PreferMua = AccountMode.SelectedIndex == 2;
        prefs.Launch.RamMb = ram;
        prefs.Launch.Java = java;
        prefs.Launch.Width = w;
        prefs.Launch.Height = h;
        if (!prefs.GameRoots.Contains(root, StringComparer.OrdinalIgnoreCase))
            prefs.GameRoots.Add(root);
        settings.Save(prefs);
    }

    async void SaveClick(object s, RoutedEventArgs e) => await Run(async () =>
    {
        if (game is
            {
                HasExited: false
            })
            throw new Exception("请先关闭游戏再更改设置。");
        SavePreferences();
        SetupLauncher();
        await StartupRecovery();
        Log("设置已保存。");
    });
}
