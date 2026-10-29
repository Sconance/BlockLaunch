using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    internal SettingsNavigationController SettingsNavigationController { get; private set; } = null!;

    internal void BuildSettingsNavigation()
    {
        var settingsTab = Window.SettingsPageView.PageHost!;
        var (original, aboutPage) = Window.SettingsPageView.TakeSections();
        settingsTab.Content = null;
        var view = new SettingsNavigationView((Style)Window.FindResource("Nav"));
        var host = new SettingsPageHost(view);
        string PageContext() => LaunchCoordinator.prefs.Root + "|" + Controls.Versions.SelectedValue;
        settingsTab.Content = view;
        var settingsNav = Window.NavigationPageView.SettingsNav;
        SettingsNavigationController = new(view, id =>
        {
            host.Show(id, PageContext());
            settingsNav.IsChecked = true;
            Window.MainTabs.SelectedItem = settingsTab;
        });
        void Complete() => SettingsNavigationController.SelectSettingsSection("launch");
        UIElement Detach(ContentControl tab)
        {
            var content = (UIElement)tab.Content;
            tab.Content = null;
            return content;
        }
        // Hosts are captured by their owning markup view, never rediscovered by tab position.
        var logsTab = Window.LogsPageView.PageHost!;
        var tasksTab = Window.TasksPageView.PageHost!;
        var diagnosticTab = DiagnosticsController.DiagnosticPage;
        var logPage = Detach(logsTab);
        var taskPage = Detach(tasksTab);
        var diagnosticPage = Detach(diagnosticTab);
        var sections = new List<SettingsSection>();
        void Add(string id, string title, string group, string glyph, Func<SettingsPageContent> factory)
        {
            sections.Add(new(id, title, group, glyph));
            host.Register(id, factory);
        }
        Add("launch", "启动设置", "启动与环境", "▷", () => new(original));
        Add("java", "Java 管理", "启动与环境", "♧", () => JavaManagerController.CreateJavaManagerPage(
            LaunchCoordinator.prefs.Root, Controls.Versions.SelectedValue as string ?? "", ApplySettingsJava, Complete));
        Add("network", "下载与网络", "启动与环境", "↓", () => NetworkController.CreateNetworkPage(Complete));
        Add("appearance", "界面与背景", "个性化", "◈", AppearanceController.CreateAppearancePage);
        Add("servers", "服务器收藏", "游戏工具", "◎", () => ExperienceController.CreateServersPage(Complete));
        Add("saves", "存档保护", "游戏工具", "▣", ExperienceController.CreateSaveProtectionPage);
        Add("downloads", "下载任务", "任务与诊断", "⇩", DownloadTaskController.CreateDownloadTasksPage);
        Add("tasks", "任务与恢复", "任务与诊断", "↻", () =>
        {
            TaskRecoveryController.RefreshOperations();
            return new(taskPage);
        });
        Add("logs", "运行日志", "任务与诊断", "≡", () => new(logPage));
        Add("diagnostics", "诊断与反馈", "任务与诊断", "◇", () => new(diagnosticPage));
        Add("about", "关于与许可", "关于", "ⓘ", () => new(aboutPage));

        var builtins = sections.ToArray();
        void RefreshExtensions(LauncherMenuArea area)
        {
            if (area is not (LauncherMenuArea.Settings or LauncherMenuArea.More))
                return;
            var all = builtins.ToList();
            foreach (var source in new[] { LauncherMenuArea.Settings, LauncherMenuArea.More })
                foreach (var extension in MenuExtensionsController.MenuPages.Pages(source))
                {
                    var id = $"extension:{source}:{extension.Id}";
                    view.CategoryIcons[id] = extension.Icon;
                    all.Add(new(id, extension.Title, source == LauncherMenuArea.Settings ? "扩展设置" : "工具扩展", extension.Glyph));
                    host.Register(id, () => new(MenuExtensionsController.CreateMenuContent(extension, null)));
                }
            SettingsNavigationController.SetSettingsSections(all);
        }
        MenuExtensionsController.MenuPages.Changed += RefreshExtensions;
        RefreshExtensions(LauncherMenuArea.Settings);
        Window.MainTabs.SelectionChanged += (_, args) =>
        {
            if (!ReferenceEquals(args.Source, Window.MainTabs))
                return;
            var selected = Window.MainTabs.SelectedItem;
            var id = ReferenceEquals(selected, logsTab) ? "logs"
                : ReferenceEquals(selected, tasksTab) ? "tasks"
                : ReferenceEquals(selected, diagnosticTab) ? "diagnostics" : null;
            if (id != null)
                SettingsNavigationController.SelectSettingsSection(id);
            else if (ReferenceEquals(selected, settingsTab))
                host.Show(SettingsNavigationController.SelectedSettingsSection, PageContext());
            else
                host.Dispose();
        };
        Window.Closed += (_, _) =>
        {
            MenuExtensionsController.MenuPages.Changed -= RefreshExtensions;
            SettingsNavigationController.Dispose();
            host.Dispose();
        };
    }

    void ApplySettingsJava(string path)
    {
        if (LaunchCoordinator.busy)
            throw new System.IO.IOException("请等待任务完成再更改 Java。");
        LaunchCoordinator.prefs.Java = path;
        LaunchCoordinator.prefs.Launch.Java = path;
        Controls.JavaPath.Text = path;
        LaunchCoordinator.settings.Save(LaunchCoordinator.prefs);
    }
}
