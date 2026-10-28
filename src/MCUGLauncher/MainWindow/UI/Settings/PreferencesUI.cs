using System.Windows;

namespace BlockLaunch;

public partial class MainWindow
{
    internal void RefreshLaunchSettingsFields()
    {
        MemoryInput.Text = prefs.Ram.ToString();
        JavaPathInput.Text = prefs.Java;
        GameWidthInput.Text = prefs.Width.ToString();
        GameHeightInput.Text = prefs.Height.ToString();
    }

    void SavePreferences()
    {
        launcherSettings.Save(prefs, new(
            GameRootInput.Text, JavaPathInput.Text, PlayerNameControl.Text,
            MemoryInput.Text, GameWidthInput.Text, GameHeightInput.Text,
            VersionSelectorControl.SelectedValue as string ?? "",
            AccountModeControl.SelectedIndex == 1, AccountModeControl.SelectedIndex == 2));
        GameRootInput.Text = prefs.Root;
    }

    async void SaveClick(object s, RoutedEventArgs e) => await Run(async () =>
    {
        SavePreferences();
        SetupLauncher();
        await StartupRecovery();
        Log("设置已保存。");
    });
}
