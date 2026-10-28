using System.Windows;

namespace BlockLaunch;

internal sealed class VersionSettingsDialog(MainWindow host, string key, string initialPage)
{
    readonly string root = host.prefs.Root;
    readonly InstanceCatalog catalog = new(host.prefs.Root);
    InstanceDescriptor descriptor = null!;
    PackRequirements requirements = null!;
    InlineDialog window = null!;
    VersionSettingsWindowView view = null!;
    VersionSettingsPageContext context = null!;

    public void Show()
    {
        if (host.busy)
            return;
        if (host.versionSettingsWindow != null)
        {
            host.versionSettingsWindow.Activate();
            return;
        }

        descriptor = host.versionManagement.Descriptor(root, key);
        requirements = host.versionManagement.Requirements(root, descriptor.LaunchProfileId);
        view = new VersionSettingsWindowView(host, descriptor.DisplayName, descriptor, requirements, Render, () => window.Close());
        window = view.Window;
        host.versionSettingsWindow = window;
        window.Closed += (_, _) =>
        {
            host.versionSettingsWindow = null;
            host.RefreshInstanceViews();
        };
        host.ConfigureUtilityWindow(window);
        window.Closing += (_, e) =>
        {
            if (host.busy)
                e.Cancel = true;
        };

        context = new VersionSettingsPageContext(host, key, root, catalog, descriptor, requirements, window, view.Title, view.Content);
        context.Render = Render;
        void RefreshExtensions(LauncherMenuArea area)
        {
            if (area == LauncherMenuArea.VersionSettings)
                RefreshExtensionPages();
        }
        void RefreshExtensionPages()
        {
            view.RefreshExtensionPages(host.MenuPages.Pages(LauncherMenuArea.VersionSettings), ShowExtension);
        }
        void ShowExtension(LauncherMenuPage page)
        {
            FrameworkElement content;
            try
            {
                content = page.CreateContent(new(host, key, null))
                    ?? MainWindow.Hint("栏目未提供页面内容。");
            }
            catch (Exception ex)
            {
                content = MainWindow.Hint("无法加载“" + page.Title + "”：" + ex.Message);
            }
            view.ShowExtensionPage(page.Title, content);
        }
        host.MenuPages.Changed += RefreshExtensions;
        window.Closed += (_, _) => host.MenuPages.Changed -= RefreshExtensions;
        RefreshExtensionPages();
        view.SelectPage(initialPage);
        window.ShowDialog();
    }

    void Render(string page)
    {
        view.BeginPage();
        view.Content.Dispatcher.BeginInvoke(() => host.ThemeWindow(host));
        switch (page)
        {
            case "概览":
                new VersionOverviewPageBuilder(context).Build();
                break;
            case "设置":
                new VersionLaunchPageBuilder(context).Build();
                break;
            case "修改":
                new VersionLoaderPageBuilder(context).Build();
                break;
            case "Mod 管理":
                new VersionModManagementPageBuilder(context).Build();
                break;
            default:
                new VersionPackExportPageBuilder(context).Build();
                new VersionLaunchScriptExportPageBuilder(context).Build();
                break;
        }
    }
}
