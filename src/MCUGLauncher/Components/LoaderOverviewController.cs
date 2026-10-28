using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class LoaderOverviewController
{
    readonly LauncherWorkspace workspace;
    internal LoaderOverviewController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal LoaderConfigurationView? loaderConfigurationView;
    internal LoaderConfigurationPresenter? loaderConfigurationPresenter;
    internal FrameworkElement BuildLoaderOverview(Border original)
    {
        var old = (StackPanel)original.Child;
        var localInstaller = old.Children.OfType<Button>().Single();
        old.Children.Remove(localInstaller);
        loaderConfigurationView = new(localInstaller, (Style)workspace.Window.FindResource("Card"), (Style)workspace.Window.FindResource("Primary"));
        loaderConfigurationPresenter = new(loaderConfigurationView, () => workspace.LaunchCoordinator.http, selection =>
        {
            try
            {
                workspace.ContentController.RequireStopped();
                workspace.DownloadQueueController.EnqueueDownload(selection.Loader + " · " + selection.Minecraft, selection.Minecraft, new()
                {
                    Version = selection.Minecraft,
                    Loader = selection.Loader,
                    LoaderVersion = selection.Version
                });
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }
        });
        workspace.Window.Closed += (_, _) => loaderConfigurationPresenter.Dispose();
        return loaderConfigurationView;
    }

    internal void PrepareLoaderConfiguration() => loaderConfigurationPresenter!.Prepare(workspace.LaunchCoordinator.rows, workspace.LaunchCoordinator.downloadVersion);
    internal Task LoadConfigurationVersions() => loaderConfigurationPresenter!.LoadVersions();
    internal void QueueConfiguredLoader(string loader) => loaderConfigurationPresenter!.QueueSelection(loader);
}
