using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class MuaAccountController
{
    readonly LauncherWorkspace workspace;
    internal MuaAccountController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal MuaAccountService muaAccounts = null!;
    internal MuaCredential? muaAccount;
    internal bool muaValidated;
    internal void BuildMuaAccountUI()
    {
        muaAccounts = new(Path.Combine(workspace.LaunchCoordinator.settingsDir, "accounts"));
        try
        {
            muaAccount = muaAccounts.Read();
        }
        catch (Exception ex) when (ex is AccountStorageException or IOException or UnauthorizedAccessException)
        {
            workspace.LaunchCoordinator.Log("MUA 账号缓存暂不可用，请在 MUA 账号设置中清除后重新登录。原文件已保留。");
        }

        if (workspace.Controls.AccountMode.Items.Count < 3)
            workspace.Controls.AccountMode.Items.Add(new ComboBoxItem { Content = "MUA 联合认证" });
        workspace.Window.PreviewDragOver += (_, e) =>
        {
            if (e.Data.GetDataPresent(DataFormats.UnicodeText) && MuaAccountService.IsMuaConfiguration(e.Data.GetData(DataFormats.UnicodeText) as string ?? ""))
            {
                e.Handled = true;
                e.Effects = workspace.LaunchCoordinator.busy ? DragDropEffects.None : DragDropEffects.Copy;
            }
        };
        workspace.Window.PreviewDrop += (_, e) =>
        {
            if (e.Handled || workspace.LaunchCoordinator.busy || !e.Data.GetDataPresent(DataFormats.UnicodeText) || !MuaAccountService.IsMuaConfiguration(e.Data.GetData(DataFormats.UnicodeText) as string ?? ""))
                return;
            e.Handled = true;
            if (InlineMessageBox.Show(workspace.Window, "使用 MUA 用户中心作为认证服务器？\n" + MuaAccountService.Api, "添加 MUA 认证", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                workspace.Controls.AccountMode.SelectedIndex = 2;
                workspace.AccountController.UpdateAccountBadge();
            }
        };
        workspace.Window.Closed += (_, _) => muaAccounts.Dispose();
    }

    internal Task<MuaProfile?> SelectMuaRole(IReadOnlyList<MuaProfile> profiles)
    {
        var body = new StackPanel();
        body.Children.Add(InstancePagesController.Hint("请选择本次登录使用的 MUA 游戏角色。角色 UUID 决定服务器中的背包和身份。"));
        var list = new ListBox
        {
            ItemsSource = profiles,
            DisplayMemberPath = "Name",
            MinHeight = 100,
            MaxHeight = 300,
            SelectedIndex = 0
        };
        body.Children.Add(list);
        var dialog = workspace.InstancePagesController.FeatureDialog("选择 MUA 角色", body);
        MuaProfile? selected = null;
        body.Children.Add(InstancePagesController.ActionButton("使用所选角色", (_, _) =>
        {
            if (list.SelectedItem is MuaProfile profile)
            {
                selected = profile;
                dialog.DialogResult = true;
            }
        }));
        body.Children.Add(InstancePagesController.ActionButton("取消", (_, _) => dialog.Close()));
        dialog.ShowDialog();
        return Task.FromResult(selected);
    }

    internal async Task<bool> MuaLogin()
    {
        var body = new StackPanel();
        body.Children.Add(InstancePagesController.Heading("MUA 联合认证"));
        body.Children.Add(InstancePagesController.Hint("使用 MUA 用户中心的账号或角色名登录。高校身份认证、注册和角色创建请在 MUA 官方网站完成。"));
        var endpoint = new TextBox
        {
            Text = MuaAccountService.Api,
            IsReadOnly = true,
            ToolTip = "凭据仅发送到此 MUA HTTPS 认证接口"
        };
        body.Children.Add(endpoint);
        body.Children.Add(InstancePagesController.Hint("账号 / 角色名"));
        var account = new TextBox
        {
            Text = muaAccount?.Account ?? "",
            MaxLength = 320
        };
        body.Children.Add(account);
        body.Children.Add(InstancePagesController.Hint("MUA 密码"));
        var password = new PasswordBox
        {
            Padding = new Thickness(10),
            Margin = new Thickness(0, 4, 0, 12),
            MaxLength = 4096
        };
        body.Children.Add(password);
        body.Children.Add(InstancePagesController.Hint("密码仅用于本次登录，不保存；登录 Token 按当前 Windows 用户加密保存。"));
        var validation = InstancePagesController.Hint("");
        body.Children.Add(validation);
        var dialog = workspace.InstancePagesController.FeatureDialog("MUA 登录", body);
        var confirmed = false;
        body.Children.Add(InstancePagesController.ActionButton("登录", (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(account.Text) || password.Password.Length == 0)
            {
                validation.Text = "请填写账号 / 角色名和 MUA 密码。";
                return;
            }

            confirmed = true;
            dialog.DialogResult = true;
        }));
        body.Children.Add(InstancePagesController.ActionButton("取消", (_, _) => dialog.Close()));
        dialog.ShowDialog();
        if (!confirmed)
        {
            password.Clear();
            workspace.LaunchCoordinator.operations.Cancel();
            workspace.LaunchCoordinator.CurrentToken.ThrowIfCancellationRequested();
            return false;
        }

        var secret = password.Password;
        password.Clear();
        try
        {
            workspace.LaunchCoordinator.operations.Current?.Report("正在验证 MUA 账号…");
            var credential = await muaAccounts.Login(account.Text, secret, SelectMuaRole, workspace.LaunchCoordinator.CurrentToken);
            muaAccount = credential;
            muaValidated = true;
            workspace.Controls.AccountMode.SelectedIndex = 2;
            workspace.LaunchCoordinator.prefs.PreferMua = true;
            workspace.LaunchCoordinator.prefs.PreferMicrosoft = false;
            workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
            workspace.AccountController.UpdateAccountBadge();
            workspace.LaunchCoordinator.Log("MUA 登录完成，角色：" + credential.Profile.Name + "。凭据已加密保存。");
            return true;
        }
        catch (OperationCanceledException) when (!workspace.LaunchCoordinator.CurrentToken.IsCancellationRequested)
        {
            workspace.LaunchCoordinator.operations.Cancel();
            workspace.LaunchCoordinator.CurrentToken.ThrowIfCancellationRequested();
            return false;
        }
        finally
        {
            secret = "";
        }
    }

    internal async Task EnsureMuaLogin(bool force = false)
    {
        try
        {
            muaValidated = false;
            muaAccount = await muaAccounts.Ensure(force, workspace.LaunchCoordinator.CurrentToken);
            muaValidated = true;
            workspace.AccountController.UpdateAccountBadge();
        }
        catch (AccountLoginException ex) when (ex.RequiresInteractive)
        {
            workspace.AccountController.UpdateAccountBadge();
            if (!await MuaLogin())
                throw new OperationCanceledException(workspace.LaunchCoordinator.CurrentToken);
        }
    }

    internal void MuaManagementClick(object sender, RoutedEventArgs args)
    {
        if (workspace.LaunchCoordinator.busy && !workspace.DownloadTaskController.DownloadInProgress)
            return;
        var body = new StackPanel();
        body.Children.Add(InstancePagesController.Heading("MUA 账号"));
        body.Children.Add(InstancePagesController.Hint(muaAccount == null ? "尚未登录 MUA。" : "角色：" + muaAccount.Profile.Name + "\nUUID：" + muaAccount.Profile.Id));
        body.Children.Add(InstancePagesController.Hint(MuaAccountService.Api));
        var dialog = workspace.InstancePagesController.FeatureDialog("MUA 账号设置", body);
        body.Children.Add(InstancePagesController.ActionButton("登录 / 更换 MUA 账号", async (_, _) =>
        {
            dialog.Close();
            await workspace.DownloadTaskController.RunAccountAction(async () =>
            {
                await MuaLogin();
            }, "MUA 登录");
        }));
        body.Children.Add(InstancePagesController.ActionButton("验证并刷新会话", async (_, _) =>
        {
            dialog.Close();
            await workspace.DownloadTaskController.RunAccountAction(() => EnsureMuaLogin(true), "刷新 MUA 会话");
        }));
        body.Children.Add(InstancePagesController.ActionButton("退出并清除 MUA 登录", async (_, _) =>
        {
            dialog.Close();
            await workspace.DownloadTaskController.RunAccountAction(async () =>
            {
                workspace.ContentController.RequireStopped();
                var revoked = await muaAccounts.Forget(workspace.LaunchCoordinator.CurrentToken);
                muaAccount = null;
                muaValidated = false;
                workspace.LaunchCoordinator.prefs.PreferMua = false;
                workspace.Controls.AccountMode.SelectedIndex = 0;
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                workspace.AccountController.UpdateAccountBadge();
                workspace.LaunchCoordinator.Log(revoked ? "已退出 MUA 并撤销当前会话。" : "已清除本地 MUA 登录，认证站撤销未确认；可在 MUA 用户中心管理会话。");
            }, "退出 MUA");
        }));
        body.Children.Add(InstancePagesController.ActionButton("MUA 用户中心 ↗", (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://skin.mualliance.ltd/user") { UseShellExecute = true })));
        dialog.ShowDialog();
    }
}
