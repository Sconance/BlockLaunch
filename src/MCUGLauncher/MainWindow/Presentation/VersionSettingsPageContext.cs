using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

internal sealed class VersionSettingsPageContext(
    MainWindow host,
    string key,
    string root,
    InstanceCatalog catalog,
    InstanceDescriptor descriptor,
    PackRequirements requirements,
    InlineDialog window,
    TextBlock title,
    StackPanel content)
{
    public MainWindow Host { get; } = host;
    public string Key { get; } = key;
    public string Root { get; } = root;
    public InstanceCatalog Catalog { get; } = catalog;
    public InstanceDescriptor Descriptor { get; } = descriptor;
    public PackRequirements Requirements { get; } = requirements;
    public InlineDialog Window { get; } = window;
    public TextBlock Title { get; } = title;
    public StackPanel Content { get; } = content;
    public Action<string> Render { get; set; } = _ => { };

    public string DataDirectory() =>
        (Descriptor.Launch ?? Host.prefs.Launch).Isolation == "共享根目录"
            ? Root
            : Catalog.DirectoryFor(Key);

    public Border VersionCard(UIElement child) => new()
    {
        Background = Brushes.White,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 12),
        Child = child
    };

    public Button Button(string label, RoutedEventHandler action)
    {
        var button = MainWindow.ActionButton(label, action);
        button.MinWidth = 112;
        button.FontSize = 12;
        button.Margin = new Thickness(0, 4, 10, 4);
        button.Padding = new Thickness(12, 8, 12, 8);
        return button;
    }

    public Grid Field(string label, Control value, Button? action = null)
    {
        var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
        Grid.SetColumn(value, 1);
        row.Children.Add(value);
        if (action != null)
        {
            Grid.SetColumn(action, 2);
            action.MinWidth = 80;
            action.Margin = new Thickness(8, 4, 0, 4);
            row.Children.Add(action);
        }

        return row;
    }

    public StackPanel Card(string name)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = name, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
        Content.Children.Add(VersionCard(stack));
        return stack;
    }

    public void SaveIdentity()
    {
        if (Host.busy)
            throw new IOException("请等待当前任务完成。");
        Host.RequireStopped();
        Catalog.Save(Descriptor);
        Title.Text = "版本设置 · " + Descriptor.DisplayName;
        Window.Title = Title.Text;
    }
}
