using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    MicrosoftAccountController microsoftAccountController = null!;
    SavedMicrosoftAccount? savedAccount => microsoftAccountController?.SavedAccount;

    void BuildAccountUI()
    {
        muaAccounts = new(Path.Combine(settingsDir, "accounts"));
        accountSessions = new AccountSessionService(new MicrosoftAccountService(
            Path.Combine(settingsDir, "accounts"), http,
            new EmbeddedMicrosoftOAuthUI(this, Path.Combine(settingsDir, "microsoft-browser"))), muaAccounts);
        microsoftAccountController = new MicrosoftAccountController(this, accountSessions);
        microsoftAccountController.Load();
        BuildMuaAccountUI();
        InitializeAccountControls(
            prefs.PreferMua ? 2 : prefs.PreferMicrosoft && savedAccount != null ? 1 : 0,
            UpdateAccountBadge,
            playerIdentityController.UpdateName,
            AccountManagementClick);
        Closed += (_, _) => playerIdentityController.Cancel();
        UpdateAccountBadge();
    }

    internal void UpdateAccountBadge()
    {
        var accountMode = AccountModeIndex;
        SetAccountStatusText(accountMode == 2 ? muaAccount == null ? "MUA 联合认证 · 尚未登录" : muaValidated ? "MUA 角色 · 会话已验证" : "MUA 账号已保存 · 启动时验证" : accountMode != 1 ? "离线游戏" : online != null ? "正版角色 · 本次会话已验证" : savedAccount != null ? "账号已保存 · 启动时验证" : "登录后显示你的正版角色");
        UpdateAccountPanel();
        playerIdentityController.Refresh();
    }

    Task MicrosoftLogin(bool interactive) => microsoftAccountController.Login(interactive);

    Task EnsureMicrosoftLogin() => microsoftAccountController.EnsureLogin();

    internal void SelectAccountMode(int index) => AccountModeIndex = index;

    void AccountManagementClick(object s, RoutedEventArgs e) => microsoftAccountController.OpenManagement();
}
