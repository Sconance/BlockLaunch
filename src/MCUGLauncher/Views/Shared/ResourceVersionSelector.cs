using System.Windows.Controls;
using System.Windows.Markup;

namespace BlockLaunch;

/// <summary>Editable version picker with a real WPF text part and a separate dropdown button.</summary>
internal sealed class ResourceVersionSelector : ComboBox
{
    internal ResourceVersionSelector()
    {
        IsEditable = true;
        IsTextSearchEnabled = true;
        StaysOpenOnEdit = true;
        MinHeight = 34;
        Template = (ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ComboBox">
          <Grid>
            <Border Background="{DynamicResource ThemeInputBrush}" BorderBrush="{DynamicResource ThemeBorderBrush}" BorderThickness="1" CornerRadius="8"/>
            <TextBox x:Name="PART_EditableTextBox" Style="{x:Null}" Background="Transparent" BorderThickness="0" Foreground="{TemplateBinding Foreground}" Margin="10,6,32,6" VerticalContentAlignment="Center"/>
            <ToggleButton Width="30" HorizontalAlignment="Right" Focusable="False" IsChecked="{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}">
              <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="Transparent"><TextBlock Text="⌄" HorizontalAlignment="Center" VerticalAlignment="Center" Foreground="{DynamicResource ThemeTextBrush}"/></Border></ControlTemplate></ToggleButton.Template>
            </ToggleButton>
            <Popup x:Name="PART_Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False">
              <Border Background="{DynamicResource ThemeSurfaceBrush}" BorderBrush="{DynamicResource ThemeBorderBrush}" BorderThickness="1" CornerRadius="8" Padding="5" MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}" MaxHeight="320">
                <ScrollViewer><ItemsPresenter/></ScrollViewer>
              </Border>
            </Popup>
          </Grid>
        </ControlTemplate>
        """);
    }
}
