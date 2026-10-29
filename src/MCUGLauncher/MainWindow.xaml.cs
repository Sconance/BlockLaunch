using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow : Window
{
    internal LauncherWorkspace Workspace { get; private set; } = null!;
    public LauncherMenuRegistry MenuPages => Workspace.MenuExtensionsController.MenuPages;

    public MainWindow() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Brand.StorageDirectory)) { }

    public MainWindow(string configurationDirectory)
    {
        InitializeComponent();
        LauncherPage[] pages = [HomePageView, InstallPageView, ResourcePageView, ContentManagementPageView, SettingsPageView, LogsPageView, TasksPageView, NavigationPageView, HeadingPageView];
        foreach (var page in pages)
            page.Materialize();
        Workspace = new LauncherWorkspace(this, configurationDirectory);
        foreach (var page in pages)
            page.PageAction += Workspace.RoutePageAction;
        Workspace.LaunchCoordinator.Initialize();
    }

    internal void RefreshInlineTitle(InlineDialog page) => Workspace.InlineHostController.RefreshInlineTitle(page);
    internal void PushInline(InlineDialog page) => Workspace.InlineHostController.PushInline(page);
    internal void ActivateInline(InlineDialog page) => Workspace.InlineHostController.ActivateInline(page);
    internal void PopInline(InlineDialog page) => Workspace.InlineHostController.PopInline(page);
    void CancelTaskClick(object sender, RoutedEventArgs args)
    {
        if (Workspace != null)
            Workspace.TaskRecoveryController.CancelTaskClick(sender, args);
    }
}
