using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;

/// <summary>Inline version-settings layout and reusable presentation elements only.</summary>
internal sealed class VersionSettingsView
{
    internal InlineDialog Window
    {
        get;
    }
    internal TextBlock Title
    {
        get;
    }
    internal StackPanel Menu
    {
        get;
    }
    internal StackPanel Content
    {
        get;
    }
    internal ScrollViewer Scroll
    {
        get;
    }

    internal VersionSettingsView(Window owner, string displayName)
    {
        var window = new InlineDialog
        {
            Owner = owner,
            Title = "版本设置 · " + displayName,
            Width = 920,
            Height = 710,
            MinWidth = 820,
            MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(226, 237, 250)),
            FontFamily = owner.FontFamily
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        layout.RowDefinitions.Add(new());
        window.Content = layout;
        var bar = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
            LastChildFill = true
        };
        bar.Visibility = Visibility.Collapsed;
        // The main window provides the shared title and Back action.
        var back = FeatureUiElements.ActionButton("← 返回", (_, _) => window.Close());
        back.Foreground = Brushes.White;
        back.Background = Brushes.Transparent;
        DockPanel.SetDock(back, Dock.Left);
        bar.Children.Add(back);
        var title = new TextBlock
        {
            Text = "版本设置 · " + displayName,
            Foreground = Brushes.White,
            FontSize = 17,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 12, 10, 12)
        };
        bar.Children.Add(title);
        layout.Children.Add(bar);
        var body = new Grid();
        body.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(150)
        });
        body.ColumnDefinitions.Add(new());
        Grid.SetRow(body, 1);
        layout.Children.Add(body);
        var menu = new StackPanel
        {
            Background = Brushes.White
        };
        body.Children.Add(menu);
        var content = new StackPanel
        {
            Margin = new Thickness(16)
        };
        var scroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(scroll, 1);
        body.Children.Add(scroll);

        Window = window;
        Title = title;
        Menu = menu;
        Content = content;
        Scroll = scroll;
    }

    internal static Border VersionCard(UIElement child) => new()
    {
        Background = Brushes.White,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 12),
        Child = child
    };

    internal static Button Button(string label, RoutedEventHandler action)
    {
        var button = FeatureUiElements.ActionButton(label, action);
        button.MinWidth = 112;
        button.FontSize = 12;
        button.Margin = new Thickness(0, 4, 10, 4);
        button.Padding = new Thickness(12, 8, 12, 8);
        return button;
    }

    internal static Grid Field(string label, Control value, Button? action = null)
    {
        var row = new Grid
        {
            Margin = new Thickness(0, 3, 0, 3)
        };
        row.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(64)
        });
        row.ColumnDefinitions.Add(new());
        row.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
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

    internal StackPanel Card(string name)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = name, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
        Content.Children.Add(VersionCard(stack));
        return stack;
    }

    internal void Header(InstanceDescriptor descriptor, PackRequirements requirements)
    {
        var row = new DockPanel();
        if (File.Exists(descriptor.IconPath))
        {
            var icon = new Image
            {
                Source = new BitmapImage(new Uri(descriptor.IconPath)),
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 14, 0)
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);
        }
        else
        {
            var icon = new TextBlock
            {
                Text = requirements.Loader == "vanilla" ? "▣" : "◇",
                FontSize = 33,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
                Margin = new Thickness(0, 0, 14, 0)
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);
        }

        var info = new StackPanel();
        info.Children.Add(new TextBlock { Text = descriptor.DisplayName, FontSize = 16, FontWeight = FontWeights.SemiBold });
        info.Children.Add(new TextBlock { Text = "Minecraft " + requirements.Minecraft + " · " + requirements.Loader + (requirements.LoaderVersion.Length > 0 ? " " + requirements.LoaderVersion : ""), FontSize = 11, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 3, 0, 0) });
        row.Children.Add(info);
        Content.Children.Add(VersionCard(row));
    }

    internal static Style NavigationStyle() => NavigationMenuStyle.Create();
}
