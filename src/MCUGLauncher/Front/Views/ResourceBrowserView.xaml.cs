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

    async void SearchClick(object sender, RoutedEventArgs args) => await model.Search();
    async void PreviousClick(object sender, RoutedEventArgs args) => await model.Previous();
    async void NextClick(object sender, RoutedEventArgs args) => await model.Next();
    void CancelClick(object sender, RoutedEventArgs args) => model.Cancel();
    async void ProjectChanged(object sender, SelectionChangedEventArgs args) => await model.Select(((ListBox)sender).SelectedItem as FrontResourceItem);

    async void SearchKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter)
            return;
        args.Handled = true;
        await model.Search();
    }
}
