using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BlockLaunch.Contracts;

namespace BlockLaunch.SharedUI;

public partial class DownloadTasksView : UserControl, IDisposable
{
    readonly DownloadTasksViewModel model;
    readonly DispatcherTimer timer;
    bool disposed;

    public DownloadTasksView(DownloadTasksViewModel model)
    {
        this.model = model;
        InitializeComponent();
        DataContext = model;
        timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Refresh(), Dispatcher);
        timer.Stop();
        Loaded += (_, _) =>
        {
            if (disposed)
                return;
            Refresh();
            timer.Start();
        };
        Unloaded += (_, _) => timer.Stop();
    }

    public void Refresh()
    {
        if (!disposed)
            model.Refresh();
    }

    void RefreshClick(object sender, RoutedEventArgs args) => Refresh();
    void PauseClick(object sender, RoutedEventArgs args) => Change(sender, FrontDownloadAction.Pause);
    void ResumeClick(object sender, RoutedEventArgs args) => Change(sender, FrontDownloadAction.Resume);
    void CancelClick(object sender, RoutedEventArgs args) => Change(sender, FrontDownloadAction.Cancel);

    void Change(object sender, FrontDownloadAction action)
    {
        if (!disposed && sender is FrameworkElement { DataContext: FrontDownloadTask task })
            model.Change(task.Id, action);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        timer.Stop();
        model.Dispose();
    }
}
