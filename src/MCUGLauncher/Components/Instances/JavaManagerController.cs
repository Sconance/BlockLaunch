namespace BlockLaunch;

internal sealed class JavaManagerController(LauncherWorkspace workspace)
{
    internal void OpenJavaManager(string root, string profile, Action<string> select, object? owner = null) =>
        SettingsDialogPresenter.Show("Java 管理", owner ?? workspace.Window,
            close => CreateJavaManagerPage(root, profile, select, close, owner),
            workspace.ExperienceThemeController.ConfigureUtilityWindow);

    internal SettingsPageContent CreateJavaManagerPage(string root, string profile, Action<string> select,
        Action close, object? owner = null) => new JavaSettingsPage(
            () => workspace.LaunchCoordinator.prefs,
            () => workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs),
            picker => picker.ShowDialog(owner ?? workspace.Window),
            picker => picker.ShowDialog(owner ?? workspace.Window)).Build(root, profile, select, close);
}
