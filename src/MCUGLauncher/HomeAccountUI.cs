using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

public partial class MainWindow
{
    AccountPanelView? accountPanel;
    void BuildHomeAccountPanel()
    {
        // Reuse the named XAML controls: existing bindings and authentication handlers stay attached.
        var originalPanel = (StackPanel)AccountMode.Parent;
        var card = (Border)originalPanel.Parent;
        var avatar = (Border)SkinAvatar.Parent;
        var originalControls = new UIElement[] {
            AccountMode,
            PlayerName,
            AvatarPlayerName,
            AccountStatus,
            avatar
        };
        foreach (var child in originalControls)
        {
            originalPanel.Children.Remove(child);
        }

        card.Child = null;
        card.Padding = new Thickness(16, 12, 16, 14);
        card.CornerRadius = new CornerRadius(10);
        accountPanel = new AccountPanelView(
            new(AccountMode, PlayerName, AvatarPlayerName, AccountStatus, avatar, (Style)FindResource("AccountLoginChip")),
            new(() => RefreshAvatar(true),
                () => RunAccountAction(Login, "微软正版登录"),
                () => RunAccountAction(MuaLogin, "MUA 登录"),
                () => AccountManagementClick(this, new RoutedEventArgs()),
                () => MuaManagementClick(this, new RoutedEventArgs())));
        card.Child = accountPanel;
        ConfigureHomeWelcome(card);
        UpdateAccountPanel();
    }

    void ConfigureHomeWelcome(Border card)
    {
        var home = (Grid)((TabItem)MainTabs.Items[0]).Content;
        var banner = home.Children.OfType<Border>().FirstOrDefault(b => b != card && Grid.GetColumn(b) == 2);
        if (banner != null && !File.Exists(prefs.BackgroundPath))
        {
            banner.SetResourceReference(Border.BackgroundProperty, "ThemeSurfaceBrush");
            banner.CornerRadius = new CornerRadius(10);
            banner.BorderBrush = new SolidColorBrush(Color.FromRgb(214, 225, 232));
            banner.BorderThickness = new Thickness(1);
            banner.Child = new HomeWelcomeView(Brand.Name);
        }
    }

    void UpdateAccountPanel() => accountPanel?.UpdateState(savedAccount != null || online != null, muaAccount != null);
}
