namespace BlockLaunch;

public partial class MainWindow
{
    internal void UninstallInstance(string key) => instanceUninstallationController.Uninstall(key);
}
