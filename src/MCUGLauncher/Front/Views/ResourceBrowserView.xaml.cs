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
    bool resetting;

    void SetLayout(bool useCards)
    {
        cards = useCards;
        var panel = new FrameworkElementFactory(cards ? typeof(ResponsiveResourcePanel) : typeof(StackPanel));
        Results.ItemsPanel = new ItemsPanelTemplate(panel);
        ScrollViewer.SetCanContentScroll(Results, false);
        var container = new Style(typeof(ListBoxItem));
        container.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        container.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
        container.Setters.Add(new Setter(PaddingProperty, new Thickness(12)));
        container.Setters.Add(new Setter(MarginProperty, new Thickness(0, 0, 8, 8)));
        Results.ItemContainerStyle = container;
        Results.ItemTemplate = (DataTemplate)FindResource(cards ? "ResourceCardItem" : "ResourceListItem");
    }

    public ResourceBrowserView(ResourceBrowserViewModel model)
    {
        this.model = model;
        InitializeComponent();
        DataContext = model;
        SetLayout(true);
        SizeChanged += (_, _) => ArrangeDetails();
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

    public void ShowDownloadCategory(string category)
    {
        ResourceKindField.Visibility = Visibility.Collapsed;
        CategoryTitle.Text = category == "Mod" ? "模组" : category;
        CategorySubtitle.Text = "发现、下载和收藏 Minecraft " + CategoryTitle.Text + "。";
        CategoryMark.Text = category switch { "整合包" => "▦", "数据包" => "≡", "资源包" => "▧", "光影包" => "☼", _ => "✚" };
        SearchQuery.ToolTip = "搜索" + CategoryTitle.Text + "名称、作者或描述…";
    }

    public void ShowContentCenter()
    {
        ResourceKindField.Visibility = Visibility.Visible;
        CategoryTitle.Text = "内容中心";
        CategorySubtitle.Text = "发现、下载和收藏 Minecraft 资源。";
        CategoryMark.Text = "✚";
    }

    void ShowDetail(bool visible)
    {
        DetailPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        SearchPanel.Visibility = SearchStatus.Visibility = Results.Visibility = PagingPanel.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        if (visible)
            ArrangeDetails();
    }

    void ArrangeDetails()
    {
        var stacked = ActualWidth < 760;
        DetailColumns.ColumnDefinitions[1].Width = stacked ? new GridLength(0) : new GridLength(270);
        DetailColumns.RowDefinitions[1].Height = stacked ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(DownloadOptions, stacked ? 0 : 1);
        Grid.SetRow(DownloadOptions, stacked ? 1 : 0);
    }

    void CardsClick(object sender, RoutedEventArgs args) => SetLayout(true);
    void ListClick(object sender, RoutedEventArgs args) => SetLayout(false);
    async void FavoritesClick(object sender, RoutedEventArgs args) => await model.Search();
    async void ClearFavoritesClick(object sender, RoutedEventArgs args) { model.FavoritesOnly = false; await model.Search(); }
    async void ResetClick(object sender, RoutedEventArgs args)
    {
        resetting = true;
        try
        {
            model.QueryText = model.GameVersion = model.Loader = model.Channel = "";
            model.Source = "modrinth";
            model.Sort = "relevance";
            model.FavoritesOnly = false;
        }
        finally { resetting = false; }
        await model.Search();
    }
    async void SortChanged(object sender, SelectionChangedEventArgs args) { if (initialized && IsVisible && !resetting) await model.Search(); }
    async void KindChanged(object sender, SelectionChangedEventArgs args) { if (initialized && IsVisible && ResourceKindField.Visibility == Visibility.Visible) await model.Search(); }
    void CardFavoriteClick(object sender, RoutedEventArgs args)
    {
        args.Handled = true;
        if (sender is Button { DataContext: FrontResourceItem item })
            model.ToggleFavorite(item);
    }
    void WebsiteClick(object sender, RoutedEventArgs args)
    {
        if (!Uri.TryCreate(model.ProjectWebsite, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.Host != "modrinth.com")
            return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception exception) { MessageBox.Show(Window.GetWindow(this), exception.Message, "无法打开项目主页"); }
    }

    public void ShowResults()
    {
        ShowDetail(false);
        Results.SelectedItem = null;
        initialized = true;
        _ = model.Search();
    }

    void BackClick(object sender, RoutedEventArgs args)
    {
        ShowDetail(false);
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
        ShowDetail(true);
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
