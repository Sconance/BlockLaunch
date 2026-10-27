using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core.Auth;

namespace BlockLaunch;

public partial class MainWindow
{
    internal async Task Login()
    {
        await MicrosoftLogin(true);
    }

    async Task Launch()
    {
        if (game is
            {
                HasExited: false
            })
            throw new Exception("游戏正在运行，请先关闭当前游戏。");
        var rootBefore = prefs.Root;
        SavePreferences();
        if (!string.Equals(rootBefore, prefs.Root, StringComparison.OrdinalIgnoreCase))
        {
            SetupLauncher();
            await Refresh();
            throw new Exception("目录已切换，请重新选择版本后启动。");
        }

        MSession session;
        if (AccountModeControl.SelectedIndex == 1)
        {
            await EnsureMicrosoftLogin();
            session = online!;
        }
        else if (AccountModeControl.SelectedIndex == 2)
        {
            await EnsureMuaLogin();
            session = MuaAccountService.Session(muaAccount!);
        }
        else
        {
            if (!Regex.IsMatch(prefs.Player, "^[A-Za-z0-9_]{3,16}$"))
                throw new Exception("离线名称必须为 3–16 位英文字母、数字或下划线。");
            session = MSession.CreateOfflineSession(prefs.Player);
        }

        var id = Selected();
        if (!LocalVersions().Contains(id))
            throw new IOException("所选版本尚未安装完成，请到「游戏安装」下载 / 修复后再启动。");
        OperationStatusLabel.Text = "构建启动命令…";
        var directory = Instance();
        var descriptor = new InstanceCatalog(prefs.Root).GetOrCreate(id);
        GameDefaults.EnsureChinese(directory, Target().GameVersion);
        PrepareOfflineSkin(directory, Target().GameVersion);
        var launchSettings = descriptor.Launch ?? prefs.Launch;
        if (nextServerAddress != null)
        {
            launchSettings = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(launchSettings))!;
            launchSettings.Server = nextServerAddress;
        }

        var requirements = RequirementsFor(descriptor.LaunchProfileId);
        var preflight = await Task.Run(() => ModPreflight.Scan(directory, requirements.Minecraft, requirements.Loader, JavaDiscovery.RequiredMajor(prefs.Root, descriptor.LaunchProfileId), requirements.LoaderVersion, CurrentToken), CurrentToken);
        if (preflight.Findings.Count > 0 && !ConfirmModScan(preflight, "启动前检查"))
            return;
        AuthlibLaunch? external = null;
        if (AccountModeControl.SelectedIndex == 2)
        {
            var metadata = await muaAccounts.Metadata(CurrentToken);
            operations.Current?.Report("准备 MUA 认证组件…");
            var jar = await AuthlibLaunch.Prepare(Path.Combine(settingsDir, "authlib-injector"), http, CurrentToken);
            external = new(jar, metadata.Json);
        }

        var process = await LaunchService.Build(new(prefs.Root, directory, SelectedLaunchVersion(), launchSettings.Java, launchSettings.AutoMemory ? MemoryPolicy.Recommend() : launchSettings.RamMb, launchSettings.Width, launchSettings.Height, session, launchSettings, external), CurrentToken, http);
        CurrentToken.ThrowIfCancellationRequested();
        var access = typeof(MSession).GetProperty("AccessToken")?.GetValue(session)?.ToString() ?? "";
        using (OperationExecution.BeginCommit())
        {
            var currentSession = new GameSession(process, Path.Combine(settingsDir, "process-logs", descriptor.InstanceId + ".log"), QueueGameLine, new[] { access, AccountModeControl.SelectedIndex == 2 ? muaAccount?.ClientToken ?? "" : "" });
            var diagnosisContext = new DiagnosticContext(directory, descriptor.LaunchProfileId, Path.Combine(settingsDir, "process-logs", descriptor.InstanceId + ".log"), DateTime.UtcNow, Secrets: new[] { access, AccountModeControl.SelectedIndex == 2 ? muaAccount?.ClientToken ?? "" : "" });
            currentSession.Start();
            game = process;
            gameSession = currentSession;
            lastGameDirectory = directory;
            recentDiagnosticContext = diagnosisContext;
            _ = ObserveGame(currentSession, diagnosisContext);
            _ = gameWindowBehaviorController.Apply(process, launchSettings, currentSession);
        }

        Log("已启动 " + id + "，实例：" + directory);
    }

    async void LoginClick(object s, RoutedEventArgs e) => await Run(Login);

    async void LaunchClick(object s, RoutedEventArgs e) => await Run(Launch);

    void StopClick(object s, RoutedEventArgs e)
    {
        try
        {
            if (gameSession != null && !gameSession.RequestStop())
                Log("未能发送关闭请求，请在游戏内正常退出。");
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }
}
