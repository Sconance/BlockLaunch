using System.IO;
using System.Net.Http;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed record AvatarState(
    int AccountMode,
    MuaCredential? MuaAccount,
    MuaAccountService MuaAccounts,
    SavedMicrosoftAccount? MicrosoftAccount,
    string? OnlineUsername,
    string PlayerName,
    string OfflineSkin,
    string SettingsDirectory,
    HttpClient Http);

internal sealed class AvatarController(
    TextBox playerName,
    Image skinAvatar,
    TextBlock avatarCaption,
    TextBlock avatarPlayerName,
    Func<AvatarState> getState)
{
    CancellationTokenSource? cancellation;
    int revision;

    public void UpdateName()
    {
        var state = getState();
        avatarPlayerName.Text = state.AccountMode == 2
            ? state.MuaAccount?.Profile.Name ?? "尚未登录"
            : state.AccountMode == 1
                ? state.OnlineUsername ?? state.MicrosoftAccount?.Username ?? "尚未登录"
                : string.IsNullOrWhiteSpace(playerName.Text)
                    ? "请输入角色名"
                    : playerName.Text;
    }

    public async void Refresh(bool bypassCache = false)
    {
        var state = getState();
        cancellation?.Cancel();
        cancellation?.Dispose();
        var currentCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        cancellation = currentCancellation;
        var token = currentCancellation.Token;
        var currentRevision = ++revision;
        UpdateName();
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
                        if (currentRevision != revision)
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
                })
                    data = await AvatarRenderer.Download(skinHttp, muaUri, token);
                var muaAvatar = AvatarRenderer.FromSkin(data);
                if (currentRevision != revision || token.IsCancellationRequested)
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
                    if (currentRevision != revision || token.IsCancellationRequested)
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
                    if (currentRevision != revision)
                        return;
                    skinAvatar.Source = cached;
                    avatarCaption.Text = "正版皮肤";
                    return;
                }
                catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException or FormatException)
                {
                }
            }

            avatarCaption.Text = "正在读取皮肤…";
            var bytes = await AvatarRenderer.Download(state.Http, uri, token);
            var avatar = AvatarRenderer.FromSkin(bytes);
            if (currentRevision != revision || token.IsCancellationRequested)
                return;
            skinAvatar.Source = avatar;
            avatarCaption.Text = "正版皮肤";
            Directory.CreateDirectory(cache);
            await File.WriteAllBytesAsync(file, bytes, token);
        }
        catch (OperationCanceledException)
        {
            if (currentRevision == revision)
                avatarCaption.Text = "头像暂不可用";
        }
        catch (Exception)
        {
            if (currentRevision == revision)
                avatarCaption.Text = "头像暂不可用，可稍后刷新";
        }
    }

    public void Cancel()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }
}
