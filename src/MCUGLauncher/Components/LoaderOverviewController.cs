using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class LoaderOverviewController
{
    readonly Window resources;
    readonly Func<System.Net.Http.HttpClient> getHttp;
    readonly Func<(IReadOnlyList<VersionRow> Rows, string? Selected)> getVersions;
    readonly Action<LoaderSelection> queue;

    internal LoaderOverviewController(Window resources, Func<System.Net.Http.HttpClient> getHttp,
        Func<(IReadOnlyList<VersionRow> Rows, string? Selected)> getVersions, Action<LoaderSelection> queue)
    {
        this.resources = resources;
        this.getHttp = getHttp;
        this.getVersions = getVersions;
        this.queue = queue;
    }

    internal LoaderConfigurationView? loaderConfigurationView;
    internal LoaderConfigurationPresenter? loaderConfigurationPresenter;
    internal FrameworkElement BuildLoaderOverview(Border original)
    {
        var old = (StackPanel)original.Child;
        var localInstaller = old.Children.OfType<Button>().Single();
        old.Children.Remove(localInstaller);
        loaderConfigurationView = new(localInstaller, (Style)resources.FindResource("Card"), (Style)resources.FindResource("Primary"));
        loaderConfigurationPresenter = new(loaderConfigurationView, getHttp, queue);
        resources.Closed += (_, _) => loaderConfigurationPresenter.Dispose();
        return loaderConfigurationView;
    }

    internal void PrepareLoaderConfiguration()
    {
        var versions = getVersions();
        loaderConfigurationPresenter!.Prepare(versions.Rows, versions.Selected);
    }
    internal Task LoadConfigurationVersions() => loaderConfigurationPresenter!.LoadVersions();
    internal void QueueConfiguredLoader(string loader) => loaderConfigurationPresenter!.QueueSelection(loader);
}
