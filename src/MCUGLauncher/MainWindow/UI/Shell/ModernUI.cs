using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public partial class MainWindow
{
    // UI-only tokens. Configuration and launcher services remain unchanged.
    void BuildModernUI()
    {
        Resources["UiCardRadius"] = new CornerRadius(10);
        Resources["UiPageSpacing"] = new Thickness(16);
        Resources["UiControlPadding"] = new Thickness(12, 7, 12, 7);
        var buttons = new Style(typeof(Button), (Style)FindResource(typeof(Button)));
        buttons.Setters.Add(new Setter(Control.PaddingProperty, Resources["UiControlPadding"]));
        buttons.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
        buttons.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 32.0));
        buttons.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 3, 6, 3)));
        Resources[typeof(Button)] = buttons;
        var root = (Grid)Content;
        root.RowDefinitions[0].Height = new GridLength(48);
        var header = (DockPanel)root.Children[0];
        var brand = header.Children.OfType<TextBlock>().First();
        brand.FontSize = 17;
        brand.Width = 146;
        var nav = header.Children.OfType<StackPanel>().First(p => !Equals(p.Tag, "WindowControls") && p.Visibility != Visibility.Collapsed);
        var style = (Style)System.Windows.Markup.XamlReader.Parse("""
        <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="RadioButton">
          <Setter Property="Foreground" Value="{DynamicResource ThemeTextBrush}"/><Setter Property="Margin" Value="3,6"/><Setter Property="Padding" Value="13,7"/>
          <Setter Property="ContentTemplate"><Setter.Value><DataTemplate><TextBlock Text="{Binding}" FontSize="12" Foreground="{Binding Foreground,RelativeSource={RelativeSource AncestorType=RadioButton}}"/></DataTemplate></Setter.Value></Setter>
          <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="RadioButton"><Border Name="Surface" CornerRadius="8" Background="Transparent" Padding="{TemplateBinding Padding}"><ContentPresenter VerticalAlignment="Center"/></Border><ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="{DynamicResource ThemeInputBrush}"/></Trigger>
            <Trigger Property="IsChecked" Value="True"><Setter TargetName="Surface" Property="Background" Value="{DynamicResource ThemeSelectedBrush}"/><Setter Property="Foreground" Value="{DynamicResource UiAccentTextBrush}"/><Setter Property="FontWeight" Value="SemiBold"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger>
            <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="{DynamicResource AccentBrush}"/><Setter TargetName="Surface" Property="BorderThickness" Value="1"/></Trigger>
          </ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
        </Style>
        """);
        foreach (var radio in nav.Children.OfType<RadioButton>())
            radio.Style = style;
        foreach (var hint in nav.Children.OfType<TextBlock>())
            hint.Visibility = Visibility.Collapsed;
        if (moreMenu.Header is TextBlock more)
        {
            more.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
            more.FontSize = 12;
        }

        moreMenu.Padding = new Thickness(12, 7, 12, 7);
        if (sketchHome != null)
        {
            sketchHome.ColumnDefinitions[0].Width = new GridLength(224);
            sketchHome.ColumnDefinitions[1].Width = new GridLength(16);
            var left = (DockPanel)sketchHome.Children[0];
            var launch = (StackPanel)left.Children[0];
            var accountScroll = (ScrollViewer)left.Children[1];
            left.Children.Clear();
            DockPanel.SetDock(accountScroll, Dock.Top);
            DockPanel.SetDock(launch, Dock.Top);
            accountScroll.VerticalAlignment = VerticalAlignment.Top;
            left.Children.Add(accountScroll);
            left.Children.Add(launch);
            left.Children.Add(new Border());
            if (accountScroll.Content is Border card && card.Child is Grid account)
            {
                card.Padding = new Thickness(12);
                card.CornerRadius = new CornerRadius(10);
                card.VerticalAlignment = VerticalAlignment.Top;
                account.RowDefinitions[1].Height = GridLength.Auto;
                var identity = (StackPanel)account.Children[1];
                identity.VerticalAlignment = VerticalAlignment.Top;
                identity.Margin = new Thickness(0, 5, 0, 0);
                var avatar = (Border)SkinAvatarControl.Parent;
                avatar.Width = 64;
                avatar.Height = 64;
                avatar.Margin = new Thickness(0, 4, 0, 8);
                AvatarPlayerNameControl.FontSize = 17;
                AvatarPlayerNameControl.MaxWidth = 196;
                AvatarPlayerNameControl.ToolTip = AvatarPlayerNameControl.Text;
                AccountStatusControl.Margin = new Thickness(0, 4, 0, 9);
                AccountStatusControl.MaxWidth = 196;
                foreach (var chip in new[] {
                    microsoftChip,
                    offlineChip,
                    muaChip
                }

                )
                    if (chip != null)
                    {
                        chip.FontSize = 11;
                        chip.Margin = new Thickness(0);
                    }
            }

            LaunchGameButtonControl.Padding = new Thickness(10, 9, 10, 9);
            LaunchGameButtonControl.MinHeight = 70;
            LaunchGameButtonControl.Margin = new Thickness(0);
            homeSelectedLabel.MaxWidth = 125;
            homeSelectedLabel.TextTrimming = TextTrimming.CharacterEllipsis;
            homeSelectedLabel.TextWrapping = TextWrapping.NoWrap;
            homeSelectedLabel.ToolTip = VersionSelectorControl.SelectedItem is VersionRow selected ? selected.Label : "尚未选择版本";
        }

        SizeChanged += (_, _) =>
        {
            brand.Width = ActualWidth < 900 ? 118 : 146;
            brand.FontSize = ActualWidth < 900 ? 15 : 17;
            ApplyResponsiveLayout(ActualWidth, ActualHeight);
        };
        ApplyResponsiveLayout(Width, Height);
        ApplyModernChrome();
    }

    void ApplyResponsiveLayout(double width, double height)
    {
        if (Content is not Grid root || sketchHome == null)
            return;
        // Use WPF device-independent dimensions: OS DPI scaling still applies once.
        // Larger windows get larger typography and controls, while the remaining
        // space continues to be measured by the existing flexible layout.
        var scale = Math.Clamp(Math.Min(width / 1060.0, height / 740.0), 1.0, 1.4);
        foreach (var child in root.Children.OfType<FrameworkElement>())
            child.LayoutTransform = scale == 1 ? Transform.Identity : new ScaleTransform(scale, scale);
        root.RowDefinitions[0].Height = new GridLength(48 * scale);
        if (System.Windows.Shell.WindowChrome.GetWindowChrome(this) is
            {
            } chrome)
            chrome.CaptionHeight = 48 * scale;
    }

    internal void ApplyModernChrome()
    {
        ModernThemeStyler.Apply(this, sketchHome, prefs.DarkMode);
    }
}
