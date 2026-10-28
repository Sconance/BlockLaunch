using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class AccountController
{
    readonly LauncherWorkspace workspace;
    internal AccountController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal MicrosoftAccountService accounts = null!;
    internal SavedMicrosoftAccount? savedAccount;
    internal void BuildAccountUI()
    {
        accounts = new MicrosoftAccountService(Path.Combine(workspace.LaunchCoordinator.settingsDir, "accounts"), workspace.LaunchCoordinator.http, new EmbeddedMicrosoftOAuthUI(workspace.Window, Path.Combine(workspace.LaunchCoordinator.settingsDir, "microsoft-browser")));
        try
        {
            savedAccount = accounts.ReadSavedAccount();
        }
        catch (Exception ex) when (ex is AccountStorageException or IOException or UnauthorizedAccessException)
        {
            workspace.LaunchCoordinator.Log("本地正版账号暂不可用。可在账号管理中清除后重新登录。");
        }

        workspace.MuaAccountController.BuildMuaAccountUI();
        workspace.Controls.AccountMode.SelectedIndex = workspace.LaunchCoordinator.prefs.PreferMua ? 2 : workspace.LaunchCoordinator.prefs.PreferMicrosoft && savedAccount != null ? 1 : 0;
        workspace.Controls.AccountMode.SelectionChanged += (_, _) => UpdateAccountBadge();
        workspace.Controls.PlayerName.TextChanged += (_, _) => workspace.AvatarController.UpdateAvatarName();
        workspace.Window.Closed += (_, _) =>
        {
            workspace.AvatarController.avatarCancellation?.Cancel();
            workspace.AvatarController.avatarCancellation?.Dispose();
        };
        ((StackPanel)workspace.Controls.AccountMode.Parent).Children.Add(FeatureUiElements.ActionButton("管理正版账号", AccountManagementClick));
        UpdateAccountBadge();
    }

    internal void UpdateAccountBadge()
    {
        workspace.Controls.AccountStatus.Text = workspace.Controls.AccountMode.SelectedIndex == 2 ? workspace.MuaAccountController.muaAccount == null ? "MUA 联合认证 · 尚未登录" : workspace.MuaAccountController.muaValidated ? "MUA 角色 · 会话已验证" : "MUA 账号已保存 · 启动时验证" : workspace.Controls.AccountMode.SelectedIndex != 1 ? "离线游戏" : workspace.LaunchCoordinator.online != null ? "正版角色 · 本次会话已验证" : savedAccount != null ? "账号已保存 · 启动时验证" : "登录后显示你的正版角色";
        workspace.HomeAccountController.UpdateAccountPanel();
        workspace.AvatarController.RefreshAvatar();
    }

    internal async Task MicrosoftLogin(bool interactive)
    {
        workspace.LaunchCoordinator.online = null;
        UpdateAccountBadge();
        workspace.LaunchCoordinator.operations.Current?.Report(interactive ? "请在微软官方授权窗口完成登录；随后验证 Xbox 和 Java 版授权…" : "正在验证 / 刷新微软、Xbox 和 Minecraft 会话…");
        workspace.LaunchCoordinator.online = await accounts.Authenticate(interactive, workspace.LaunchCoordinator.CurrentToken);
        savedAccount = accounts.ReadSavedAccount();
        workspace.LaunchCoordinator.prefs.PreferMicrosoft = true;
        workspace.LaunchCoordinator.prefs.PreferMua = false;
        workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
        workspace.Controls.AccountMode.SelectedIndex = 1;
        UpdateAccountBadge();
        workspace.LaunchCoordinator.Log(interactive ? "正版登录完成，已验证 Java 版档案和授权；凭据按当前 Windows 用户加密保存。" : "正版会话验证 / 刷新完成。");
    }

    internal async Task EnsureMicrosoftLogin()
    {
        try
        {
            await MicrosoftLogin(false);
        }
        catch (AccountLoginException ex) when (ex.RequiresInteractive)
        {
            workspace.LaunchCoordinator.Log(ex.Message);
            await MicrosoftLogin(true);
        }
    }

    internal void AccountManagementClick(object s, RoutedEventArgs e)
    {
        if (workspace.LaunchCoordinator.busy && !workspace.DownloadTaskController.DownloadInProgress)
            return;
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("微软正版账号"));
        body.Children.Add(FeatureUiElements.Hint(savedAccount == null ? "没有可用的已保存账号。" : "玩家：" + savedAccount.Username + "\nUUID：" + savedAccount.Uuid));
        body.Children.Add(FeatureUiElements.Hint("凭据使用 Windows DPAPI 加密，限定当前 Windows 用户解密。每次正版启动前会检查 / 刷新会话。此版本保存一个正版账号，重新登录成功后替换。"));
        var dialog = workspace.InstancePagesController.FeatureDialog("正版账号管理", body);
        body.Children.Add(FeatureUiElements.ActionButton("微软登录 / 切换账号", async (_, _) =>
        {
            dialog.Close();
            await workspace.DownloadTaskController.RunAccountAction(workspace.LaunchCoordinator.Login, "微软正版登录");
        }));
        body.Children.Add(FeatureUiElements.ActionButton("验证 / 刷新已保存账号", async (_, _) =>
        {
            dialog.Close();
            await workspace.DownloadTaskController.RunAccountAction(() => MicrosoftLogin(false), "刷新正版登录");
        }));
        body.Children.Add(FeatureUiElements.ActionButton("退出并清除本地账号", async (_, _) =>
        {
            dialog.Close();
            await workspace.DownloadTaskController.RunAccountAction(() =>
            {
                workspace.ContentController.RequireStopped();
                accounts.Forget();
                workspace.LaunchCoordinator.online = null;
                savedAccount = null;
                workspace.Controls.AccountMode.SelectedIndex = 0;
                workspace.LaunchCoordinator.prefs.PreferMicrosoft = false;
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                UpdateAccountBadge();
                workspace.LaunchCoordinator.Log("已清除启动器本地账号。微软浏览器的单点登录状态未清除，重新登录可在官方页面切换账号。");
                return Task.CompletedTask;
            }, "退出正版账号");
        }));
        dialog.ShowDialog();
    }
}
