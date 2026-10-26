using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace BlockLaunch;
public partial class MainWindow
{
    RadioButton? microsoftChip, offlineChip, muaChip;
    StackPanel? offlineIdentity, microsoftIdentity, muaIdentity;
    Button? microsoftSignIn, muaSignIn;
    void BuildHomeAccountPanel()
    {
        var old=(StackPanel)AccountMode.Parent; var card=(Border)old.Parent; var avatar=(Border)SkinAvatar.Parent;
        foreach(var child in new UIElement[]{AccountMode,PlayerName,AvatarPlayerName,AccountStatus,avatar}) old.Children.Remove(child);
        card.Child=null; card.Padding=new Thickness(16,12,16,14); card.CornerRadius=new CornerRadius(10);
        var grid=new Grid(); grid.RowDefinitions.Add(new(){Height=GridLength.Auto}); grid.RowDefinitions.Add(new()); card.Child=grid;
        var chips=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,8)};
        var style=(Style)System.Windows.Markup.XamlReader.Parse("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='RadioButton'><Setter Property='Foreground' Value='{DynamicResource UiSecondaryTextBrush}'/><Setter Property='Margin' Value='2,0'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='RadioButton'><Border Name='Surface' CornerRadius='15' Background='Transparent' Padding='9,7'><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Surface' Property='Background' Value='{DynamicResource AccentBrush}'/><Setter Property='Foreground' Value='White'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='Opacity' Value='0.8'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");
        style.Setters.Add(new Setter(ContentControl.ContentTemplateProperty,(DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' Foreground='{Binding Foreground, RelativeSource={RelativeSource AncestorType=RadioButton}}'/></DataTemplate>")));
        microsoftChip=new RadioButton {Content="◇  正版",GroupName="AccountLoginMode",Style=style}; offlineChip=new RadioButton {Content="♧  离线",GroupName="AccountLoginMode",Style=style};
        muaChip=new RadioButton {Content="MUA 登录",GroupName="AccountLoginMode",Style=style,FontSize=11};
        foreach(var chip in new[]{microsoftChip,offlineChip,muaChip}) {chip.SetBinding(IsEnabledProperty,new Binding(nameof(AccountMode.IsEnabled)){Source=AccountMode});chips.Children.Add(chip);}
        microsoftChip.Checked+=(_,_)=>{if(AccountMode.SelectedIndex!=1)AccountMode.SelectedIndex=1;}; offlineChip.Checked+=(_,_)=>{if(AccountMode.SelectedIndex!=0)AccountMode.SelectedIndex=0;};
        muaChip.Checked+=(_,_)=>{if(AccountMode.SelectedIndex!=2)AccountMode.SelectedIndex=2;};
        AccountMode.Visibility=Visibility.Collapsed; chips.Children.Add(AccountMode); grid.Children.Add(chips);
        var identity=new StackPanel {VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,10,0,12)}; Grid.SetRow(identity,1); grid.Children.Add(identity);
        avatar.Width=84;avatar.Height=84;avatar.Padding=new Thickness(5);avatar.Margin=new Thickness(0,6,0,12);avatar.CornerRadius=new CornerRadius(11);avatar.ToolTip="点击刷新角色头像";avatar.MouseLeftButtonUp+=(_,_)=>RefreshAvatar(true);identity.Children.Add(avatar);
        AvatarPlayerName.FontSize=20;AvatarPlayerName.MaxWidth=220;AvatarPlayerName.TextTrimming=TextTrimming.CharacterEllipsis;identity.Children.Add(AvatarPlayerName);
        AccountStatus.FontSize=11;AccountStatus.TextWrapping=TextWrapping.Wrap;AccountStatus.MaxWidth=220;AccountStatus.TextAlignment=TextAlignment.Center;AccountStatus.Margin=new Thickness(0,7,0,14);identity.Children.Add(AccountStatus);
        offlineIdentity=new StackPanel();offlineIdentity.Children.Add(new TextBlock {Text="离线角色名",FontSize=11,Foreground=Brushes.SlateGray}); PlayerName.ToolTip="离线模式使用的游戏名称：3–16 位英文字母、数字或下划线";offlineIdentity.Children.Add(PlayerName);offlineIdentity.Children.Add(new TextBlock {Text="仅用于离线游戏，不影响正版角色。",FontSize=10,Foreground=Brushes.SlateGray,TextWrapping=TextWrapping.Wrap});identity.Children.Add(offlineIdentity);
        microsoftIdentity=new StackPanel();microsoftSignIn=ActionButton("登录微软账号",async(_,_)=>await Run(Login,"微软正版登录"));microsoftIdentity.Children.Add(microsoftSignIn);
        var manage=ActionButton("账号设置",AccountManagementClick);manage.Background=Brushes.Transparent;manage.FontSize=11;microsoftIdentity.Children.Add(manage);identity.Children.Add(microsoftIdentity);
        muaIdentity=new StackPanel();muaSignIn=ActionButton("登录 MUA 账号",async(_,_)=>await Run(async()=>{await MuaLogin();},"MUA 登录"));muaIdentity.Children.Add(muaSignIn);muaIdentity.Children.Add(ActionButton("MUA 账号设置",MuaManagementClick));identity.Children.Add(muaIdentity);
        var home=(Grid)((TabItem)MainTabs.Items[0]).Content;
        var banner=home.Children.OfType<Border>().FirstOrDefault(b=>b!=card && Grid.GetColumn(b)==2);
        if(banner!=null && !File.Exists(prefs.BackgroundPath))
        {
            banner.SetResourceReference(Border.BackgroundProperty,"ThemeSurfaceBrush");banner.CornerRadius=new CornerRadius(10);
            banner.BorderBrush=new SolidColorBrush(Color.FromRgb(214,225,232));banner.BorderThickness=new Thickness(1);
            var welcome=new StackPanel {Margin=new Thickness(36),VerticalAlignment=VerticalAlignment.Center};
            welcome.Children.Add(new TextBlock {Text="MINECRAFT JAVA EDITION",FontSize=11,Foreground=new SolidColorBrush(Color.FromRgb(100,119,130)),Margin=new Thickness(0,0,0,14)});
            welcome.Children.Add(new TextBlock {Text=Brand.Name,FontSize=36,FontWeight=FontWeights.SemiBold,Foreground=new SolidColorBrush(Color.FromRgb(0,109,184))});
            welcome.Children.Add(new TextBlock {Text="选择版本，开始游戏。",FontSize=16,Margin=new Thickness(0,14,0,26)});
            welcome.Children.Add(new Border {Height=2,Width=48,HorizontalAlignment=HorizontalAlignment.Left,Background=new SolidColorBrush(Color.FromRgb(158,167,171)),Margin=new Thickness(0,0,0,22)});
            banner.Child=welcome;
        }
        UpdateAccountPanel();
    }
    void UpdateAccountPanel()
    {
        if(microsoftChip==null || offlineChip==null || offlineIdentity==null || microsoftIdentity==null) return;
        var onlineMode=AccountMode.SelectedIndex==1;var muaMode=AccountMode.SelectedIndex==2; microsoftChip.IsChecked=onlineMode;offlineChip.IsChecked=!onlineMode && !muaMode;if(muaChip!=null)muaChip.IsChecked=muaMode;
        offlineIdentity.Visibility=onlineMode || muaMode ? Visibility.Collapsed : Visibility.Visible; microsoftIdentity.Visibility=onlineMode ? Visibility.Visible : Visibility.Collapsed;
        if(muaIdentity!=null)muaIdentity.Visibility=muaMode ? Visibility.Visible : Visibility.Collapsed;
        if(muaSignIn!=null)muaSignIn.Content=muaAccount==null ? "登录 MUA 账号" : "切换 MUA 账号";
        microsoftSignIn!.Content=savedAccount!=null || online!=null ? "切换微软账号" : "登录微软账号";
    }
}



