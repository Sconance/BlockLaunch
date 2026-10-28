using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class VersionSettingsController
{
    readonly LauncherWorkspace workspace;
    internal VersionSettingsController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal InlineDialog? versionSettingsWindow;
    internal string versionInitialPage = "概览";
    internal void OpenVersionSection(string key, string section)
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

    internal static Style VersionMenuStyle() => VersionSettingsView.NavigationStyle();

    internal void OpenVersionSettings(string key)
    {
        try
        {
            OpenVersionSettingsCore(key);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(workspace.Window, ex.Message, "无法打开版本设置");
        }
    }

    internal void OpenVersionSettingsCore(string key)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        if (versionSettingsWindow != null)
        {
            versionSettingsWindow.Activate();
            return;
        }

        var root = workspace.LaunchCoordinator.prefs.Root;
        var catalog = new InstanceCatalog(root);
        var descriptor = catalog.GetOrCreate(key);
        var requirements = workspace.InstancePagesController.RequirementsFor(descriptor.LaunchProfileId);
        var view = new VersionSettingsView(workspace.Window, descriptor.DisplayName);
        var window = view.Window;
        versionSettingsWindow = window;
        window.Closed += (_, _) =>
        {
            versionSettingsWindow = null;
            workspace.InstancePagesController.RefreshInstanceViews();
        };
        workspace.ExperienceThemeController.ConfigureUtilityWindow(window);
        window.Closing += (_, e) =>
        {
            if (workspace.LaunchCoordinator.busy)
                e.Cancel = true;
        };

        // Navigation is wired after composition has populated the page registry.
        IReadOnlyDictionary<string, Action> pages = null!;
        void Render(string page)
        {
            view.Content.Children.Clear();
            view.Scroll.ScrollToTop();
            view.Header(descriptor, requirements);
            view.Content.Dispatcher.BeginInvoke(() => workspace.ExperienceThemeController.ThemeWindow(workspace.Window));
            pages[page]();
        }

        var context = new VersionSettingsPageContext(key, root, catalog, descriptor, requirements, view,
            () => (descriptor.Launch ?? workspace.LaunchCoordinator.prefs.Launch).Isolation == "共享根目录"
                ? root : catalog.DirectoryFor(key), Render);
        pages = workspace.CreateVersionSettingsPages(context);
        foreach (var item in new[]
        {
            ("◇", "概览"), ("⚙", "设置"), ("✎", "修改"), ("▦", "Mod 管理"), ("□", "导出")
        })
        {
            var nav = new RadioButton
            {
                Style = VersionMenuStyle(),
                Content = item.Item1 + "   " + item.Item2,
                GroupName = "VersionPages",
                Margin = new Thickness(0, 7, 0, 0),
                FontSize = 14
            };
            nav.Checked += (_, _) =>
            {
                try
                {
                    Render(item.Item2);
                }
                catch (Exception ex)
                {
                    view.Content.Children.Clear();
                    view.Content.Children.Add(FeatureUiElements.Hint(ex.Message));
                }
            };
            view.Menu.Children.Add(nav);
        }

        workspace.MenuExtensionsController.AttachExtensionButtons(LauncherMenuArea.VersionSettings, view.Menu, instanceId: key, lifetime: window);
        var selectedPage = versionInitialPage switch
        {
            "设置" => 1,
            "修改" => 2,
            "Mod 管理" => 3,
            "导出" => 4,
            _ => 0
        };
        ((RadioButton)view.Menu.Children[selectedPage]).IsChecked = true;
        window.ShowDialog();
    }
}
