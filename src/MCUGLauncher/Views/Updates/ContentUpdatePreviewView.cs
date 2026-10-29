using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class ContentUpdatePreviewView
{
    readonly InlineDialog dialog;

    public ContentUpdatePreviewView(
        Func<string, StackPanel, InlineDialog> createDialog,
        Func<string, RoutedEventHandler, Button> createButton,
        object owner,
        ContentUpdatePreview preview)
    {
        var details = new StackPanel();
        details.Children.Add(Heading("确认更新清单"));
        details.Children.Add(Hint($"{preview.Changes.Count} 项（含必需依赖） · 文件总量约 {preview.Bytes / 1024d / 1024:N1} MiB，已校验缓存可复用\n禁用状态会保留，原文件保留为回退备份。"));
        foreach (var change in preview.Changes)
            details.Children.Add(Hint(change.Label));
        dialog = createDialog("更新预览", details);
        dialog.Owner = owner;
        details.Children.Add(createButton("确认并加入下载队列", (_, _) => dialog.DialogResult = true));
    }

    public bool Show() => dialog.ShowDialog() == true;

    static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 5, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };

    static TextBlock Hint(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.SlateGray,
        Margin = new Thickness(0, 5, 0, 10)
    };
}
