using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class MicrosoftAccountController(MainWindow host, AccountSessionService accounts)
{
    public SavedMicrosoftAccount? SavedAccount
    {
        get; private set;
    }

    public void Load()
    {
        try
        {
            SavedAccount = accounts.ReadMicrosoft();
        }
        catch (Exception ex) when (ex is AccountStorageException or IOException or UnauthorizedAccessException)
        {
            host.Log("本地正版账号暂不可用。可在账号管理中清除后重新登录。");
        }
    }

    public async Task Login(bool interactive, bool ensure = false)
    {
        host.online = null;
        host.UpdateAccountBadge();
        host.operations.Current?.Report(interactive
            ? "请在微软官方授权窗口完成登录；随后验证 Xbox 和 Java 版授权…"
            : "正在验证 / 刷新微软、Xbox 和 Minecraft 会话…");
        host.online = ensure
            ? await accounts.EnsureMicrosoft(host.CurrentToken, host.Log)
            : await accounts.LoginMicrosoft(interactive, host.CurrentToken);
        SavedAccount = accounts.ReadMicrosoft();
        host.prefs.PreferMicrosoft = true;
        host.prefs.PreferMua = false;
        host.settings.Save(host.prefs);
        host.SelectAccountMode(1);
        host.UpdateAccountBadge();
        host.Log(interactive
            ? "正版登录完成，已验证 Java 版档案和授权；凭据按当前 Windows 用户加密保存。"
            : "正版会话验证 / 刷新完成。");
    }

    public Task EnsureLogin() => Login(false, true);

    public void OpenManagement()
    {
        if (host.busy && !host.DownloadInProgress)
            return;

        var body = new StackPanel();
        body.Children.Add(MainWindow.Heading("微软正版账号"));
        body.Children.Add(MainWindow.Hint(SavedAccount == null
            ? "没有可用的已保存账号。"
            : "玩家：" + SavedAccount.Username + "\nUUID：" + SavedAccount.Uuid));
        body.Children.Add(MainWindow.Hint("凭据使用 Windows DPAPI 加密，限定当前 Windows 用户解密。每次正版启动前会检查 / 刷新会话。此版本保存一个正版账号，重新登录成功后替换。"));
        var dialog = host.FeatureDialog("正版账号管理", body);
        body.Children.Add(MainWindow.ActionButton("微软登录 / 切换账号", async (_, _) =>
        {
            dialog.Close();
            await host.RunAccountAction(host.Login, "微软正版登录");
        }));
        body.Children.Add(MainWindow.ActionButton("验证 / 刷新已保存账号", async (_, _) =>
        {
            dialog.Close();
            await host.RunAccountAction(() => Login(false), "刷新正版登录");
        }));
        body.Children.Add(MainWindow.ActionButton("退出并清除本地账号", async (_, _) =>
        {
            dialog.Close();
            await host.RunAccountAction(() =>
            {
                host.RequireStopped();
                accounts.ForgetMicrosoft();
                host.online = null;
                SavedAccount = null;
                host.SelectAccountMode(0);
                host.prefs.PreferMicrosoft = false;
                host.settings.Save(host.prefs);
                host.UpdateAccountBadge();
                host.Log("已清除启动器本地账号。微软浏览器的单点登录状态未清除，重新登录可在官方页面切换账号。");
                return Task.CompletedTask;
            }, "退出正版账号");
        }));
        dialog.ShowDialog();
    }
}
