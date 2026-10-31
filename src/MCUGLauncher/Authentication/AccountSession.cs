using CmlLib.Core.Auth;
using BlockLaunch;
using MCUGLauncher.Models;
using MCUGLauncher.Services;
using MCUGLauncher.Ui;

namespace MCUGLauncher.Authentication;

/// <summary>
/// 账号会话。
/// </summary>
/// <remarks>
/// 依赖 MicrosoftAccountService、MuaAccountService、<see cref="ConfigStore"/>、<see cref="AppConfig"/> 与 <see cref="UiBridge"/>；登录是异步网络操作，状态变化通过 Changed 事件通知界面。
/// </remarks>
sealed class AccountSession
{
    readonly ConfigStore _store;
    readonly AppConfig _config;
    readonly UiBridge _bridge;

    public AccountSession(ConfigStore store, AppConfig config, UiBridge bridge, HttpClient http)
    {
        _store = store;
        _config = config;
        _bridge = bridge;

        var authDirectory = Path.Combine(store.DataDirectory, "auth");
        try
        {
            Microsoft = new MicrosoftAccountService(authDirectory, http);
            Mua = new MuaAccountService(authDirectory, http);
            if (config.AccountType == "mua")
            {
                var saved = Mua.Read();
                if (saved != null)
                {
                    Session = MuaAccountService.Session(saved);
                    config.Player = saved.Profile.Name;
                }
            }
        }
        catch
        {
            // 损坏的账号缓存不应阻止启动器打开。
            config.AccountType = "offline";
        }
    }

    public MicrosoftAccountService? Microsoft
    {
        get;
    }

    public MuaAccountService? Mua
    {
        get;
    }

    public MSession? Session
    {
        get; private set;
    }

    /// <summary>登录状态、玩家名或账号类型变化后触发，用于刷新界面状态。</summary>
    public event Action? Changed;

    /// <summary>
    /// 执行交互式微软登录。
    /// </summary>
    /// <remarks>
    /// 成功后写 AccountType=microsoft 与玩家名；失败只提示，不覆盖已保存账号。
    /// </remarks>
    public async Task LoginMicrosoftAsync()
    {
        if (Microsoft == null)
        {
            Fail("登录组件未就绪。");
            return;
        }
        _bridge.Send(new
        {
            type = "authResult",
            pending = true,
            message = "已打开微软登录页，请完成授权…"
        });
        try
        {
            var session = await Microsoft.Authenticate(true, CancellationToken.None);
            Session = session;
            _config.AccountType = "microsoft";
            _config.Player = session.Username ?? _config.Player;
            _store.Save(_config);
            _bridge.Send(new
            {
                type = "authResult",
                ok = true,
                message = $"已登录正版账号 {session.Username}"
            });
            Changed?.Invoke();
        }
        catch (Exception)
        {
            Fail("微软登录未完成。请检查网络后重试；已保存的账号不会被覆盖。");
        }
    }

    /// <summary>
    /// 静默刷新已保存的微软会话。
    /// </summary>
    /// <remarks>
    /// 仅 AccountType=microsoft 时执行；刷新失败会降级为离线账号。
    /// </remarks>
    public async Task RestoreMicrosoftAsync()
    {
        if (_config.AccountType != "microsoft" || Microsoft == null)
            return;
        try
        {
            var session = await Microsoft.Authenticate(false, CancellationToken.None);
            Session = session;
            _config.Player = session.Username ?? _config.Player;
            _store.Save(_config);
            Changed?.Invoke();
        }
        catch
        {
            Session = null;
            _config.AccountType = "offline";
            _store.Save(_config);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// 登录 MUA 外置账号。
    /// </summary>
    /// <param name="account">MUA 账号</param>
    /// <param name="password">MUA 密码</param>
    /// <remarks>
    /// 账号或密码为空直接失败；成功后写 AccountType=mua 与角色名。
    /// </remarks>
    public async Task LoginMuaAsync(string account, string password)
    {
        if (Mua == null)
        {
            Fail("登录组件未就绪。");
            return;
        }
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrEmpty(password))
        {
            Fail("请填写 MUA 账号和密码。");
            return;
        }
        _bridge.Send(new
        {
            type = "authResult",
            pending = true,
            message = "正在登录 MUA…"
        });
        try
        {
            var credential = await Mua.Login(account.Trim(), password,
                profiles => Task.FromResult<MuaProfile?>(profiles.FirstOrDefault()), CancellationToken.None);
            Session = MuaAccountService.Session(credential);
            _config.AccountType = "mua";
            _config.Player = credential.Profile.Name;
            _store.Save(_config);
            _bridge.Send(new
            {
                type = "authResult",
                ok = true,
                message = $"已登录 MUA 账号 {credential.Profile.Name}"
            });
            Changed?.Invoke();
        }
        catch (Exception)
        {
            Fail("MUA 登录失败。请检查账号、密码和网络后重试。");
        }
    }

    /// <summary>
    /// 退出当前账号。
    /// </summary>
    /// <remarks>
    /// 清空内存会话并把 AccountType 置为 offline，不删除磁盘上的令牌缓存。
    /// </remarks>
    public void Logout()
    {
        Session = null;
        _config.AccountType = "offline";
        _store.Save(_config);
        Changed?.Invoke();
    }

    void Fail(string message) => _bridge.Send(new
    {
        type = "authResult",
        ok = false,
        message
    });
}
