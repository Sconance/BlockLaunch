using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlockLaunch;
internal sealed class InlineHostController
{
    readonly LauncherWorkspace workspace;
    internal InlineHostController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal readonly List<InlineDialog> inlinePages = new();
    internal readonly Dictionary<InlineDialog, IInputElement?> inlineReturnFocus = new();
    internal Grid? inlineHost;
    internal ContentControl? inlineContent;
    internal TextBlock? inlineTitle;
    internal Button? managerCaptionBack;
    internal UIElement? underlyingPage;
    internal IReadOnlyList<InlineDialog> InlinePages => inlinePages;

    internal void BuildInlineHost()
    {
        var root = (Grid)workspace.Window.Content;
        underlyingPage = root.Children.OfType<Grid>().First(g => Grid.GetRow(g) == 1);
        inlineHost = new Grid
        {
            Visibility = Visibility.Collapsed,
            Background = workspace.Window.Background,
            Margin = new Thickness(16, 0, 16, 10)
        };
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
        back.Click += (_, _) => inlinePages.LastOrDefault()?.Close();
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
        managerCaptionBack.Click += (_, _) => inlinePages.LastOrDefault()?.Close();
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
        workspace.Window.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && inlinePages.Count > 0)
            {
                e.Handled = true;
                if (InlineEscapeHandler.DismissSelector(inlinePages[^1]))
                    return;
                inlinePages[^1].Close();
            }
        };
        workspace.Window.Closing += (_, _) =>
        {
            foreach (var page in inlinePages.ToList().AsEnumerable().Reverse())
                page.ForceClose();
        };
        ((DockPanel)root.Children[0]).PreviewMouseDown += (_, e) =>
        {
            if (inlinePages.Count != 1 || !(object.Equals(inlinePages[0].Tag, "SketchManager") || object.Equals(inlinePages[0].Tag, "DownloadTasks")))
                return;
            DependencyObject? node = e.OriginalSource as DependencyObject;
            while (node != null && node is not RadioButton && node is not MenuItem)
                node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
            if (node is RadioButton radio)
            {
                inlinePages[0].Close();
                if (radio == workspace.Controls.HomeNav)
                    workspace.Window.MainTabs.SelectedIndex = 0;
                else if (radio.Tag?.ToString() == "1")
                    workspace.Window.MainTabs.SelectedIndex = 1;
                else if (radio.Tag?.ToString() == "4")
                    workspace.Window.MainTabs.SelectedIndex = 4;
            }
            else if (node is MenuItem)
                inlinePages[0].Close();
        };
    }

    internal void PushInline(InlineDialog page)
    {
        if (inlineHost == null)
            BuildInlineHost();
        if (inlinePages.Contains(page))
        {
            ActivateInline(page);
            return;
        }

        inlineReturnFocus[page] = Keyboard.FocusedElement;
        inlineContent!.Content = null;
        inlinePages.Add(page);
        RenderInline();
    }

    internal void PopInline(InlineDialog page)
    {
        if (!inlinePages.Remove(page))
            return;
        if (object.ReferenceEquals(inlineContent?.Content, page))
            inlineContent.Content = null;
        RenderInline();
        if (inlineReturnFocus.Remove(page, out var previous) && previous is UIElement element)
            workspace.Window.Dispatcher.BeginInvoke(() =>
            {
                if (element.IsVisible && element.IsEnabled)
                    element.Focus();
            });
    }

    internal void ActivateInline(InlineDialog page)
    {
        if (inlinePages.LastOrDefault() == page)
            page.Focus();
    }

    internal void RefreshInlineTitle(InlineDialog page)
    {
        if (inlinePages.LastOrDefault() == page && inlineTitle != null)
            inlineTitle.Text = page.Title;
    }

    internal void RenderInline()
    {
        var current = inlinePages.LastOrDefault();
        var manager = current != null && object.Equals(current.Tag, "SketchManager");
        inlineHost!.RowDefinitions[0].Height = new GridLength(manager ? 8 : 54);
        inlineHost.Children[0].Visibility = manager ? Visibility.Collapsed : Visibility.Visible;
        managerCaptionBack!.Visibility = manager ? Visibility.Visible : Visibility.Collapsed;
        var caption = (DockPanel)((Grid)workspace.Window.Content).Children[0];
        foreach (var logo in caption.Children.OfType<Image>())
            logo.Visibility = manager ? Visibility.Collapsed : Visibility.Visible;
        if (caption.Children.OfType<TextBlock>().FirstOrDefault() is { } brand)
            brand.Text = manager ? "版本内容管理" : Brand.Name;
        inlineContent!.Content = current;
        inlineHost!.Visibility = current == null ? Visibility.Collapsed : Visibility.Visible;
        if (underlyingPage != null)
            underlyingPage.Visibility = current == null ? Visibility.Visible : Visibility.Collapsed;
        // Keep the caption controls available. Leaving a modal editor uses its explicit Back action.
        foreach (var item in ((DockPanel)((Grid)workspace.Window.Content).Children[0]).Children.OfType<StackPanel>().Where(item => !object.Equals(item.Tag, "WindowControls")))
            item.IsEnabled = current == null || (inlinePages.Count == 1 && (object.Equals(current.Tag, "SketchManager") || object.Equals(current.Tag, "DownloadTasks")));
        if (current != null)
        {
            inlineTitle!.Text = current.Title;
            inlineHost.Background = workspace.Window.Background;
            workspace.Window.Dispatcher.BeginInvoke(() =>
            {
                workspace.ExperienceThemeController.ThemeWindow(workspace.Window);
                current.Focus();
            });
        }
    }

    internal void ShowStartupProblem(string message)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(32),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new TextBlock { Text = Brand.Name + " 无法启动", FontSize = 23, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 20) });
        var close = new Button
        {
            Content = "关闭启动器",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 140
        };
        close.Click += (_, _) => workspace.Window.Close();
        panel.Children.Add(close);
        workspace.Window.Content = panel;
    }
}
