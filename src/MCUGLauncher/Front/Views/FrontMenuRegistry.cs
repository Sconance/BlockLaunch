using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public enum FrontMenuArea { Top, Download, Settings, More, ResourceDetails, Home, VersionSettings, InstanceContent }

public sealed record FrontMenuContext(Window Owner, IFrontLauncher Launcher, string? Instance, FrontResourceItem? Resource);
public sealed record FrontMenuPage(string Id, string Title, Func<FrontMenuContext, FrameworkElement> CreateContent, int Order = 100);

public sealed class FrontMenuRegistry
{
    readonly Dictionary<FrontMenuArea, List<FrontMenuPage>> pages = new();
    event Action? Changed;

    public void Register(FrontMenuArea area, FrontMenuPage page)
    {
        if (!Enum.IsDefined(area) || string.IsNullOrWhiteSpace(page.Id) || string.IsNullOrWhiteSpace(page.Title) || page.CreateContent == null)
            throw new ArgumentException("菜单需要有效区域、唯一 ID、标题和内容工厂。");
        if (!pages.TryGetValue(area, out var entries))
            pages[area] = entries = [];
        if (entries.Any(item => item.Id == page.Id))
            throw new ArgumentException("菜单 ID 已存在：" + page.Id);
        entries.Add(page);
        Changed?.Invoke();
    }

    public void Wrap(FrontMenuArea area, UserControl view, Func<FrontMenuContext> context)
    {
        var original = view.Content;
        view.Content = null;
        var body = new DockPanel();
        var actions = new WrapPanel();
        DockPanel.SetDock(actions, Dock.Top);
        body.Children.Add(actions);
        if (original is UIElement content)
            body.Children.Add(content);
        view.Content = body;
        Attach(area, actions, context);
    }

    public void Attach(FrontMenuArea area, Panel actions, Func<FrontMenuContext> context)
    {
        void Refresh()
        {
            foreach (var button in actions.Children.OfType<Button>().Where(button => button.Tag is FrontMenuPage).ToArray())
                actions.Children.Remove(button);
            if (!pages.TryGetValue(area, out var entries))
                return;
            foreach (var page in entries.OrderBy(item => item.Order).ThenBy(item => item.Title, StringComparer.Ordinal))
            {
                var button = new Button { Content = page.Title, Tag = page, Margin = new Thickness(4) };
                button.Click += (_, _) =>
                {
                    var current = context();
                    try
                    {
                        new Window
                        {
                            Owner = current.Owner, Title = page.Title, Width = 800, Height = 600,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Content = page.CreateContent(current)
                        }.ShowDialog();
                    }
                    catch (Exception exception) { MessageBox.Show(current.Owner, exception.Message, "扩展页面失败"); }
                };
                actions.Children.Add(button);
            }
        }
        actions.Loaded += (_, _) => { Changed -= Refresh; Changed += Refresh; Refresh(); };
        actions.Unloaded += (_, _) => Changed -= Refresh;
    }
}
