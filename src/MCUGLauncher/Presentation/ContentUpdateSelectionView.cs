using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class ContentUpdateSelectionView : IContentUpdateSelection
{
    readonly InlineDialog dialog;
    readonly Func<string, StackPanel, InlineDialog> createDialog;
    readonly Func<string, RoutedEventHandler, Button> createButton;

    public InlineDialog Dialog => dialog;

    public ContentUpdateSelectionView(
        Func<string, StackPanel, InlineDialog> createDialog,
        Func<string, RoutedEventHandler, Button> createButton,
        object owner,
        IEnumerable<ContentUpdate> updates,
        string? project,
        Func<IContentUpdateSelection, IReadOnlyList<ContentVersion>, Task> preview,
        Func<bool> isBusy)
    {
        this.createDialog = createDialog;
        this.createButton = createButton;
        var panel = new StackPanel();
        panel.Children.Add(Heading("选择更新内容"));
        panel.Children.Add(Hint("仅检查已管理的 Modrinth 内容。先勾选项目，再预览版本、必需依赖和文件变化。"));
        var selected = new List<(CheckBox Box, ContentVersion Version)>();
        foreach (var update in updates.Where(update => project == null || update.Current.ProjectId == project))
        {
            var box = new CheckBox
            {
                Content = new TextBlock { Text = update.Label, TextWrapping = TextWrapping.Wrap },
                IsEnabled = update.Latest != null,
                IsChecked = update.Latest != null,
                Margin = new Thickness(0, 5, 0, 8)
            };
            panel.Children.Add(box);
            if (update.Latest != null)
                selected.Add((box, update.Latest));
        }

        if (selected.Count == 0)
            panel.Children.Add(Hint("没有可选择的兼容正式版更新。"));
        dialog = createDialog("内容更新", panel);
        dialog.Owner = owner;
        var choices = new WrapPanel();
        choices.Children.Add(createButton("全选", (_, _) => selected.ForEach(item => item.Box.IsChecked = true)));
        choices.Children.Add(createButton("全不选", (_, _) => selected.ForEach(item => item.Box.IsChecked = false)));
        panel.Children.Add(choices);
        panel.Children.Add(createButton("预览所选更新", async (_, _) =>
            await preview(this, selected.Where(item => item.Box.IsChecked == true).Select(item => item.Version).ToList())));
        dialog.Closing += (_, e) =>
        {
            if (isBusy())
                e.Cancel = true;
        };
    }

    public void Show() => dialog.ShowDialog();

    public void Close() => dialog.Close();

    public bool ConfirmPreview(ContentUpdatePreview preview) =>
        new ContentUpdatePreviewView(createDialog, createButton, dialog, preview).Show();

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
        Foreground = System.Windows.Media.Brushes.SlateGray,
        Margin = new Thickness(0, 5, 0, 10)
    };
}
