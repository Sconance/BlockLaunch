using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    internal InlineDialog? versionSettingsWindow;
    string versionInitialPage = "概览";

    static Style VersionMenuStyle() => NavigationMenuStyle.Create();

    void OpenVersionSection(string key, string section)
    {
        versionInitialPage = section;
        try
        {
            OpenVersionSettings(key);
        }
        finally
        {
            versionInitialPage = "概览";
        }
    }

    void OpenVersionSettings(string key)
    {
        try
        {
            new VersionSettingsDialog(this, key, versionInitialPage).Show();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(this, ex.Message, "无法打开版本设置");
        }
    }
}
