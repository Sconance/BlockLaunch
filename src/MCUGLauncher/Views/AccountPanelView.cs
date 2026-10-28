using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockLaunch;

public sealed record AccountPanelControls(ComboBox Mode, TextBox PlayerName, TextBlock DisplayName, TextBlock Status, Border Avatar, Style ChipStyle);
public sealed record AccountPanelActions(Action RefreshAvatar, Func<Task> SignInMicrosoft, Func<Task> SignInMua, Action ManageMicrosoft, Action ManageMua);

/// <summary>Account presentation only. Authentication and credential persistence stay in application services.</summary>
public sealed class AccountPanelView : UserControl
{
    readonly AccountPanelControls controls;
    readonly AccountPanelActions actions;
    readonly Grid layout = new();
    RadioButton? microsoftChip, offlineChip, muaChip;
    StackPanel? offlineIdentity, microsoftIdentity, muaIdentity;
    Button? microsoftSignIn, muaSignIn;


    public AccountPanelView(AccountPanelControls controls, AccountPanelActions actions)
    {
        this.controls = controls;
        this.actions = actions;
        layout.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        layout.RowDefinitions.Add(new());
        layout.Children.Add(BuildAccountModeSelector());
        var identity = BuildAccountIdentity(controls.Avatar);
        Grid.SetRow(identity, 1);
        layout.Children.Add(identity);
        Content = layout;
    }

    public RadioButton MicrosoftMode => microsoftChip!;
    public RadioButton MuaMode => muaChip!;
    public StackPanel OfflineIdentity => offlineIdentity!;
    public StackPanel MicrosoftIdentity => microsoftIdentity!;
    public StackPanel MuaIdentity => muaIdentity!;

    static Button ActionButton(string text, RoutedEventHandler action)
    {
        var button = new Button { Content = text };
        button.Click += action;
        return button;
    }

    public void ApplyCompactLayout()
    {
        layout.RowDefinitions[1].Height = GridLength.Auto;
        var identity = (StackPanel)layout.Children[1];
        identity.VerticalAlignment = VerticalAlignment.Top;
        identity.Margin = new Thickness(0, 5, 0, 0);
        controls.Avatar.Width = 64;
        controls.Avatar.Height = 64;
        controls.Avatar.Margin = new Thickness(0, 4, 0, 8);
        controls.DisplayName.FontSize = 17;
        controls.DisplayName.MaxWidth = 196;
        controls.DisplayName.ToolTip = controls.DisplayName.Text;
        controls.Status.Margin = new Thickness(0, 4, 0, 9);
        controls.Status.MaxWidth = 196;
        foreach (var chip in new[] { microsoftChip!, offlineChip!, muaChip! })
        {
            chip.FontSize = 11;
            chip.Margin = new Thickness(0);
        }
    }
    StackPanel BuildAccountModeSelector()
    {
        var chips = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var style = controls.ChipStyle;
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
            chip.SetBinding(IsEnabledProperty, new Binding(nameof(controls.Mode.IsEnabled)) { Source = controls.Mode });
            chips.Children.Add(chip);
        }

        microsoftChip.Checked += (_, _) =>
        {
            if (controls.Mode.SelectedIndex != 1)
                controls.Mode.SelectedIndex = 1;
        };
        offlineChip.Checked += (_, _) =>
        {
            if (controls.Mode.SelectedIndex != 0)
                controls.Mode.SelectedIndex = 0;
        };
        muaChip.Checked += (_, _) =>
        {
            if (controls.Mode.SelectedIndex != 2)
                controls.Mode.SelectedIndex = 2;
        };
        controls.Mode.Visibility = Visibility.Collapsed;
        chips.Children.Add(controls.Mode);
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
        avatar.MouseLeftButtonUp += (_, _) => actions.RefreshAvatar();
        identity.Children.Add(avatar);
        controls.DisplayName.FontSize = 20;
        controls.DisplayName.MaxWidth = 220;
        controls.DisplayName.TextTrimming = TextTrimming.CharacterEllipsis;
        identity.Children.Add(controls.DisplayName);
        controls.Status.FontSize = 11;
        controls.Status.TextWrapping = TextWrapping.Wrap;
        controls.Status.MaxWidth = 220;
        controls.Status.TextAlignment = TextAlignment.Center;
        controls.Status.Margin = new Thickness(0, 7, 0, 14);
        identity.Children.Add(controls.Status);
        identity.Children.Add(BuildOfflineIdentity());
        identity.Children.Add(BuildMicrosoftIdentity());
        identity.Children.Add(BuildMuaIdentity());
        return identity;
    }

    StackPanel BuildOfflineIdentity()
    {
        offlineIdentity = new StackPanel();
        offlineIdentity.Children.Add(new TextBlock { Text = "离线角色名", FontSize = 11, Foreground = Brushes.SlateGray });
        controls.PlayerName.ToolTip = "离线模式使用的游戏名称：3–16 位英文字母、数字或下划线";
        offlineIdentity.Children.Add(controls.PlayerName);
        offlineIdentity.Children.Add(new TextBlock { Text = "仅用于离线游戏，不影响正版角色。", FontSize = 10, Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
        return offlineIdentity;
    }

    StackPanel BuildMicrosoftIdentity()
    {
        microsoftIdentity = new StackPanel();
        microsoftSignIn = ActionButton("登录微软账号", async (_, _) => await actions.SignInMicrosoft());
        microsoftIdentity.Children.Add(microsoftSignIn);
        var manage = ActionButton("账号设置", (_, _) => actions.ManageMicrosoft());
        manage.Background = Brushes.Transparent;
        manage.FontSize = 11;
        microsoftIdentity.Children.Add(manage);
        return microsoftIdentity;
    }

    StackPanel BuildMuaIdentity()
    {
        muaIdentity = new StackPanel();
        muaSignIn = ActionButton("登录 MUA 账号", async (_, _) => await actions.SignInMua());
        muaIdentity.Children.Add(muaSignIn);
        muaIdentity.Children.Add(ActionButton("MUA 账号设置", (_, _) => actions.ManageMua()));
        return muaIdentity;
    }

    public void UpdateState(bool microsoftSaved, bool muaSaved)
    {
        if (microsoftChip == null || offlineChip == null || offlineIdentity == null || microsoftIdentity == null)
            return;
        var onlineMode = controls.Mode.SelectedIndex == 1;
        var muaMode = controls.Mode.SelectedIndex == 2;
        microsoftChip.IsChecked = onlineMode;
        offlineChip.IsChecked = !onlineMode && !muaMode;
        if (muaChip != null)
            muaChip.IsChecked = muaMode;
        offlineIdentity.Visibility = onlineMode || muaMode ? Visibility.Collapsed : Visibility.Visible;
        microsoftIdentity.Visibility = onlineMode ? Visibility.Visible : Visibility.Collapsed;
        if (muaIdentity != null)
            muaIdentity.Visibility = muaMode ? Visibility.Visible : Visibility.Collapsed;
        if (muaSignIn != null)
            muaSignIn.Content = !muaSaved ? "登录 MUA 账号" : "切换 MUA 账号";
        microsoftSignIn!.Content = microsoftSaved ? "切换微软账号" : "登录微软账号";
    }
}
