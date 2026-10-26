using BlockLaunch;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
public static class Program
{
    [STAThread] public static void Main()
    {
        var directory = Path.GetFullPath("work/v015-ui"); var root = Path.Combine(directory, "minecraft"); var settings = Path.Combine(directory, "settings");
        new SettingsStore(settings).Save(new Preferences { Root = root, Player = "FixtureUser", LastVersion = "fixture" });
        AtomicFile.WriteJson(Path.Combine(root, "versions", "1.21.1", "1.21.1.json"), new { id = "1.21.1", type = "release", libraries = Array.Empty<object>() });
        AtomicFile.WriteJson(Path.Combine(root, "versions", "fixture", "fixture.json"), new { id = "fixture", inheritsFrom = "1.21.1", libraries = new[] { new { name = "net.fabricmc:fabric-loader:0.19.5" } } });
        var catalog = new InstanceCatalog(root); catalog.GetOrCreate("fixture");
        using var http = new HttpClient(new Handler());
        if (ContentService.ReadManaged(catalog.DirectoryFor("fixture")).Count == 0) new ContentService(http).Install(new([Handler.Version("old")], new() { ["fixture-resource"] = "mod" }), catalog.DirectoryFor("fixture")).GetAwaiter().GetResult();
        var source = System.Xml.Linq.XDocument.Load("work/v092/BlockLaunch/App.xaml"); var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose, Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString()) };
        var window = new MainWindow(settings) { Title = "BlockLaunch 0.15 · 更新与升级隔离验收", Width = 1200, Height = 840 };
        typeof(MainWindow).GetField("http", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, new HttpClient(new Handler()));
        app.Run(window);
    }
}
sealed class Handler : HttpMessageHandler
{
    public static ContentVersion Version(string id)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(id);
        return new() { Id = id, ProjectId = "fixture-resource", Name = "隔离测试 Mod", VersionNumber = id, VersionType = "release", GameVersions = ["1.21.1"], Loaders = ["fabric"], Files = [new() { Filename = "fixture.jar", Url = "https://cdn.modrinth.com/" + id, Size = bytes.Length, Hashes = new() { ["sha512"] = Convert.ToHexString(SHA512.HashData(bytes)) } }] };
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.RequestUri!.Host == "cdn.modrinth.com") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(request.RequestUri.AbsolutePath.Trim('/')) });
        var json = request.RequestUri.AbsolutePath.EndsWith("/version") ? JsonSerializer.Serialize(new[] { Version("new") }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }) : "{\"project_type\":\"mod\",\"client_side\":\"required\"}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
    }
}
