using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BlockLaunch;

/// <summary>The entire group heading is the toggle target, rather than just its arrow.</summary>
internal static class InstanceGroupExpanderStyle
{
    internal static Style Create() => (Style)XamlReader.Parse("""
    <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Expander">
      <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
      <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Expander"><DockPanel>
        <ToggleButton DockPanel.Dock="Top" IsChecked="{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" HorizontalContentAlignment="Stretch" Background="Transparent" BorderThickness="0" Padding="5,7">
          <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}"><DockPanel>
            <TextBlock x:Name="Arrow" DockPanel.Dock="Left" Text="▸" Margin="0,0,8,0"/>
            <ContentPresenter HorizontalAlignment="Stretch"/>
          </DockPanel></Border><ControlTemplate.Triggers>
            <Trigger Property="IsChecked" Value="True"><Setter TargetName="Arrow" Property="Text" Value="▾"/></Trigger>
            <Trigger Property="IsMouseOver" Value="True"><Setter Property="Background" Value="{DynamicResource ThemeSelectedBrush}"/></Trigger>
          </ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
          <ContentPresenter ContentSource="Header"/>
        </ToggleButton>
        <ContentPresenter x:Name="Body" ContentSource="Content"/>
      </DockPanel><ControlTemplate.Triggers><Trigger Property="IsExpanded" Value="False"><Setter TargetName="Body" Property="Visibility" Value="Collapsed"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    """);
}
