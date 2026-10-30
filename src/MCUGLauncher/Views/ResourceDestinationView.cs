using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BlockLaunch;

/// <summary>Mutually exclusive download/install controls. This view only emits selection commands.</summary>
internal sealed class ResourceDestinationView : StackPanel
{
    readonly RadioButton custom = new() { Content = "自定义下载", GroupName = "ResourceDestination", IsChecked = true };
    readonly RadioButton install = new() { Content = "安装到实例", GroupName = "ResourceDestination" };
    readonly StackPanel customPanel = new();
    readonly StackPanel instancePanel = new();
    readonly TextBlock directory = FeatureUiElements.Hint("");
    readonly ComboBox instances = new() { ToolTip = "选择安装目标实例" };
    bool updating;
    internal bool InstallToInstance => install.IsChecked == true;
    internal string? SelectedInstanceKey => (instances.SelectedItem as InstanceView)?.Key;
    internal event Action? DestinationChanged;

    internal ResourceDestinationView(Action chooseDirectory, Action openDirectory)
    {
        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(InstanceView.Name)));
        label.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        instances.ItemTemplate = new DataTemplate { VisualTree = label };
        var modes = new WrapPanel();
        custom.Margin = new Thickness(0, 0, 10, 6);
        install.Margin = new Thickness(0, 0, 0, 6);
        modes.Children.Add(custom);
        modes.Children.Add(install);
        Children.Add(modes);
        customPanel.Children.Add(directory);
        var actions = new WrapPanel();
        actions.Children.Add(FeatureUiElements.ActionButton("选择目录", (_, _) => chooseDirectory()));
        actions.Children.Add(FeatureUiElements.ActionButton("打开目录", (_, _) => openDirectory()));
        customPanel.Children.Add(actions);
        instancePanel.Children.Add(FeatureUiElements.Hint("目标实例"));
        instancePanel.Children.Add(instances);
        Children.Add(customPanel);
        Children.Add(instancePanel);
        custom.Checked += (_, _) => Changed();
        install.Checked += (_, _) => Changed();
        instances.SelectionChanged += (_, _) => { if (!updating && InstallToInstance) DestinationChanged?.Invoke(); };
        Render();
    }
    internal void Configure(IReadOnlyList<InstanceView> values, bool allowInstall, string path)
    {
        updating = true;
        try
        {
            var key = SelectedInstanceKey;
            instances.ItemsSource = values;
            instances.SelectedItem = values.FirstOrDefault(item => item.Key == key) ?? values.FirstOrDefault();
            install.Visibility = allowInstall ? Visibility.Visible : Visibility.Collapsed;
            install.IsEnabled = allowInstall && values.Count > 0;
            if (!install.IsEnabled)
                custom.IsChecked = true;
            SetDirectory(path);
            Render();
        }
        finally { updating = false; }
    }
    internal void SetDirectory(string path)
    {
        directory.Text = path;
        directory.ToolTip = path;
    }
    void Changed()
    {
        Render();
        if (!updating)
            DestinationChanged?.Invoke();
    }
    void Render()
    {
        customPanel.Visibility = InstallToInstance ? Visibility.Collapsed : Visibility.Visible;
        instancePanel.Visibility = InstallToInstance ? Visibility.Visible : Visibility.Collapsed;
    }
}
