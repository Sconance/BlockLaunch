namespace BlockLaunch;

public static class MicrosoftAuthorizationPolicy
{
    public static bool IsAuthorization(Uri uri) => uri.Scheme == "https" && uri.UserInfo.Length == 0 && uri.Host is "login.live.com" or "login.microsoftonline.com";
}
