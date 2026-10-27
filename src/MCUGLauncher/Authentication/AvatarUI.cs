using System.IO;
using System.Net.Http;
using System.Windows;

namespace BlockLaunch;

public partial class MainWindow
{
    CancellationTokenSource? avatarCancellation;
    int avatarRevision;

    void UpdateAvatarName()
    {
        AvatarPlayerName.Text = AccountMode.SelectedIndex == 2 ? muaAccount?.Profile.Name ?? "尚未登录" : AccountMode.SelectedIndex == 1 ? online?.Username ?? savedAccount?.Username ?? "尚未登录" : string.IsNullOrWhiteSpace(PlayerName.Text) ? "请输入角色名" : PlayerName.Text;
    }

    async void RefreshAvatar(bool bypassCache = false)
    {
        if (SkinAvatar == null || AvatarCaption == null)
            return;
        avatarCancellation?.Cancel();
        avatarCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        avatarCancellation = cancellation;
        var token = cancellation.Token;
        var revision = ++avatarRevision;
        UpdateAvatarName();
        SkinAvatar.Source = AvatarRenderer.Default();
        AvatarCaption.Text = "默认头像";
        try
        {
            if (AccountMode.SelectedIndex == 2)
            {
                var account = muaAccount;
                if (account == null)
                    return;
                var cacheFile = Path.Combine(settingsDir, "mua-avatars", account.Profile.Id + ".png");
                if (!bypassCache && File.Exists(cacheFile) && new FileInfo(cacheFile).Length <= 2 * 1024 * 1024)
                {
                    try
                    {
                        var cachedMua = AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(cacheFile, token));
                        if (revision != avatarRevision)
                            return;
                        SkinAvatar.Source = cachedMua;
                        AvatarCaption.Text = "MUA 皮肤";
                        return;
                    }
                    catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or FormatException)
                    {
                    }
                }

                var metadata = await muaAccounts.Metadata(token);
                var muaUri = await muaAccounts.Skin(account, metadata, token);
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
                SkinAvatar.Source = muaAvatar;
                AvatarCaption.Text = "MUA 皮肤";
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
                await File.WriteAllBytesAsync(cacheFile, data, token);
                return;
            }

            if (AccountMode.SelectedIndex != 1)
            {
                if (File.Exists(prefs.OfflineSkin))
                {
                    var localBytes = await File.ReadAllBytesAsync(prefs.OfflineSkin, token);
                    if (revision != avatarRevision || token.IsCancellationRequested)
                        return;
                    SkinAvatar.Source = AvatarRenderer.FromSkin(localBytes);
                    AvatarCaption.Text = "本地皮肤";
                }

                return;
            }

            var skin = savedAccount?.SkinUrl;
            if (string.IsNullOrWhiteSpace(skin))
                return;
            var uri = AvatarRenderer.SkinUri(skin);
            var cache = Path.Combine(settingsDir, "avatars");
            var file = AvatarRenderer.CachePath(cache, uri);
            if (!bypassCache && File.Exists(file))
            {
                try
                {
                    var cached = AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(file, token));
                    if (revision != avatarRevision)
                        return;
                    SkinAvatar.Source = cached;
                    AvatarCaption.Text = "正版皮肤";
                    return;
                }
                catch (Exception ex) when (ex is IOException or System.NotSupportedException or System.ArgumentException or FormatException)
                {
                }
            }

            AvatarCaption.Text = "正在读取皮肤…";
            var bytes = await AvatarRenderer.Download(http, uri, token);
            var avatar = AvatarRenderer.FromSkin(bytes);
            if (revision != avatarRevision || token.IsCancellationRequested)
                return;
            SkinAvatar.Source = avatar;
            AvatarCaption.Text = "正版皮肤";
            Directory.CreateDirectory(cache);
            await File.WriteAllBytesAsync(file, bytes, token);
        }
        catch (OperationCanceledException)
        {
            if (revision == avatarRevision)
                AvatarCaption.Text = "头像暂不可用";
        }
        catch (Exception)
        {
            if (revision == avatarRevision)
                AvatarCaption.Text = "头像暂不可用，可稍后刷新";
        }
    }

    async void RefreshAvatarClick(object s, RoutedEventArgs e)
    {
        if (busy)
            return;
        if (AccountMode.SelectedIndex == 1 && savedAccount != null)
            await Run(() => MicrosoftLogin(false), "刷新玩家档案与头像");
        RefreshAvatar(true);
    }
}
