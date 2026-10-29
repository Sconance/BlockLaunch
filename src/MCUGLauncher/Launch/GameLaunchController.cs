using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BlockLaunch;

public sealed class GameLaunchController(IGameLaunchInteraction interaction)
{
    public async Task Launch(CancellationToken token)
    {
        if (interaction.IsGameRunning)
            throw new InvalidOperationException("游戏正在运行，请先关闭当前游戏。");
        token.ThrowIfCancellationRequested();
        await interaction.SaveSettings();
        token.ThrowIfCancellationRequested();
        var identity = await interaction.Authenticate();
        token.ThrowIfCancellationRequested();
        var selection = interaction.CaptureSelection();
        if (!selection.IsInstalled)
            throw new IOException("所选版本尚未安装完成，请到「游戏安装」下载 / 修复后再启动。");

        interaction.Report("构建启动命令…");
        var descriptor = new InstanceCatalog(selection.Root).GetOrCreate(selection.Version);
        GameDefaults.EnsureChinese(selection.InstanceDirectory, selection.GameVersion);
        OfflineSkinPackBuilder.Prepare(selection.Root, selection.InstanceDirectory, selection.OfflineSkin, selection.GameVersion, identity.IsOffline);
        var settings = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(descriptor.Launch ?? selection.DefaultSettings))!;
        if (selection.ServerAddress != null)
            settings.Server = selection.ServerAddress;

        var requirements = ProfileInspector.Read(selection.Root, descriptor.LaunchProfileId);
        var preflight = await Task.Run(() => ModPreflight.Scan(selection.InstanceDirectory, requirements.Minecraft, requirements.Loader, JavaDiscovery.RequiredMajor(selection.Root, descriptor.LaunchProfileId), requirements.LoaderVersion, token), token);
        if (preflight.Findings.Count > 0 && !interaction.ConfirmPreflight(preflight))
            return;
        token.ThrowIfCancellationRequested();

        AuthlibLaunch? external = null;
        if (identity.ReadExternalMetadata != null)
        {
            var metadata = await identity.ReadExternalMetadata(token);
            interaction.Report("准备 MUA 认证组件…");
            var jar = await AuthlibLaunch.Prepare(Path.Combine(selection.SettingsDirectory, "authlib-injector"), selection.Http, token);
            external = new(jar, metadata);
        }

        var process = await LaunchService.Build(new(selection.Root, selection.InstanceDirectory, descriptor.LaunchProfileId, settings.Java, settings.AutoMemory ? MemoryPolicy.Recommend() : settings.RamMb, settings.Width, settings.Height, identity.Session, settings, external), token, selection.Http);
        try
        {
            token.ThrowIfCancellationRequested();
            var access = identity.Session.AccessToken ?? "";
            var secrets = new[] { access, identity.ClientToken };
            var logFile = Path.Combine(selection.SettingsDirectory, "process-logs", descriptor.InstanceId + ".log");
            using (OperationExecution.BeginCommit())
            {
                var session = new GameSession(process, logFile, interaction.QueueOutput, secrets);
                var diagnostics = new DiagnosticContext(selection.InstanceDirectory, descriptor.LaunchProfileId, logFile, DateTime.UtcNow, Secrets: secrets);
                session.Start();
                interaction.Started(new(session, diagnostics, settings));
            }
        }
        catch
        {
            if (!HasStarted(process))
                process.Dispose();
            throw;
        }

        interaction.Log("已启动 " + selection.Version + "，实例：" + selection.InstanceDirectory);
    }

    public void Stop()
    {
        try
        {
            if (interaction.CurrentSession != null && !interaction.CurrentSession.RequestStop())
                interaction.Log("未能发送关闭请求，请在游戏内正常退出。");
        }
        catch (Exception ex)
        {
            interaction.Log(ex.Message);
        }
    }

    static bool HasStarted(Process process)
    {
        try
        {
            return process.Id > 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
