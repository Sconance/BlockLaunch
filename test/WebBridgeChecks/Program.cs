using System.Net.Http;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using MCUGLauncher;
using MCUGLauncher.Services;
using MCUGLauncher.UseCases;

var fixture = Path.Combine(Path.GetTempPath(), "mcug-web-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixture);
var store = new ConfigStore(fixture);
var gameRoot = Path.Combine(fixture, "game");
var versionDir = Path.Combine(gameRoot, "versions", "custom-fabric");
Directory.CreateDirectory(versionDir);
File.WriteAllText(Path.Combine(versionDir, "custom-fabric.json"), """{"id":"custom-fabric","inheritsFrom":"1.21.1"}""");
store.Save(new MCUGLauncher.Models.AppConfig { Root = gameRoot, Player = "Fixture", LastVersion = "custom-fabric", LastInstance = "custom-fabric" });
var messages = new List<JsonElement>();
var desktop = new FakeDesktop();
using var application = new LauncherApplication(value => messages.Add(JsonSerializer.SerializeToElement(value)), desktop, store);
bool Has(string type) => messages.Any(m => m.GetProperty("type").GetString() == type);
void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
async Task Request(string json) { messages.Clear(); await application.HandleAsync(json); }

await Request("""{"type":"requestState"}""");
Check(Has("state") && messages[0].GetProperty("selectedVersion").GetString() == "custom-fabric", "state reflects fixture instance");
await Request("""{"type":"saveSettings","player":"Changed","ram":1,"width":99999,"height":1}""");
Check(Has("settingsSaved") && store.Load().Player == "Changed" && store.Load().Ram == 512 && store.Load().Width == 7680, "settings persist and clamp values");
await Request("""{"type":"saveSettings","player":"MustNotPersist","anonymous":"bad"}""");
Check(Has("error") && application.Config.Player == "Changed", "malformed settings rejected before mutation");
foreach (var bad in new[] { "[]", "{", "{}", "{\"type\":\"unknown\"}" })
{
    await Request(bad);
    Check(Has("error"), "invalid protocol has observable error: " + bad);
}
await Request("""{"type":"window","action":"minimize"}""");
Check(desktop.LastAction == "minimize", "native command routes through desktop boundary");
await Request("""{"type":"saveServer","name":"Fixture","address":"localhost:25565"}""");
Check(Has("serverResult") && store.Load().Servers.Any(s => s.Name == "Fixture"), "server save persists");
await Request("""{"type":"deleteServer","address":"localhost:25565"}""");
Check(!store.Load().Servers.Any(s => s.Name == "Fixture"), "server delete persists");
await Request("""{"type":"saveAnnouncements","items":[{"title":"fixture"}]}""");
Check(Has("community") && File.Exists(Path.Combine(fixture, "announcements.local.json")), "community local write and response");
var modDir = Path.Combine(versionDir, "mods");
Directory.CreateDirectory(modDir);
File.WriteAllText(Path.Combine(modDir, "fixture.jar"), "fixture");
await Request("""{"type":"toggleMod","file":"fixture.jar","enabled":false}""");
Check(File.Exists(Path.Combine(modDir, "fixture.jar.disabled")), "mod toggle routes to selected instance");
await Request("""{"type":"launch","version":".."}""");
Check(Has("error"), "instance traversal rejected");
application.Config.AccountType = "microsoft";
await Request("""{"type":"launch","version":"custom-fabric"}""");
Check(Has("launchResult") && messages.Last().GetProperty("ok").GetBoolean() == false, "unverified online account cannot fall back to offline launch");
application.Config.AccountType = "offline";
await Request("""{"type":"saveAnnouncements","items":"bad"}""");
Check(Has("error") && File.ReadAllText(Path.Combine(fixture, "announcements.local.json")).Contains("fixture"), "malformed community request cannot erase saved announcements");
Check(MainWindow.IsLocalPage("https://app.local/index.html?screen=mods") && !MainWindow.IsLocalPage("https://evil.example/") && !MainWindow.IsLocalPage("https://app.local:9999/"), "bridge origin whitelist");
Check(ProtocolValues.OfflineUuid("Notch") == "b50ad385829d3141a2167e7d7539ba7f", "offline UUID uses Java byte order");

using var handler = new ModFixtureHandler();
using var http = new HttpClient(handler);
var service = new ModService(http);
var saved = await service.DownloadAsync("fixture", "1.21.1", Path.Combine(fixture, "downloads"), CancellationToken.None, "neoforge");
Check(File.ReadAllText(saved) == "verified bytes" && handler.Query.Contains("1.21.1") && handler.Query.Contains("neoforge"), "mod download uses requested version and loader");
handler.Empty = true;
try
{
    await service.DownloadAsync("fixture", "1.21.1", fixture, CancellationToken.None);
    throw new Exception("empty response accepted");
}
catch (IOException) { Check(true, "empty file list rejected"); }
handler.Empty = false;
handler.FileName = "../escaped.jar";
try
{
    await service.DownloadAsync("fixture", "1.21.1", fixture, CancellationToken.None);
    throw new Exception("unsafe filename accepted");
}
catch (IOException) { Check(!File.Exists(Path.Combine(fixture, "escaped.jar")), "unsafe download filename rejected"); }
Console.WriteLine("ALL_WEB_BRIDGE_CHECKS_PASSED (isolated fixtures; no live account or gameplay)");

sealed class FakeDesktop : IDesktopHost
{
    public string LastAction = "";
    public void Window(string action) => LastAction = action;
    public void Drag()
    {
    }
    public void PickFolder(string target)
    {
    }
    public void PickFile(string target)
    {
    }
    public void ResetWindow()
    {
    }
}
sealed class ModFixtureHandler : HttpMessageHandler
{
    public string Query = "";
    public bool Empty;
    public string FileName = "fixture.jar";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.RequestUri!.Host == "files.example")
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("verified bytes") });
        Query = Uri.UnescapeDataString(request.RequestUri.Query);
        var files = Empty ? Array.Empty<object>() : new object[] { new { primary = true, filename = FileName, url = "https://files.example/fixture.jar" } };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new[] { new { files } }), Encoding.UTF8, "application/json") });
    }
}
