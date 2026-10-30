using BlockLaunch;

var root = Path.Combine(Path.GetTempPath(), "MCUG-resource-plan-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
void Check(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
    Console.WriteLine("PASS " + message);
}
void Reject(Action action, string message)
{
    try
    {
        action();
        throw new Exception("Expected rejection: " + message);
    }
    catch (IOException) { Console.WriteLine("PASS " + message); }
}
var version = new ContentVersion { ProjectId = "fixture", Id = "fixed", GameVersions = ["1.21.1"], Loaders = ["fabric"] };
var target = new PackRequirements("1.21.1", "fabric", "0.16.0");
var destination = Path.Combine(root, "instance-a");
var request = ResourceInstallPlanner.Create(version, target, "mod", destination);
Check(request.Kind == "content" && request.Destination == destination && request.Content?.Id == "fixed"
    && request.GameVersion == target.Minecraft && request.Loader == "fabric", "installation freezes selected instance directory, game version, loader and resource version");
Reject(() => ResourceInstallPlanner.Create(version, new("1.20.1", "fabric", ""), "mod", destination), "wrong Minecraft version rejected");
Reject(() => ResourceInstallPlanner.Create(version, new("1.21.1", "forge", ""), "mod", destination), "wrong loader rejected");
Reject(() => ResourceInstallPlanner.Create(version, target, "modpack", destination), "modpacks use import rather than an existing instance");
var pack = new ContentVersion { GameVersions = ["1.21.1"] };
Check(ResourceInstallPlanner.Create(pack, new("1.21.1", "vanilla", ""), "resourcepack", destination).ResourceKind == "resourcepack", "resource packs do not require a mod loader");
var data = new ContentVersion { GameVersions = ["1.21.1"], Loaders = ["datapack"] };
Check(ResourceInstallPlanner.Create(data, target, "datapack", destination).Loader == "datapack", "datapacks retain their installation kind");
void Profile(string key, string json)
{
    var directory = Path.Combine(root, "versions", key);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, key + ".json"), json);
}
Profile("renamed-fabric", """{"id":"renamed-fabric","clientVersion":"1.21.1","libraries":[{"name":"net.fabricmc:fabric-loader:0.16.0"}]}""");
Check(ProfileInspector.Read(root, "renamed-fabric") == target, "renamed standalone Fabric profile reports Minecraft metadata instead of its custom name");
Profile("renamed-optifine", """{"id":"renamed-optifine","libraries":[{"name":"optifine:OptiFine:1.20.1_HD_U_I6"}]}""");
var optifine = ProfileInspector.Read(root, "renamed-optifine");
Check(optifine.Minecraft == "1.20.1" && optifine.Loader == "optifine" && optifine.LoaderVersion == "HD_U_I6", "OptiFine profile reports loader and base version");
var queue = new OperationCoordinator(Path.Combine(root, "tasks"));
var queued = queue.Enqueue("queued", root, "fixture", new() { Kind = "resource" });
Check(!queue.DeleteHistory(queued.Id) && !queue.DeleteHistory("../escape"), "queued jobs and invalid history paths cannot be deleted");
queue.ChangePending(queued.Id, OperationState.Paused);
Check(!queue.DeleteHistory(queued.Id), "paused jobs cannot be deleted");
queue.ChangePending(queued.Id, OperationState.Cancelled);
var downloaded = Path.Combine(root, "downloaded.jar");
File.WriteAllText(downloaded, "retained");
Check(queue.DeleteHistory(queued.Id) && File.ReadAllText(downloaded) == "retained", "deleting cancelled history preserves downloaded files");
var waiting = queue.Enqueue("waiting", root, "fixture", new() { Kind = "resource" });
await queue.Run("finished", root, "fixture", _ => Task.CompletedTask);
var completed = queue.Enqueue("finished download", root, "fixture", new() { Kind = "resource" });
await queue.Run(completed.Name, root, completed.Target, _ => Task.CompletedTask, completed);
Check(queue.ClearFinishedDownloads() == 1 && queue.History().Any(record => record.Id == waiting.Id)
    && queue.History().Any(record => record.Download == null) && File.Exists(downloaded), "clear history removes only ended downloads and retains queued jobs, other history and files");
var installMode = true;
var selection = new ResourceInstallSelection(root, "instance-a", destination, target, "mod");
var currentRoot = root;
DownloadRequest? submitted = null;
string? submittedKey = null;
var directDownloads = 0;
var errors = new List<string>();
string? pickedWorld = null;
var controller = new ResourceInstallationController(() => installMode, () => selection, () => currentRoot,
    _ => directDownloads++, _ => pickedWorld, (_, key, plan) => { submitted = plan; submittedKey = key; }, errors.Add);
controller.Install(version);
Check(submitted?.Destination == destination && submittedKey == "instance-a", "UI coordinator enqueues captured instance through explicit ports without WPF");
submitted = null;
currentRoot = Path.Combine(root, "changed");
controller.Install(version);
Check(submitted == null && errors.Count == 1, "root switch before queue submission is rejected");
installMode = false;
controller.Install(version);
Check(directDownloads == 1 && submitted == null, "custom mode only submits an independent file download");
installMode = true;
currentRoot = root;
selection = selection with { Kind = "datapack" };
controller.Install(data);
Check(submitted == null, "cancelled world picker queues no datapack installation");
pickedWorld = Path.Combine(root, "outside-world");
Directory.CreateDirectory(pickedWorld);
File.WriteAllText(Path.Combine(pickedWorld, "level.dat"), "fixture");
controller.Install(data);
Check(submitted == null && errors.Count == 2, "datapack picker cannot install outside selected instance saves");
pickedWorld = Path.Combine(destination, "saves", "world");
Directory.CreateDirectory(pickedWorld);
File.WriteAllText(Path.Combine(pickedWorld, "level.dat"), "fixture");
controller.Install(data);
Check(submitted?.Destination == pickedWorld && submitted.Loader == "datapack", "valid selected-instance world queues a frozen datapack destination");
Check(InstanceView.VersionSummary("custom-name", new("custom-name", "vanilla", "")).StartsWith("版本未知"), "missing version metadata does not repeat a custom instance name");
Check(InstanceView.VersionSummary("renamed", target) == "1.21.1 · fabric 0.16.0", "instance subtitle contains only Minecraft and loader versions");
Console.WriteLine("ALL_RESOURCE_INSTALL_CHECKS_PASSED");
