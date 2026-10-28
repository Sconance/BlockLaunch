using System.IO;
using System.Net.Http;

namespace BlockLaunch;

public sealed record DownloadExecutionServices(
    HttpClient Http,
    NetworkSettings Network,
    Func<string, string, CancellationToken, Task> ProtectWorlds,
    Func<LaunchSettings> DefaultLaunchSettings,
    Func<string, bool> IsCurrentRoot,
    Func<string, string?, Task> RefreshInstalledContent,
    Action<string> Log);

public sealed class DownloadTaskExecutor(DownloadExecutionServices services)
{
    public async Task Execute(OperationRecord record, OperationContext context)
    {
        var request = record.Download ?? throw new IOException("任务缺少下载参数。");
        var root = Path.GetFullPath(record.Root);
        var token = context.Token;
        token.ThrowIfCancellationRequested();
        Action<string> report = message => context.Report(message);
        if (request.BackupInstanceDirectory.Length > 0)
            await services.ProtectWorlds(root, request.BackupInstanceDirectory, token);

        context.Report("恢复安装记录并校验已有文件…");
        if (request.Kind != "resource")
            foreach (var message in await Task.Run(() => InstallationRecovery.Recover(root), token))
                services.Log(message);

        string? installed = null;
        async Task<string> InstallGame(PackRequirements requirements) =>
            await new GameInstaller(services.Http, root, request.Java, report, token, services.Network).Install(requirements);

        var packs = new PackService(services.Http, token);
        switch (request.Kind)
        {
            case "resource":
                if (InstanceUninstaller.Inside(request.Destination, root) || Path.GetFullPath(request.Destination).Equals(root, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("请选择游戏目录之外的独立下载目录。");
                var downloaded = await ResourceDownloads.Download(services.Http, request.Destination, request.ResourceKind, request.Content ?? throw new IOException("资源任务缺少版本。"), report, token);
                context.Report("已保存：" + downloaded);
                services.Log("资源已保存：" + downloaded);
                break;
            case "game":
                installed = await InstallGame(new(request.Version, request.Loader, request.LoaderVersion));
                break;
            case "pack":
            case "import":
                var file = request.File;
                if (request.Kind == "import" && !ContentTransaction.Hash(file).Equals(request.SourceHash, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("原整合包文件已改变，请重新加入任务。");
                if (request.Kind == "pack")
                {
                    var content = request.Content ?? throw new IOException("整合包任务缺少版本信息。");
                    var asset = content.Files.FirstOrDefault(item => item.Primary) ?? content.Files.FirstOrDefault() ?? throw new IOException("没有整合包文件。");
                    if (!asset.Filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase))
                        throw new IOException("此版本没有 .mrpack 文件。");
                    file = Path.Combine(root, ".blocklaunch-downloads", ContentService.SafeFile(asset.Filename));
                    await packs.Download(asset.Url, file, asset.Hashes.GetValueOrDefault("sha512") ?? "", report);
                }

                var result = await packs.Import(file, root, InstallGame, request.Optional, report, request.PackInstance);
                installed = result.InstanceId;
                var catalog = new InstanceCatalog(root);
                var descriptor = catalog.GetOrCreate(installed);
                if (request.PackDisplayName.Length > 0)
                {
                    descriptor.DisplayName = request.PackDisplayName;
                    catalog.Save(descriptor);
                }

                break;
            case "content":
                var service = new ContentService(services.Http, token);
                var plan = request.PreparedPlan ?? await service.Resolve(request.Content ?? throw new IOException("任务缺少资源版本。"), request.GameVersion, request.Loader);
                await service.Install(plan, request.Destination, report, request.ContentFingerprint);
                break;
            default:
                throw new IOException("不支持的下载任务类型。");
        }

        if (installed != null)
        {
            try
            {
                var catalog = new InstanceCatalog(root);
                var descriptor = catalog.GetOrCreate(installed);
                var directory = (descriptor.Launch ?? services.DefaultLaunchSettings()).Isolation == "共享根目录" ? root : catalog.DirectoryFor(installed);
                GameDefaults.EnsureChinese(directory, request.Version, request.Kind is "pack" or "import");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                services.Log("游戏已安装，默认语言暂未写入，启动时会重试：" + ex.Message);
            }
        }

        if (request.Kind != "resource" && services.IsCurrentRoot(root))
        {
            try
            {
                await services.RefreshInstalledContent(root, installed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                services.Log("安装完成，列表刷新暂不可用：" + ex.Message);
            }
        }

        services.Log("下载任务完成：" + record.Name);
    }
}
