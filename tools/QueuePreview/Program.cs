using BlockLaunch;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Windows;
public static class Program
{
    [STAThread] public static void Main()
    {
        var directory=Path.GetFullPath("work/v010-ui"); var root=Path.Combine(directory,"minecraft");
        var settings=Path.Combine(directory,"settings");
        new SettingsStore(settings).Save(new Preferences { SchemaVersion=2,Root=root, Player="FixtureUser" });
        var bytes=Enumerable.Range(0,16*1024*1024).Select(i=>(byte)(i%251)).ToArray();
        var queue=new OperationCoordinator(Path.Combine(settings,"tasks"));
        if(!queue.History().Any(r=>r.Download!=null))
        {
            var job=queue.Enqueue("隔离下载验收 · 合成资源包",root,"fixture",new() { Kind="content",Destination=Path.Combine(root,"fixture"),GameVersion="1.21.1",Loader="vanilla", Content=new() { Id="fixture-v1",ProjectId="fixture",Name="合成资源包",GameVersions=["1.21.1"],Files=[new() { Filename="fixture.zip",Primary=true,Size=bytes.Length,Url="https://cdn.modrinth.com/fixture.zip",Hashes=new() { ["sha512"]=Convert.ToHexString(SHA512.HashData(bytes)) } }] } });
            queue.ChangePending(job.Id,OperationState.Paused);
        }
        var source=System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml")); var ns=source.Root!.Name.Namespace;
        var dictionary=new System.Xml.Linq.XElement(ns+"ResourceDictionary",new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns+"x","http://schemas.microsoft.com/winfx/2006/xaml"),source.Root.Element(ns+"Application.Resources")!.Nodes());
        var app=new Application { ShutdownMode=ShutdownMode.OnMainWindowClose,Resources=(ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString()) };
        var window=new MainWindow(settings) { Title="BlockLaunch 0.10 · 下载队列隔离验收",Width=1180,Height=800 };
        typeof(MainWindow).GetField("http",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(window,new HttpClient(new Handler(bytes)));
        app.Run(window);
    }
}
sealed class Handler(byte[] bytes):HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        if(request.RequestUri!.Host=="api.modrinth.com") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent("{\"project_type\":\"resourcepack\",\"client_side\":\"optional\"}") });
        var offset=request.Headers.Range?.Ranges.Single().From??0;
        var response=new HttpResponseMessage(offset>0?HttpStatusCode.PartialContent:HttpStatusCode.OK) { Content=new StreamContent(new Slow(bytes,(int)offset)) };
        response.Content.Headers.ContentLength=bytes.Length-offset;
        if(offset>0) response.Content.Headers.ContentRange=new ContentRangeHeaderValue(offset,bytes.Length-1,bytes.Length);
        return Task.FromResult(response);
    }
}
sealed class Slow(byte[] bytes,int offset):MemoryStream(bytes)
{
    bool initialized;
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default)
    { if(!initialized) { Position=offset; initialized=true; } await Task.Delay(250,token); return await base.ReadAsync(buffer[..Math.Min(buffer.Length,65536)],token); }
}
