using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    LoaderConfigurationView? loaderConfigurationView;
    LoaderConfigurationPresenter? loaderConfigurationPresenter;

    FrameworkElement BuildLoaderOverview(Border original)
    {
        var old = (StackPanel)original.Child;
        var localInstaller = old.Children.OfType<Button>().Single();
        old.Children.Remove(localInstaller);
        loaderConfigurationView = new(localInstaller, (Style)FindResource("Card"), (Style)FindResource("Primary"));
        loaderConfigurationPresenter = new(loaderConfigurationView, () => http, selection =>
        {
            try
            {
                RequireStopped();
                EnqueueDownload(selection.Loader + " · " + selection.Minecraft, selection.Minecraft,
                    new()
                    {
                        Version = selection.Minecraft,
                        Loader = selection.Loader,
                        LoaderVersion = selection.Version
                    });
            }
            catch (Exception ex) { InlineMessageBox.Show(ex.Message); }
        });
        Closed += (_, _) => loaderConfigurationPresenter.Dispose();
        return loaderConfigurationView;
    }

    void PrepareLoaderConfiguration() => loaderConfigurationPresenter!.Prepare(rows, downloadVersion);
    Task LoadConfigurationVersions() => loaderConfigurationPresenter!.LoadVersions();
    void QueueConfiguredLoader(string loader) => loaderConfigurationPresenter!.QueueSelection(loader);
}
