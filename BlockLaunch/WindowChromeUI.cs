using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
namespace BlockLaunch;
public partial class MainWindow
{
    void BuildWindowChrome()
    {
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.CanResize;
        WindowChrome.SetWindowChrome(this,new WindowChrome {CaptionHeight=48,ResizeBorderThickness=new Thickness(6),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(10),UseAeroCaptionButtons=false});
        var header=(DockPanel)((Grid)Content).Children[0]; var navigation=header.Children[header.Children.Count-1]; header.Children.Remove(navigation);
        var controls=new StackPanel {Tag="WindowControls",Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Stretch,Margin=new Thickness(0,0,4,0)}; DockPanel.SetDock(controls,Dock.Right); header.Children.Add(controls); header.Children.Add(navigation);
        WindowChrome.SetIsHitTestVisibleInChrome(controls,true);
        // Leave empty title-bar space to the native caption hit test. Windows then
        // owns drag-to-restore, snap, double-click maximize and monitor transitions.
        foreach(var control in Elements(navigation).Prepend(navigation).OfType<FrameworkElement>().Where(c=>c is RadioButton or Menu or MenuItem))
            WindowChrome.SetIsHitTestVisibleInChrome(control,true);
        Button Caption(string symbol,string name,RoutedEventHandler click,bool danger=false)
        {
            var button=new Button {Content=new TextBlock {Text=symbol,Foreground=Brushes.White,FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=12},ContentTemplate=null,FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=12,Width=42,Height=38,Padding=new Thickness(0),Margin=new Thickness(0,5,0,5),Foreground=Brushes.White,Background=Brushes.Transparent,ToolTip=name,Focusable=false};
            button.ClearValue(Control.BackgroundProperty);var style=new Style(typeof(Button),(Style)FindResource(typeof(Button)));style.Setters.Add(new Setter(Control.BackgroundProperty,Brushes.Transparent));var hover=new Trigger {Property=IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Control.BackgroundProperty,danger ? new SolidColorBrush(Color.FromRgb(218,55,68)) : new SolidColorBrush(Color.FromArgb(48,255,255,255))));style.Triggers.Add(hover);button.Style=style;button.Click+=click;AutomationProperties.SetName(button,name);controls.Children.Add(button);return button;
        }
        Caption("\uE921","最小化",(_,_)=>WindowState=WindowState.Minimized);
        var maximize=Caption("\uE922","最大化或还原",(_,_)=>WindowState=WindowState==WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        Caption("\uE8BB","关闭启动器",(_,_)=>Close(),true);
        StateChanged+=(_,_)=>((TextBlock)maximize.Content).Text=WindowState==WindowState.Maximized ? "\uE923" : "\uE922";
        // WindowChrome calculates the work area on the active monitor; do not cap
        // the window to the primary display's height.
    }
}

