using BlockLaunch;
using System.Text.Json;
if (args.FirstOrDefault() is "--ui-prepare" or "--ui-cleanup")
{
    var uiStore = new ResourceBookmarkStore(args[1]); var id = args[2];
    if (!id.StartsWith("blocklaunch-ui-",StringComparison.Ordinal)) throw new Exception("Invalid fixture ID");
    if (args[0] == "--ui-prepare")
    {
        if(uiStore.Read().Any(i=>i.Project.Id==id)) throw new Exception("Fixture already exists");
        uiStore.Save(new(id,id,"界面验收样例（临时）","仅用于本地收藏标签交互测试","BlockLaunch",0),"mod","初始标签");
    }
    else
    {
        var fixture=uiStore.Read().FirstOrDefault(i=>i.Project.Id==id);
        if(fixture!=null && fixture.Project.Title!="界面验收样例（临时）")throw new Exception("Fixture identity changed");
        if(fixture!=null)uiStore.Remove(id);
    }
    Console.WriteLine("UI_FIXTURE_OPERATION_OK"); return;
}
var root = Path.Combine(Path.GetTempPath(), "BlockLaunch-collections-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
void Assert(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine(label + "_OK"); }
var storeDirectory = Path.Combine(root, "bookmarks"); var store = new ResourceBookmarkStore(storeDirectory);
var project = new ContentProject("sodium", "sodium", "Sodium", "Fast rendering", "CaffeineMC", 123) { Tags = "optimization" };
store.Save(project, "mod", "性能，客户端,性能;Optimization");
Assert(new ResourceBookmarkStore(storeDirectory).Read().Single().Labels.Length == 3, "BOOKMARK_LABELS_DEDUPLICATED_AND_PERSISTED");
Assert(store.Query("sodium 性能").Count == 1 && store.Query(label: "optimization").Count == 1 && store.Query(kind: "shader").Count == 0, "BOOKMARK_QUERY_AND_FILTER");
store.Save(project with { Title = "Sodium updated" }, "mod", "新标签"); Assert(store.Read().Count == 1 && store.Query(label: "新标签").Count == 1, "EDIT_BOOKMARK_NO_DUPLICATE");
var other = new ContentProject("other", "other", "Other", "desc", "author", 99);
var prioritized = store.Prioritize([other,project]); Assert(prioritized[0].Id == "sodium" && prioritized[0].DisplayTitle.StartsWith("★") && prioritized[0].CustomTags == "新标签", "SEARCH_PAGE_FAVORITES_FIRST");
try { store.Save(project, "mod", new string('x',41)); throw new Exception("Oversize tag accepted"); } catch (ArgumentException) { }
Assert(store.Read().Single().Labels[0] == "新标签", "INVALID_LABEL_PRESERVES_BOOKMARK");
var legacyDirectory=Path.Combine(root,"legacy-bookmarks"); Directory.CreateDirectory(legacyDirectory);
AtomicFile.WriteJson(Path.Combine(legacyDirectory,"resource-bookmarks.json"),new { SchemaVersion=1, Items=new[] { new { Project=project,Kind="mod",Labels=new[]{"旧标签"},Added=DateTimeOffset.UtcNow } } });
var legacyStore=new ResourceBookmarkStore(legacyDirectory);
Assert(legacyStore.Read().Single().Folder == "默认收藏夹" && legacyStore.Read().Single().Labels.Single()=="旧标签", "LEGACY_BOOKMARK_DEFAULT_FOLDER");
store.CreateFolder("常用模组"); store.CreateFolder("空收藏夹"); store.Save(project,"mod","性能","常用模组");
store.Save(project,"mod","编辑标签");
Assert(store.Read().Single().Folder=="常用模组" && new ResourceBookmarkStore(storeDirectory).Folders().Contains("空收藏夹"), "FOLDERS_AND_MOVE_PERSIST_AND_TAG_EDIT_PRESERVES_FOLDER");
store.Remove("sodium"); Assert(store.Read().Count == 0, "UNFAVORITE_PERSISTS");
var jsonFile = Path.Combine(storeDirectory,"resource-bookmarks.json"); File.WriteAllText(jsonFile,"corrupt");
try { store.Save(project,"mod",""); throw new Exception("Corrupt store overwritten"); } catch (JsonException) { }
Assert(File.ReadAllText(jsonFile) == "corrupt", "CORRUPT_BOOKMARK_FILE_NOT_OVERWRITTEN");
File.WriteAllText(jsonFile,"{\"SchemaVersion\":999,\"Items\":[]}");
try { store.Save(project,"mod",""); throw new Exception("Future store overwritten"); } catch (IOException) { }
Assert(File.ReadAllText(jsonFile).Contains("999"), "FUTURE_BOOKMARK_SCHEMA_PRESERVED");
var local = new[] { new LocalContent(Path.Combine(root,"mods","Sodium.jar"),"Sodium","fabric performance","已启用",null),new LocalContent(Path.Combine(root,"mods","Other.jar.disabled"),"Other","fabric","已禁用",null),new LocalContent(Path.Combine(root,"resourcepacks","Fresh.zip"),"Fresh","resourcepack","已启用",null) };
Assert(LocalContentSearch.Filter(local,"sodium PERFORMANCE","mods","已启用").Count == 1 && LocalContentSearch.Filter(local,"","mods","已禁用").Single().Name == "Other" && LocalContentSearch.Filter(local,"","resourcepacks").Count == 1, "LOCAL_CONTENT_QUERY_TYPE_AND_STATE");
var gameRoot = Path.Combine(root,"minecraft"); Directory.CreateDirectory(Path.Combine(gameRoot,"versions","1.21.1"));
AtomicFile.WriteJson(Path.Combine(gameRoot,"versions","1.21.1","1.21.1.json"),new { id="1.21.1",type="release" });
var catalog = new InstanceCatalog(gameRoot); var key = catalog.Create("Source", "1.21.1", "1.21.1"); var source = catalog.DirectoryFor(key);
void Put(string path,string content) { var full = Path.Combine(source,path); Directory.CreateDirectory(Path.GetDirectoryName(full)!); File.WriteAllText(full,content); }
Put("mods/demo.jar","mod"); Put("config/demo.toml","config"); Put("saves/World/level.dat","world"); Put("screenshots/test.png","image"); Put("servers.dat","server");
Put("accounts/microsoft-account.dat","private"); Put("logs/latest.log","private-log"); Put(".blocklaunch/downloads/cache","cache"); Put("settings.json","private-settings");
AtomicFile.WriteJson(Path.Combine(source,".blocklaunch-content.json"),new List<ManagedContent>{new(){ProjectId="demo",RelativePath="mods/demo.jar",Name="Demo"}});
var original = catalog.GetOrCreate(key); original.Launch = new LaunchSettings { Isolation="共享根目录", RamMb=6144,AutoMemory=false }; catalog.Save(original);
var effective = original.Launch;
var clone = await InstanceCloner.Copy(gameRoot,key,source,"1.21.1","Copy",effective,new()); var cloned = catalog.GetOrCreate(clone); var data = catalog.DirectoryFor(clone);
Assert(cloned.InstanceId != original.InstanceId && cloned.Launch?.Isolation == "独立实例" && cloned.Launch.RamMb == 6144 && cloned.DataDirectory.Length == 0, "CLONE_HAS_NEW_ID_AND_ISOLATED_SETTINGS");
Assert(File.ReadAllText(Path.Combine(data,"mods/demo.jar")) == "mod" && File.Exists(Path.Combine(data,"config/demo.toml")) && File.Exists(Path.Combine(data,".blocklaunch-content.json")), "CLONE_COPIES_CONTENT_CONFIG_AND_MANIFEST");
Assert(!Directory.Exists(Path.Combine(data,"saves")) && !Directory.Exists(Path.Combine(data,"screenshots")) && !File.Exists(Path.Combine(data,"servers.dat")) && !File.Exists(Path.Combine(data,"settings.json")) && !Directory.Exists(Path.Combine(data,"accounts")) && !Directory.Exists(Path.Combine(data,"logs")) && !Directory.Exists(Path.Combine(data,".blocklaunch")), "CLONE_EXCLUDES_UNSELECTED_AND_PRIVATE_DATA");
File.WriteAllText(Path.Combine(data,"mods/demo.jar"),"changed"); Assert(File.ReadAllText(Path.Combine(source,"mods/demo.jar")) == "mod", "CLONE_FILES_INDEPENDENT");
using(var profile = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameRoot,"versions",clone,clone+".json")))) Assert(profile.RootElement.GetProperty("inheritsFrom").GetString() == "1.21.1", "CLONE_INHERITS_INSTALLED_PROFILE");
var full = await InstanceCloner.Copy(gameRoot,key,source,"1.21.1","Full",effective,new(true,true,true)); var fullDirectory=catalog.DirectoryFor(full);
Assert(File.Exists(Path.Combine(fullDirectory,"saves/World/level.dat")) && File.Exists(Path.Combine(fullDirectory,"screenshots/test.png")) && File.Exists(Path.Combine(fullDirectory,"servers.dat")) && !File.Exists(Path.Combine(fullDirectory,"saves/World/session.lock")), "CLONE_OPTIONAL_WORLD_SHOTS_SERVER_AND_SKIP_LOCK");
var count = Directory.GetDirectories(Path.Combine(gameRoot,"instances")).Length; var profiles=Directory.GetDirectories(Path.Combine(gameRoot,"versions")).Length;
using(var cancel = new CancellationTokenSource())
{
    try { await InstanceCloner.Copy(gameRoot,key,source,"1.21.1","Cancel",effective,new(),_=>cancel.Cancel(),cancel.Token); throw new Exception("Clone cancel ignored"); } catch(OperationCanceledException) { }
    Assert(Directory.GetDirectories(Path.Combine(gameRoot,"instances")).Length==count && Directory.GetDirectories(Path.Combine(gameRoot,"versions")).Length==profiles && !Directory.EnumerateDirectories(Path.Combine(gameRoot,".blocklaunch-clone-staging")).Any(),"CLONE_CANCEL_LEAVES_NO_REGISTERED_INSTANCE");
}
using(var worldLock = new FileStream(Path.Combine(source,"saves/World/session.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite,FileShare.None))
{
    try { await InstanceCloner.Copy(gameRoot,key,source,"1.21.1","Locked",effective,new(true)); throw new Exception("Active world copied"); } catch(IOException) { }
    Assert(Directory.GetDirectories(Path.Combine(gameRoot,"instances")).Length==count, "CLONE_REFUSES_LOCKED_WORLD");
}
var provider = new BatchProvider();
ResourceBookmark Mark(string id,string kind="mod") => new(new ContentProject(id,id,id,"","",0),kind,[],DateTimeOffset.UtcNow);
var batch = await BookmarkBatch.Resolve(provider,[Mark("good"),Mark("empty"),Mark("broken"),Mark("good"),Mark("pack","modpack"),Mark("data","datapack")],"1.21.1","fabric",CancellationToken.None);
Assert(batch.Count==5 && batch.Count(i=>i.Version!=null)==3 && batch.First().Version!.VersionType=="release", "BATCH_DEDUPLICATES_SELECTS_RELEASE_AND_ISOLATES_ERRORS");
Assert(provider.Calls.Contains("pack///modpack") && provider.Calls.Contains("data/1.21.1/datapack/datapack"), "BATCH_PACK_INDEPENDENT_AND_DATAPACK_FILTER");
var vanilla=await BookmarkBatch.Resolve(provider,[Mark("good")],"1.21.1","vanilla",CancellationToken.None);
Assert(vanilla.Single().Version==null,"BATCH_REFUSES_MODS_FOR_VANILLA");
using(var cancel=new CancellationTokenSource()) { cancel.Cancel(); try { await BookmarkBatch.Resolve(provider,[Mark("good")],"1.21.1","fabric",cancel.Token); throw new Exception("Cancellation ignored"); } catch(OperationCanceledException){} }
Console.WriteLine("BATCH_CANCELLATION_OK");
Console.WriteLine("ALL_COLLECTION_CHECKS_PASSED");
public sealed class BatchProvider : IResourceProvider
{
    public List<string> Calls = [];
    public Task<List<ContentVersion>> Versions(string project,string game,string loader,string type) { Calls.Add($"{project}/{game}/{loader}/{type}"); if(project=="broken") throw new IOException("fixture failure"); return Task.FromResult(project=="empty" ? new List<ContentVersion>() : [new() { Id="beta",VersionType="beta" },new() { Id="stable",VersionType="release" }]); }
    public Task<SearchPage> Search(string q,string game,string loader,string type,int offset=0)=>throw new NotSupportedException();
    public Task<InstallPlan> Resolve(ContentVersion version,string game,string loader)=>throw new NotSupportedException();
}
