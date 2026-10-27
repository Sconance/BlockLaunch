using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BlockLaunch;

internal static class NavigationMenuStyle
{
    public static Style Create() => (Style)XamlReader.Parse("""
        <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="RadioButton">
          <Setter Property="Background" Value="{DynamicResource ThemeSurfaceBrush}"/><Setter Property="Foreground" Value="{DynamicResource ThemeTextBrush}"/>
          <Setter Property="BorderBrush" Value="Transparent"/><Setter Property="BorderThickness" Value="3,0,0,0"/>
          <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="RadioButton">
            <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" Padding="10,9"><ContentPresenter VerticalAlignment="Center"/></Border>
          </ControlTemplate></Setter.Value></Setter>
          <Style.Triggers><Trigger Property="IsChecked" Value="True"><Setter Property="Background" Value="{DynamicResource ThemeSelectedBrush}"/><Setter Property="Foreground" Value="#006DB8"/><Setter Property="BorderBrush" Value="#006DB8"/></Trigger></Style.Triggers>
        </Style>
        """);
}
