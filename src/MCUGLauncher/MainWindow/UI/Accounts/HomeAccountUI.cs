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
        var grid = new Grid();
        grid.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        grid.RowDefinitions.Add(new());
        card.Child = grid;
        grid.Children.Add(BuildAccountModeSelector());
        var identity = BuildAccountIdentity(avatar);
        Grid.SetRow(identity, 1);
        grid.Children.Add(identity);
        ConfigureHomeWelcome(card);
        UpdateAccountPanel();
    }

    StackPanel BuildAccountModeSelector()
    {
        var chips = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var style = (Style)FindResource("AccountLoginChip");
        microsoftChip = new RadioButton
        {
            Content = "◇  正版",
            GroupName = "AccountLoginMode",
            Style = style
        };
        offlineChip = new RadioButton
        {
            Content = "♧  离线",
            GroupName = "AccountLoginMode",
            Style = style
        };
        muaChip = new RadioButton
        {
            Content = "MUA 登录",
            GroupName = "AccountLoginMode",
            Style = style,
            FontSize = 11
        };
        var loginChips = new[] {
            microsoftChip,
            offlineChip,
            muaChip
        };
        foreach (var chip in loginChips)
        {
            chip.SetBinding(IsEnabledProperty, new Binding(nameof(AccountMode.IsEnabled)) { Source = AccountMode });
            chips.Children.Add(chip);
        }

        microsoftChip.Checked += (_, _) =>
        {
            if (AccountMode.SelectedIndex != 1)
                AccountMode.SelectedIndex = 1;
        };
        offlineChip.Checked += (_, _) =>
        {
            if (AccountMode.SelectedIndex != 0)
                AccountMode.SelectedIndex = 0;
        };
        muaChip.Checked += (_, _) =>
        {
            if (AccountMode.SelectedIndex != 2)
                AccountMode.SelectedIndex = 2;
        };
        AccountMode.Visibility = Visibility.Collapsed;
        chips.Children.Add(AccountMode);
        return chips;
    }

    StackPanel BuildAccountIdentity(Border avatar)
    {
        var identity = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 12)
        };
        avatar.Width = 84;
        avatar.Height = 84;
        avatar.Padding = new Thickness(5);
        avatar.Margin = new Thickness(0, 6, 0, 12);
        avatar.CornerRadius = new CornerRadius(11);
        avatar.ToolTip = "点击刷新角色头像";
        avatar.MouseLeftButtonUp += (_, _) => playerIdentityController.Refresh(true);
        identity.Children.Add(avatar);
        AvatarPlayerName.FontSize = 20;
        AvatarPlayerName.MaxWidth = 220;
        AvatarPlayerName.TextTrimming = TextTrimming.CharacterEllipsis;
        identity.Children.Add(AvatarPlayerName);
        AccountStatus.FontSize = 11;
        AccountStatus.TextWrapping = TextWrapping.Wrap;
        AccountStatus.MaxWidth = 220;
        AccountStatus.TextAlignment = TextAlignment.Center;
        AccountStatus.Margin = new Thickness(0, 7, 0, 14);
        identity.Children.Add(AccountStatus);
        identity.Children.Add(BuildOfflineIdentity());
        identity.Children.Add(BuildMicrosoftIdentity());
        identity.Children.Add(BuildMuaIdentity());
        return identity;
    }

    StackPanel BuildOfflineIdentity()
    {
        offlineIdentity = new StackPanel();
        offlineIdentity.Children.Add(new TextBlock { Text = "离线角色名", FontSize = 11, Foreground = Brushes.SlateGray });
        PlayerName.ToolTip = "离线模式使用的游戏名称：3–16 位英文字母、数字或下划线";
        offlineIdentity.Children.Add(PlayerName);
        offlineIdentity.Children.Add(new TextBlock { Text = "仅用于离线游戏，不影响正版角色。", FontSize = 10, Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
        return offlineIdentity;
    }

    StackPanel BuildMicrosoftIdentity()
    {
        microsoftIdentity = new StackPanel();
        microsoftSignIn = ActionButton("登录微软账号", async (_, _) => await RunAccountAction(Login, "微软正版登录"));
        microsoftIdentity.Children.Add(microsoftSignIn);
        var manage = ActionButton("账号设置", AccountManagementClick);
        manage.Background = Brushes.Transparent;
        manage.FontSize = 11;
        microsoftIdentity.Children.Add(manage);
        return microsoftIdentity;
    }

    StackPanel BuildMuaIdentity()
    {
        muaIdentity = new StackPanel();
        muaSignIn = ActionButton("登录 MUA 账号", async (_, _) => await RunAccountAction(async () =>
        {
            await MuaLogin();
        }, "MUA 登录"));
        muaIdentity.Children.Add(muaSignIn);
        muaIdentity.Children.Add(ActionButton("MUA 账号设置", MuaManagementClick));
        return muaIdentity;
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
            var welcome = new StackPanel
            {
                Margin = new Thickness(36),
                VerticalAlignment = VerticalAlignment.Center
            };
            welcome.Children.Add(new TextBlock { Text = "MINECRAFT JAVA EDITION", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(100, 119, 130)), Margin = new Thickness(0, 0, 0, 14) });
            welcome.Children.Add(new TextBlock { Text = Brand.Name, FontSize = 36, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 109, 184)) });
            welcome.Children.Add(new TextBlock { Text = "选择版本，开始游戏。", FontSize = 16, Margin = new Thickness(0, 14, 0, 26) });
            welcome.Children.Add(new Border { Height = 2, Width = 48, HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(Color.FromRgb(158, 167, 171)), Margin = new Thickness(0, 0, 0, 22) });
            banner.Child = welcome;
        }
    }

    void UpdateAccountPanel()
    {
        if (microsoftChip == null || offlineChip == null || offlineIdentity == null || microsoftIdentity == null)
            return;
        var onlineMode = AccountMode.SelectedIndex == 1;
        var muaMode = AccountMode.SelectedIndex == 2;
        microsoftChip.IsChecked = onlineMode;
        offlineChip.IsChecked = !onlineMode && !muaMode;
        if (muaChip != null)
            muaChip.IsChecked = muaMode;
        offlineIdentity.Visibility = onlineMode || muaMode ? Visibility.Collapsed : Visibility.Visible;
        microsoftIdentity.Visibility = onlineMode ? Visibility.Visible : Visibility.Collapsed;
        if (muaIdentity != null)
            muaIdentity.Visibility = muaMode ? Visibility.Visible : Visibility.Collapsed;
        if (muaSignIn != null)
            muaSignIn.Content = muaAccount == null ? "登录 MUA 账号" : "切换 MUA 账号";
        microsoftSignIn!.Content = savedAccount != null || online != null ? "切换微软账号" : "登录微软账号";
    }
}
