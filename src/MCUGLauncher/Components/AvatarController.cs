using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class AvatarController
{
    readonly Image skinAvatar;
    readonly TextBlock avatarCaption;
    readonly TextBlock displayName;
    readonly Func<PlayerIdentityState> getState;
    readonly Func<bool> isBusy;
    readonly Func<Task> refreshMicrosoftProfile;

    internal AvatarController(Image skinAvatar,
        TextBlock avatarCaption, TextBlock displayName,
        Func<PlayerIdentityState> getState, Func<bool> isBusy, Func<Task> refreshMicrosoftProfile)
    {
        this.skinAvatar = skinAvatar;
        this.avatarCaption = avatarCaption;
        this.displayName = displayName;
        this.getState = getState;
        this.isBusy = isBusy;
        this.refreshMicrosoftProfile = refreshMicrosoftProfile;
    }

    internal CancellationTokenSource? avatarCancellation;
    internal int avatarRevision;
    internal void UpdateAvatarName()
    {
        var state = getState();
        displayName.Text = state.AccountMode switch
        {
            2 => state.MuaAccount?.Profile.Name ?? "尚未登录",
            1 => state.OnlineUsername ?? state.MicrosoftAccount?.Username ?? "尚未登录",
            _ => string.IsNullOrWhiteSpace(state.PlayerName) ? "请输入角色名" : state.PlayerName
        };
    }

    internal async void RefreshAvatar(bool bypassCache = false)
    {
        var state = getState();
        if (skinAvatar == null || avatarCaption == null)
            return;
        avatarCancellation?.Cancel();
        avatarCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        avatarCancellation = cancellation;
        var token = cancellation.Token;
        var revision = ++avatarRevision;
        UpdateAvatarName();
        skinAvatar.Source = AvatarRenderer.Default();
        avatarCaption.Text = "默认头像";
        try
        {
            if (state.AccountMode == 2)
            {
                var account = state.MuaAccount;
                if (account == null)
                    return;
                var cacheFile = Path.Combine(state.SettingsDirectory, "mua-avatars", account.Profile.Id + ".png");
                if (!bypassCache && File.Exists(cacheFile) && new FileInfo(cacheFile).Length <= 2 * 1024 * 1024)
                {
                    try
                    {
                        var cachedMua = AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(cacheFile, token));
                        if (revision != avatarRevision)
                            return;
                        skinAvatar.Source = cachedMua;
                        avatarCaption.Text = "MUA 皮肤";
                        return;
                    }
                    catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or FormatException)
                    {
                    }
                }

                var metadata = await state.MuaAccounts.Metadata(token);
                var muaUri = await state.MuaAccounts.Skin(account, metadata, token);
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
                skinAvatar.Source = muaAvatar;
                avatarCaption.Text = "MUA 皮肤";
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
                await File.WriteAllBytesAsync(cacheFile, data, token);
                return;
            }

            if (state.AccountMode != 1)
            {
                if (File.Exists(state.OfflineSkin))
                {
                    var localBytes = await File.ReadAllBytesAsync(state.OfflineSkin, token);
                    if (revision != avatarRevision || token.IsCancellationRequested)
                        return;
                    skinAvatar.Source = AvatarRenderer.FromSkin(localBytes);
                    avatarCaption.Text = "本地皮肤";
                }

                return;
            }

            var skin = state.MicrosoftAccount?.SkinUrl;
            if (string.IsNullOrWhiteSpace(skin))
                return;
            var uri = AvatarRenderer.SkinUri(skin);
            var cache = Path.Combine(state.SettingsDirectory, "avatars");
            var file = AvatarRenderer.CachePath(cache, uri);
            if (!bypassCache && File.Exists(file))
            {
                try
                {
                    var cached = AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(file, token));
                    if (revision != avatarRevision)
                        return;
                    skinAvatar.Source = cached;
                    avatarCaption.Text = "正版皮肤";
                    return;
                }
                catch (Exception ex) when (ex is IOException or System.NotSupportedException or System.ArgumentException or FormatException)
                {
                }
            }

            avatarCaption.Text = "正在读取皮肤…";
            var bytes = await AvatarRenderer.Download(state.Http, uri, token);
            var avatar = AvatarRenderer.FromSkin(bytes);
            if (revision != avatarRevision || token.IsCancellationRequested)
                return;
            skinAvatar.Source = avatar;
            avatarCaption.Text = "正版皮肤";
            Directory.CreateDirectory(cache);
            await File.WriteAllBytesAsync(file, bytes, token);
        }
        catch (OperationCanceledException)
        {
            if (revision == avatarRevision)
                avatarCaption.Text = "头像暂不可用";
        }
        catch (Exception)
        {
            if (revision == avatarRevision)
                avatarCaption.Text = "头像暂不可用，可稍后刷新";
        }
    }

    internal async void RefreshAvatarClick(object s, RoutedEventArgs e)
    {
        var state = getState();
        if (isBusy())
            return;
        if (state.AccountMode == 1 && state.MicrosoftAccount != null)
            await refreshMicrosoftProfile();
        RefreshAvatar(true);
    }
}
