using System.Net.Http;

namespace BlockLaunch;

public sealed class AccountSkinService(AccountSessionService accounts, MuaAccountService mua, Preferences preferences)
{
    public async Task<byte[]?> Read(CancellationToken token)
    {
        if (!preferences.PreferMicrosoft && !preferences.PreferMua)
        {
            if (string.IsNullOrWhiteSpace(preferences.OfflineSkin) || !File.Exists(preferences.OfflineSkin))
                return null;
            if (new FileInfo(preferences.OfflineSkin).Length > 2 * 1024 * 1024)
                throw new IOException("皮肤文件超过 2 MiB。");
            return await File.ReadAllBytesAsync(preferences.OfflineSkin, token);
        }
        Uri? uri;
        if (preferences.PreferMua)
        {
            var credential = accounts.ReadMua();
            if (credential == null)
                return null;
            uri = await mua.Skin(credential, await mua.Metadata(token), token);
        }
        else
        {
            var value = accounts.ReadMicrosoft()?.SkinUrl;
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var source) || source.Host != "textures.minecraft.net"
                || source.Scheme is not ("http" or "https") || !source.AbsolutePath.StartsWith("/texture/", StringComparison.Ordinal)
                || source.UserInfo.Length != 0)
                throw new IOException("皮肤来源无效。");
            uri = new UriBuilder("https", "textures.minecraft.net") { Path = source.AbsolutePath }.Uri;
        }
        if (uri == null)
            return null;
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 2 * 1024 * 1024)
            throw new IOException("皮肤文件超过 2 MiB。");
        await using var input = await response.Content.ReadAsStreamAsync(token);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, token)) > 0)
        {
            if (output.Length + count > 2 * 1024 * 1024)
                throw new IOException("皮肤文件超过 2 MiB。");
            await output.WriteAsync(buffer.AsMemory(0, count), token);
        }
        return output.ToArray();
    }
}
