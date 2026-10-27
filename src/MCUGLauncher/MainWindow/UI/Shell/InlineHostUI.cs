using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly List<InlineDialog> inlinePages = new();
    InlinePageHost? inlinePageHost;
    internal Grid? inlineHost;
    internal Button? managerCaptionBack;
    internal IReadOnlyList<InlineDialog> InlinePages => inlinePages;

    void BuildInlineHost() => inlinePageHost!.Build();
    internal void PushInline(InlineDialog page) => inlinePageHost!.Push(page);
    internal void PopInline(InlineDialog page) => inlinePageHost!.Pop(page);
    internal void ActivateInline(InlineDialog page) => inlinePageHost!.Activate(page);
    internal void RefreshInlineTitle(InlineDialog page) => inlinePageHost!.RefreshTitle(page);

    void ShowStartupProblem(string message)
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
        close.Click += (_, _) => Close();
        panel.Children.Add(close);
        Content = panel;
    }
}
