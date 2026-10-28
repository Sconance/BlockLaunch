using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class InlinePageHost(MainWindow host, List<InlineDialog> pages, RadioButton homeNavigation, TabControl mainTabs)
{
    readonly Dictionary<InlineDialog, IInputElement?> returnFocus = new();
    Grid? inlineHost;
    ContentControl? inlineContent;
    TextBlock? inlineTitle;
    Button? managerCaptionBack;
    UIElement? underlyingPage;

    public Grid? InlineHost => inlineHost;
    public IReadOnlyList<InlineDialog> Pages => pages;

    public void Build()
    {
        var root = (Grid)host.Content;
        underlyingPage = root.Children.OfType<Grid>().First(g => Grid.GetRow(g) == 1);
        inlineHost = new Grid
        {
            Visibility = Visibility.Collapsed,
            Background = host.Background,
            Margin = new Thickness(16, 0, 16, 10)
        };
        host.inlineHost = inlineHost;
        inlineHost.RowDefinitions.Add(new()
        {
            Height = new GridLength(54)
        });
        inlineHost.RowDefinitions.Add(new());
        var heading = new DockPanel
        {
            LastChildFill = true
        };
        var back = new Button
        {
            Content = "‹  返回",
            MinWidth = 82,
            Margin = new Thickness(0, 7, 14, 7),
            Padding = new Thickness(10, 5, 10, 5)
        };
        back.Click += (_, _) => pages.LastOrDefault()?.Close();
        heading.Children.Add(back);
        inlineTitle = new TextBlock
        {
            FontSize = 19,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        heading.Children.Add(inlineTitle);
        inlineHost.Children.Add(heading);
        managerCaptionBack = new Button
        {
            Content = "‹",
            ToolTip = "返回启动页",
            Width = 32,
            MinWidth = 0,
            FontSize = 23,
            Padding = new Thickness(0),
            Margin = new Thickness(8, 5, 0, 5),
            Visibility = Visibility.Collapsed
        };
        host.managerCaptionBack = managerCaptionBack;
        managerCaptionBack.Click += (_, _) => pages.LastOrDefault()?.Close();
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(managerCaptionBack, true);
        ((DockPanel)root.Children[0]).Children.Insert(0, managerCaptionBack);
        inlineContent = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRow(inlineContent, 1);
        inlineHost.Children.Add(inlineContent);
        Grid.SetRow(inlineHost, 1);
        root.Children.Add(inlineHost);
        host.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && pages.Count > 0)
            {
                e.Handled = true;
                pages[^1].Close();
            }
        };
        host.Closing += (_, _) =>
        {
            foreach (var page in pages.ToList().AsEnumerable().Reverse())
                page.ForceClose();
        };
        ((DockPanel)root.Children[0]).PreviewMouseDown += (_, e) =>
        {
            if (pages.Count != 1 || !(Equals(pages[0].Tag, "SketchManager") || Equals(pages[0].Tag, "DownloadTasks")))
                return;
            DependencyObject? node = e.OriginalSource as DependencyObject;
            while (node != null && node is not RadioButton && node is not MenuItem)
                node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
            if (node is RadioButton radio)
            {
                pages[0].Close();
                if (radio == homeNavigation)
                    mainTabs.SelectedIndex = 0;
                else if (radio.Tag?.ToString() == "1")
                    mainTabs.SelectedIndex = 1;
                else if (radio.Tag?.ToString() == "4")
                    mainTabs.SelectedIndex = 4;
            }
            else if (node is MenuItem)
                pages[0].Close();
        };
    }

    public void Push(InlineDialog page)
    {
        if (inlineHost == null)
            Build();
        if (pages.Contains(page))
        {
            Activate(page);
            return;
        }

        returnFocus[page] = Keyboard.FocusedElement;
        inlineContent!.Content = null;
        pages.Add(page);
        Render();
    }

    public void Pop(InlineDialog page)
    {
        if (!pages.Remove(page))
            return;
        if (ReferenceEquals(inlineContent?.Content, page))
            inlineContent.Content = null;
        Render();
        if (returnFocus.Remove(page, out var previous) && previous is UIElement element)
            host.Dispatcher.BeginInvoke(() =>
            {
                if (element.IsVisible && element.IsEnabled)
                    element.Focus();
            });
    }

    public void Activate(InlineDialog page)
    {
        if (pages.LastOrDefault() == page)
            page.Focus();
    }

    public void RefreshTitle(InlineDialog page)
    {
        if (pages.LastOrDefault() == page && inlineTitle != null)
            inlineTitle.Text = page.Title;
    }

    void Render()
    {
        var current = pages.LastOrDefault();
        var manager = current != null && Equals(current.Tag, "SketchManager");
        inlineHost!.RowDefinitions[0].Height = new GridLength(manager ? 8 : 54);
        inlineHost.Children[0].Visibility = manager ? Visibility.Collapsed : Visibility.Visible;
        managerCaptionBack!.Visibility = manager ? Visibility.Visible : Visibility.Collapsed;
        var caption = (DockPanel)((Grid)host.Content).Children[0];
        foreach (var logo in caption.Children.OfType<Image>())
            logo.Visibility = manager ? Visibility.Collapsed : Visibility.Visible;
        if (caption.Children.OfType<TextBlock>().FirstOrDefault() is
            {
            } brand)
            brand.Text = manager ? "版本内容管理" : Brand.Name;
        inlineContent!.Content = current;
        inlineHost!.Visibility = current == null ? Visibility.Collapsed : Visibility.Visible;
        if (underlyingPage != null)
            underlyingPage.Visibility = current == null ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in ((DockPanel)((Grid)host.Content).Children[0]).Children.OfType<StackPanel>().Where(item => !Equals(item.Tag, "WindowControls")))
            item.IsEnabled = current == null || (pages.Count == 1 && (Equals(current.Tag, "SketchManager") || Equals(current.Tag, "DownloadTasks")));
        if (current != null)
        {
            inlineTitle!.Text = current.Title;
            inlineHost.Background = host.Background;
            host.Dispatcher.BeginInvoke(() =>
            {
                host.ThemeWindow(host);
                current.Focus();
            });
        }
    }
}
