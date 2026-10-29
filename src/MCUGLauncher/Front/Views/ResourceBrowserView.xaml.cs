using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public partial class ResourceBrowserView : UserControl
{
    readonly ResourceBrowserViewModel model;
    bool initialized;

    public ResourceBrowserView(ResourceBrowserViewModel model)
    {
        this.model = model;
        InitializeComponent();
        DataContext = model;
        Loaded += async (_, _) =>
        {
            if (!IsVisible)
                return;
            if (initialized)
                return;
            initialized = true;
            await model.Search();
        };
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible)
                model.Cancel();
        };
        Unloaded += (_, _) => model.Cancel();
    }

    public void ShowResults()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        Results.SelectedItem = null;
        initialized = true;
        _ = model.Search();
    }

    void BackClick(object sender, RoutedEventArgs args)
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        Results.SelectedItem = null;
    }

    async void ReloadVersionsClick(object sender, RoutedEventArgs args) => await model.Select(model.Selected);

    async void SearchClick(object sender, RoutedEventArgs args) => await model.Search();
    async void PreviousClick(object sender, RoutedEventArgs args) => await model.Previous();
    async void NextClick(object sender, RoutedEventArgs args) => await model.Next();
    void CancelClick(object sender, RoutedEventArgs args) => model.Cancel();
    async void DownloadClick(object sender, RoutedEventArgs args)
    {
        if (MessageBox.Show(Window.GetWindow(this), "将所选资源加入真实下载队列？", "确认下载", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            await model.DownloadSelected();
    }
    async void ProjectChanged(object sender, SelectionChangedEventArgs args)
    {
        if (((ListBox)sender).SelectedItem is not FrontResourceItem item)
            return;
        DetailPanel.Visibility = Visibility.Visible;
        await model.Select(item);
    }

    async void SearchKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter)
            return;
        args.Handled = true;
        await model.Search();
    }
}
