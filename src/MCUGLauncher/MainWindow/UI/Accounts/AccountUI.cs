using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    MicrosoftAccountController microsoftAccountController = null!;
    SavedMicrosoftAccount? savedAccount => microsoftAccountController?.SavedAccount;

    void BuildAccountUI()
    {
        microsoftAccountController = new MicrosoftAccountController(this, settingsDir, http);
        microsoftAccountController.Load();
        BuildMuaAccountUI();
        AccountMode.SelectedIndex = prefs.PreferMua ? 2 : prefs.PreferMicrosoft && savedAccount != null ? 1 : 0;
        AccountMode.SelectionChanged += (_, _) => UpdateAccountBadge();
        PlayerName.TextChanged += (_, _) => playerIdentityController.UpdateName();
        Closed += (_, _) => playerIdentityController.Cancel();
        ((StackPanel)AccountMode.Parent).Children.Add(ActionButton("管理正版账号", AccountManagementClick));
        UpdateAccountBadge();
    }

    internal void UpdateAccountBadge()
    {
        AccountStatus.Text = AccountMode.SelectedIndex == 2 ? muaAccount == null ? "MUA 联合认证 · 尚未登录" : muaValidated ? "MUA 角色 · 会话已验证" : "MUA 账号已保存 · 启动时验证" : AccountMode.SelectedIndex != 1 ? "离线游戏" : online != null ? "正版角色 · 本次会话已验证" : savedAccount != null ? "账号已保存 · 启动时验证" : "登录后显示你的正版角色";
        UpdateAccountPanel();
        playerIdentityController.Refresh();
    }

    Task MicrosoftLogin(bool interactive) => microsoftAccountController.Login(interactive);

    Task EnsureMicrosoftLogin() => microsoftAccountController.EnsureLogin();

    void AccountManagementClick(object s, RoutedEventArgs e) => microsoftAccountController.OpenManagement();
}
