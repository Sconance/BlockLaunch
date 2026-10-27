using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    MicrosoftAccountService accounts = null!;
    SavedMicrosoftAccount? savedAccount;

    void BuildAccountUI()
    {
        accounts = new MicrosoftAccountService(Path.Combine(settingsDir, "accounts"), http, new EmbeddedMicrosoftOAuthUI(this, Path.Combine(settingsDir, "microsoft-browser")));
        try
        {
            savedAccount = accounts.ReadSavedAccount();
        }
        catch (Exception ex) when (ex is AccountStorageException or IOException or UnauthorizedAccessException)
        {
            Log("本地正版账号暂不可用。可在账号管理中清除后重新登录。");
        }

        BuildMuaAccountUI();
        AccountMode.SelectedIndex = prefs.PreferMua ? 2 : prefs.PreferMicrosoft && savedAccount != null ? 1 : 0;
        AccountMode.SelectionChanged += (_, _) => UpdateAccountBadge();
        PlayerName.TextChanged += (_, _) => avatarController.UpdateName();
        Closed += (_, _) => avatarController.Cancel();
        ((StackPanel)AccountMode.Parent).Children.Add(ActionButton("管理正版账号", AccountManagementClick));
        UpdateAccountBadge();
    }

    void UpdateAccountBadge()
    {
        AccountStatus.Text = AccountMode.SelectedIndex == 2 ? muaAccount == null ? "MUA 联合认证 · 尚未登录" : muaValidated ? "MUA 角色 · 会话已验证" : "MUA 账号已保存 · 启动时验证" : AccountMode.SelectedIndex != 1 ? "离线游戏" : online != null ? "正版角色 · 本次会话已验证" : savedAccount != null ? "账号已保存 · 启动时验证" : "登录后显示你的正版角色";
        UpdateAccountPanel();
        avatarController.Refresh();
    }

    async Task MicrosoftLogin(bool interactive)
    {
        online = null;
        UpdateAccountBadge();
        operations.Current?.Report(interactive ? "请在微软官方授权窗口完成登录；随后验证 Xbox 和 Java 版授权…" : "正在验证 / 刷新微软、Xbox 和 Minecraft 会话…");
        online = await accounts.Authenticate(interactive, CurrentToken);
        savedAccount = accounts.ReadSavedAccount();
        prefs.PreferMicrosoft = true;
        prefs.PreferMua = false;
        settings.Save(prefs);
        AccountMode.SelectedIndex = 1;
        UpdateAccountBadge();
        Log(interactive ? "正版登录完成，已验证 Java 版档案和授权；凭据按当前 Windows 用户加密保存。" : "正版会话验证 / 刷新完成。");
    }

    async Task EnsureMicrosoftLogin()
    {
        try
        {
            await MicrosoftLogin(false);
        }
        catch (AccountLoginException ex) when (ex.RequiresInteractive)
        {
            Log(ex.Message);
            await MicrosoftLogin(true);
        }
    }

    void AccountManagementClick(object s, RoutedEventArgs e)
    {
        if (busy && !DownloadInProgress)
            return;
        var body = new StackPanel();
        body.Children.Add(Heading("微软正版账号"));
        body.Children.Add(Hint(savedAccount == null ? "没有可用的已保存账号。" : "玩家：" + savedAccount.Username + "\nUUID：" + savedAccount.Uuid));
        body.Children.Add(Hint("凭据使用 Windows DPAPI 加密，限定当前 Windows 用户解密。每次正版启动前会检查 / 刷新会话。此版本保存一个正版账号，重新登录成功后替换。"));
        var dialog = FeatureDialog("正版账号管理", body);
        body.Children.Add(ActionButton("微软登录 / 切换账号", async (_, _) =>
        {
            dialog.Close();
            await RunAccountAction(Login, "微软正版登录");
        }));
        body.Children.Add(ActionButton("验证 / 刷新已保存账号", async (_, _) =>
        {
            dialog.Close();
            await RunAccountAction(() => MicrosoftLogin(false), "刷新正版登录");
        }));
        body.Children.Add(ActionButton("退出并清除本地账号", async (_, _) =>
        {
            dialog.Close();
            await RunAccountAction(() =>
            {
                RequireStopped();
                accounts.Forget();
                online = null;
                savedAccount = null;
                AccountMode.SelectedIndex = 0;
                prefs.PreferMicrosoft = false;
                settings.Save(prefs);
                UpdateAccountBadge();
                Log("已清除启动器本地账号。微软浏览器的单点登录状态未清除，重新登录可在官方页面切换账号。");
                return Task.CompletedTask;
            }, "退出正版账号");
        }));
        dialog.ShowDialog();
    }
}
