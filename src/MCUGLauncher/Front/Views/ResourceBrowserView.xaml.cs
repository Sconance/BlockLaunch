using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public partial class ResourceBrowserView : UserControl
{
    readonly ResourceBrowserViewModel model;
    bool initialized;
    bool cards;

    void LayoutClick(object sender, RoutedEventArgs args)
    {
        cards = !cards;
        var panel = new FrameworkElementFactory(cards ? typeof(WrapPanel) : typeof(StackPanel));
        if (cards)
            panel.SetValue(WrapPanel.ItemWidthProperty, 280d);
        Results.ItemsPanel = new ItemsPanelTemplate(panel);
        var container = new Style(typeof(ListBoxItem));
        container.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        container.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
        if (cards)
        {
            container.Setters.Add(new Setter(WidthProperty, 278d));
            container.Setters.Add(new Setter(MinHeightProperty, 150d));
        }
        Results.ItemContainerStyle = container;
        LayoutButton.Content = cards ? "切换列表视图" : "切换卡片视图";
    }

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
    void SaveFavoriteClick(object sender, RoutedEventArgs args) => model.SaveFavorite(false);
    void RemoveFavoriteClick(object sender, RoutedEventArgs args) => model.SaveFavorite(true);
    async void DownloadClick(object sender, RoutedEventArgs args)
    {
        if (MessageBox.Show(Window.GetWindow(this), "将所选资源加入真实下载队列？", "确认下载", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            await model.DownloadSelected();
    }
    async void InstallClick(object sender, RoutedEventArgs args)
    {
        await model.InstallSelected(summary => MessageBox.Show(Window.GetWindow(this), summary, "确认资源及依赖安装", MessageBoxButton.OKCancel) == MessageBoxResult.OK);
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
