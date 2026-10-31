using BlockLaunch;
using CmlLib.Core.Auth;
using MCUGLauncher.Models;
using MCUGLauncher.Services;

namespace MCUGLauncher.UseCases;

internal sealed class LauncherContext : IDisposable
{
    readonly CancellationTokenSource lifetime = new();
    public CancellationToken Token => lifetime.Token;
    public ConfigStore Store
    {
        get;
    }
    public CommunityService Community { get; } = new();
    public HttpClient Http { get; } = new() { Timeout = TimeSpan.FromSeconds(30) };
    public MicrosoftAccountService? Microsoft
    {
        get; private set;
    }
    public MuaAccountService? Mua
    {
        get; private set;
    }
    public MSession? OnlineSession
    {
        get; set;
    }
    public AppConfig Config
    {
        get;
    }
    public Action<object> Send
    {
        get;
    }
    public Action RefreshState { get; set; } = () => { };

    public LauncherContext(Action<object> send, ConfigStore? store = null)
    {
        Send = send;
        Store = store ?? new ConfigStore();
        Config = Store.Load();
        var authDirectory = Path.Combine(Store.DataDirectory, "auth");
        try
        {
            Microsoft = new MicrosoftAccountService(authDirectory, Http);
            Mua = new MuaAccountService(authDirectory, Http);
            if (Config.AccountType == "mua")
            {
                var saved = Mua.Read();
                if (saved != null)
                {
                    OnlineSession = MuaAccountService.Session(saved);
                    Config.Player = saved.Profile.Name;
                }
            }
        }
        catch
        {
            // A damaged account cache must never prevent the launcher from opening.
            Config.AccountType = "offline";
        }
    }

    public void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Store.DataDirectory);
            File.AppendAllText(Path.Combine(Store.DataDirectory, "launcher.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
    public void Dispose()
    {
        lifetime.Cancel();
        Http.Dispose();
    }
}
