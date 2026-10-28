using System.IO;
using System.Net.Http;
using System.Windows;

namespace BlockLaunch;
internal sealed class AvatarController
{
    readonly LauncherWorkspace workspace;
    internal AvatarController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal CancellationTokenSource? avatarCancellation;
    internal int avatarRevision;
    internal void UpdateAvatarName()
    {
        workspace.Controls.AvatarPlayerName.Text = workspace.Controls.AccountMode.SelectedIndex == 2 ? workspace.MuaAccountController.muaAccount?.Profile.Name ?? "尚未登录" : workspace.Controls.AccountMode.SelectedIndex == 1 ? workspace.LaunchCoordinator.online?.Username ?? workspace.AccountController.savedAccount?.Username ?? "尚未登录" : string.IsNullOrWhiteSpace(workspace.Controls.PlayerName.Text) ? "请输入角色名" : workspace.Controls.PlayerName.Text;
    }

    internal async void RefreshAvatar(bool bypassCache = false)
    {
        if (workspace.Controls.SkinAvatar == null || workspace.Controls.AvatarCaption == null)
            return;
        avatarCancellation?.Cancel();
        avatarCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        avatarCancellation = cancellation;
        var token = cancellation.Token;
        var revision = ++avatarRevision;
        UpdateAvatarName();
        workspace.Controls.SkinAvatar.Source = AvatarRenderer.Default();
        workspace.Controls.AvatarCaption.Text = "默认头像";
        try
        {
            if (workspace.Controls.AccountMode.SelectedIndex == 2)
            {
                var account = workspace.MuaAccountController.muaAccount;
                if (account == null)
                    return;
                var cacheFile = Path.Combine(workspace.LaunchCoordinator.settingsDir, "mua-avatars", account.Profile.Id + ".png");
                if (!bypassCache && File.Exists(cacheFile) && new FileInfo(cacheFile).Length <= 2 * 1024 * 1024)
                {
                    try
                    {
                        var cachedMua = AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(cacheFile, token));
                        if (revision != avatarRevision)
                            return;
                        workspace.Controls.SkinAvatar.Source = cachedMua;
                        workspace.Controls.AvatarCaption.Text = "MUA 皮肤";
                        return;
                    }
                    catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or FormatException)
                    {
                    }
                }

                var metadata = await workspace.MuaAccountController.muaAccounts.Metadata(token);
                var muaUri = await workspace.MuaAccountController.muaAccounts.Skin(account, metadata, token);
                if (muaUri == null)
                    return;
                byte[] data;
                using (var skinHttp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
                {
                    Timeout = TimeSpan.FromSeconds(15)
                }

                )
                    data = await AvatarRenderer.Download(skinHttp, muaUri, token);
                var muaAvatar = AvatarRenderer.FromSkin(data);
                if (revision != avatarRevision || token.IsCancellationRequested)
                    return;
                workspace.Controls.SkinAvatar.Source = muaAvatar;
                workspace.Controls.AvatarCaption.Text = "MUA 皮肤";
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
                await File.WriteAllBytesAsync(cacheFile, data, token);
                return;
            }

            if (workspace.Controls.AccountMode.SelectedIndex != 1)
            {
                if (File.Exists(workspace.LaunchCoordinator.prefs.OfflineSkin))
                {
                    var localBytes = await File.ReadAllBytesAsync(workspace.LaunchCoordinator.prefs.OfflineSkin, token);
                    if (revision != avatarRevision || token.IsCancellationRequested)
                        return;
                    workspace.Controls.SkinAvatar.Source = AvatarRenderer.FromSkin(localBytes);
                    workspace.Controls.AvatarCaption.Text = "本地皮肤";
                }

                return;
            }

            var skin = workspace.AccountController.savedAccount?.SkinUrl;
            if (string.IsNullOrWhiteSpace(skin))
                return;
            var uri = AvatarRenderer.SkinUri(skin);
            var cache = Path.Combine(workspace.LaunchCoordinator.settingsDir, "avatars");
            var file = AvatarRenderer.CachePath(cache, uri);
            if (!bypassCache && File.Exists(file))
            {
                try
                {
                    var cached = AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(file, token));
                    if (revision != avatarRevision)
                        return;
                    workspace.Controls.SkinAvatar.Source = cached;
                    workspace.Controls.AvatarCaption.Text = "正版皮肤";
                    return;
                }
                catch (Exception ex) when (ex is IOException or System.NotSupportedException or System.ArgumentException or FormatException)
                {
                }
            }

            workspace.Controls.AvatarCaption.Text = "正在读取皮肤…";
            var bytes = await AvatarRenderer.Download(workspace.LaunchCoordinator.http, uri, token);
            var avatar = AvatarRenderer.FromSkin(bytes);
            if (revision != avatarRevision || token.IsCancellationRequested)
                return;
            workspace.Controls.SkinAvatar.Source = avatar;
            workspace.Controls.AvatarCaption.Text = "正版皮肤";
            Directory.CreateDirectory(cache);
            await File.WriteAllBytesAsync(file, bytes, token);
        }
        catch (OperationCanceledException)
        {
            if (revision == avatarRevision)
                workspace.Controls.AvatarCaption.Text = "头像暂不可用";
        }
        catch (Exception)
        {
            if (revision == avatarRevision)
                workspace.Controls.AvatarCaption.Text = "头像暂不可用，可稍后刷新";
        }
    }

    internal async void RefreshAvatarClick(object s, RoutedEventArgs e)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        if (workspace.Controls.AccountMode.SelectedIndex == 1 && workspace.AccountController.savedAccount != null)
            await workspace.LaunchCoordinator.Run(() => workspace.AccountController.MicrosoftLogin(false), "刷新玩家档案与头像");
        RefreshAvatar(true);
    }
}
