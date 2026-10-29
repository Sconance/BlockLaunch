using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    internal SettingsNavigationController SettingsNavigationController { get; private set; } = null!;

    internal void BuildSettingsNavigation()
    {
        var settingsTab = (TabItem)Window.MainTabs.Items[4];
        var original = (ScrollViewer)settingsTab.Content;
        var stack = (StackPanel)original.Content;
        var about = stack.Children.OfType<Border>().Last();
        stack.Children.Remove(about);
        foreach (var panel in stack.Children.OfType<Border>().Select(card => card.Child).OfType<StackPanel>())
            foreach (var button in panel.Children.OfType<Button>().Where(button => Equals(button.Content, "下载与网络设置")).ToArray())
                panel.Children.Remove(button);
        settingsTab.Content = null;
        var view = new SettingsNavigationView((Style)Window.FindResource("Nav"));
        var host = new SettingsPageHost(view);
        string PageContext() => LaunchCoordinator.prefs.Root + "|" + Controls.Versions.SelectedValue;
        var aboutPage = new ScrollViewer { Content = about, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        settingsTab.Content = view;
        var settingsNav = ((StackPanel)Controls.HomeNav.Parent).Children.OfType<RadioButton>().Single(button => button.Tag?.ToString() == "4");
        SettingsNavigationController = new(view, id =>
        {
            host.Show(id, PageContext());
            settingsNav.IsChecked = true;
            Window.MainTabs.SelectedIndex = 4;
        });
        SettingsPageContent Tool(Func<InlineDialog> create) => SettingsPageContent.FromDialog(create(),
            () => SettingsNavigationController.SelectSettingsSection("launch"));
        UIElement Detach(int index)
        {
            var tab = (TabItem)Window.MainTabs.Items[index];
            var content = (UIElement)tab.Content;
            tab.Content = null;
            return content;
        }
        // Original controls and their event handlers are reused; only their host changes.
        var logPage = Detach(5);
        var taskPage = Detach(6);
        var diagnosticPage = Detach(DiagnosticsController.diagnosticTabIndex);
        var sections = new List<SettingsSection>();
        void Add(string id, string title, string group, string glyph, Func<SettingsPageContent> factory)
        {
            sections.Add(new(id, title, group, glyph));
            host.Register(id, factory);
        }
        Add("launch", "启动设置", "启动与环境", "▷", () => new(original));
        Add("java", "Java 管理", "启动与环境", "♧", () => Tool(() => JavaManagerController.CreateJavaManagerPage(
            LaunchCoordinator.prefs.Root, Controls.Versions.SelectedValue as string ?? "", ApplySettingsJava)));
        Add("network", "下载与网络", "启动与环境", "↓", () => Tool(NetworkController.CreateNetworkPage));
        Add("appearance", "界面与背景", "个性化", "◈", () => Tool(AppearanceController.CreateAppearancePage));
        Add("servers", "服务器收藏", "游戏工具", "◎", () => Tool(ExperienceController.CreateServersPage));
        Add("saves", "存档保护", "游戏工具", "▣", () => Tool(ExperienceController.CreateSaveProtectionPage));
        Add("downloads", "下载任务", "任务与诊断", "⇩", () => Tool(DownloadTaskController.CreateDownloadTasksPage));
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
            var id = Window.MainTabs.SelectedIndex switch
            {
                5 => "logs",
                6 => "tasks",
                _ when Window.MainTabs.SelectedIndex == DiagnosticsController.diagnosticTabIndex => "diagnostics",
                _ => null
            };
            if (id != null)
                SettingsNavigationController.SelectSettingsSection(id);
            else if (Window.MainTabs.SelectedIndex == 4)
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
