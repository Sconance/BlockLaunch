using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;
internal sealed class HomeAccountController
{
    readonly LauncherWorkspace workspace;
    internal HomeAccountController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal AccountPanelView? accountPanel;
    internal void BuildHomeAccountPanel()
    {
        // Reuse the named XAML controls: existing bindings and authentication handlers stay attached.
        var originalPanel = (StackPanel)workspace.Controls.AccountMode.Parent;
        var card = (Border)originalPanel.Parent;
        var avatar = (Border)workspace.Controls.SkinAvatar.Parent;
        var originalControls = new UIElement[]
        {
            workspace.Controls.AccountMode,
            workspace.Controls.PlayerName,
            workspace.Controls.AvatarPlayerName,
            workspace.Controls.AccountStatus,
            avatar
        };
        foreach (var child in originalControls)
        {
            originalPanel.Children.Remove(child);
        }

        card.Child = null;
        card.Padding = new Thickness(16, 12, 16, 14);
        card.CornerRadius = new CornerRadius(10);
        accountPanel = new AccountPanelView(new(workspace.Controls.AccountMode, workspace.Controls.PlayerName, workspace.Controls.AvatarPlayerName, workspace.Controls.AccountStatus, avatar, (Style)workspace.Window.FindResource("AccountLoginChip")), new(() => workspace.AvatarController.RefreshAvatar(true), () => workspace.DownloadTaskController.RunAccountAction(workspace.LaunchCoordinator.Login, "微软正版登录"), () => workspace.DownloadTaskController.RunAccountAction(workspace.MuaAccountController.MuaLogin, "MUA 登录"), () => workspace.AccountController.AccountManagementClick(workspace.Window, new RoutedEventArgs()), () => workspace.MuaAccountController.MuaManagementClick(workspace.Window, new RoutedEventArgs())));
        card.Child = accountPanel;
        ConfigureHomeWelcome(card);
        UpdateAccountPanel();
    }

    internal void ConfigureHomeWelcome(Border card)
    {
        var home = (Grid)((TabItem)workspace.Window.MainTabs.Items[0]).Content;
        var banner = home.Children.OfType<Border>().FirstOrDefault(b => b != card && Grid.GetColumn(b) == 2);
        if (banner != null && !File.Exists(workspace.LaunchCoordinator.prefs.BackgroundPath))
        {
            banner.SetResourceReference(Border.BackgroundProperty, "ThemeSurfaceBrush");
            banner.CornerRadius = new CornerRadius(10);
            banner.BorderBrush = new SolidColorBrush(Color.FromRgb(214, 225, 232));
            banner.BorderThickness = new Thickness(1);
            banner.Child = new HomeWelcomeView(Brand.Name);
        }
    }

    internal void UpdateAccountPanel() => accountPanel?.UpdateState(workspace.AccountController.savedAccount != null || workspace.LaunchCoordinator.online != null, workspace.MuaAccountController.muaAccount != null);
}
