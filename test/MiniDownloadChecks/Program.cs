using BlockLaunch;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

static class Program
{
    [STAThread] static void Main()
    {
        var root = Path.GetFullPath("work/v0153-check-" + Guid.NewGuid().ToString("N"));
        new SettingsStore(root).Save(new() { Root = Path.Combine(root, "minecraft") });
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml")); var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown, Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString()) };
        var window = new MainWindow(root);
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        object Field(string name) => typeof(MainWindow).GetField(name, flags)!.GetValue(window)!;
        object? Call(string name, params object[] args) => typeof(MainWindow).GetMethod(name, flags)!.Invoke(window, args);
        void Assert(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
        void Wait(Task task) { var frame = new DispatcherFrame(); task.ContinueWith(_ => window.Dispatcher.BeginInvoke(() => frame.Continue = false)); Dispatcher.PushFrame(frame); task.GetAwaiter().GetResult(); }
        async Task Until(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(10); while(!condition()) { if(DateTime.UtcNow > end) throw new Exception("Timeout"); await Task.Delay(30); } }
        void Capture(string name) { var grid = (Grid)window.Content; grid.Background = (Brush)window.Background; grid.Measure(new Size(1060,700)); grid.Arrange(new Rect(0,0,1060,700)); grid.UpdateLayout(); var bmp = new RenderTargetBitmap(1060,700,96,96,PixelFormats.Pbgra32); bmp.Render(grid); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp)); using var file = File.Create("work/verify/" + name); encoder.Save(file); }
        try
        {
            var coordinator = (OperationCoordinator)Field("operations"); var prefs = (Preferences)Field("prefs");
            var bytes = new byte[1024 * 1024]; new Random(42).NextBytes(bytes); var hash = Convert.ToHexString(SHA512.HashData(bytes));
            var fixture = new Fixture(bytes); using var http = new HttpClient(new DownloadTransferHandler(fixture));
            var target = Path.Combine(root, "download.jar");
            var record = coordinator.Enqueue("secret-file-detail.jar", root, target, new() { Kind = "resource" });
            var second = coordinator.Enqueue("next", root, "next", new());
            OperationContext? context = null;
            Task<OperationRecord> Start() => coordinator.Run(record.Name, root, target, async c => { context = c; await ReliableDownloader.Download(http, new Uri("https://test.invalid/file"), target, hash, size: bytes.Length, progress: text => c.Report(text), token: c.Token); }, record);
            var task = Start();
            Wait(Until(() => context?.TransferredBytes > 160000)); Call("RefreshMiniDownloads", true);
            Assert(((TextBlock)Field("miniDownloadLabel")).Text.Contains('%'), "numeric progress available during transfer");
            Wait(Task.Delay(350)); Call("RefreshMiniDownloads", false);
            Assert(((TextBlock)Field("miniDownloadSpeed")).Text != "0 B/s", "speed is measured from actual consumed response bytes");
            var mini = (Border)Field("miniDownload");
            foreach(var tab in new[]{0,1,4,5,6,7}) { ((TabControl)Field("MainTabs")).SelectedIndex = tab; Assert(mini.Visibility == Visibility.Visible && mini.Parent == window.Content, "global download visible on page " + tab); }
            ((TabControl)Field("MainTabs")).SelectedIndex = 1; Capture("v0153-downloading.png");
            Assert(!((TextBlock)Field("miniDownloadLabel")).Text.Contains("secret"), "compact indicator excludes task and file details");
            Call("ToggleMiniDownload"); Wait(task); Call("RefreshMiniDownloads", true);
            Assert(task.Result.State == OperationState.Paused && prefs.DownloadsPaused, "pause retains job and holds entire queue");
            var partial = ReliableDownloader.PartialPath(target, hash); var offset = new FileInfo(partial).Length;
            Assert(offset > 0 && offset < bytes.Length, "partial file preserved after pause");
            typeof(MainWindow).GetField("queueReady", flags)!.SetValue(window, true); Wait((Task)Call("PumpDownloads")!);
            Assert(coordinator.History().Single(r => r.Id == second.Id).State == OperationState.Queued, "next job cannot start while queue paused");
            Assert(new SettingsStore(root).Load().DownloadsPaused, "pause state persists across restart"); Capture("v0153-paused.png");
            Call("ToggleMiniDownload");
            Assert(!prefs.DownloadsPaused && coordinator.History().Single(r => r.Id == record.Id).State == OperationState.Queued, "click resumes original paused job");
            task = Start(); Wait(task); Call("RefreshMiniDownloads", true);
            Assert(fixture.LastOffset == offset && File.ReadAllBytes(target).SequenceEqual(bytes), "HTTP range resume produces exact verified file");
            Assert(context!.TransferredBytes == bytes.Length - offset, "resume speed counts only newly received bytes");
            coordinator.ChangePending(second.Id, OperationState.Cancelled); Call("RefreshMiniDownloads", true);
            Assert(mini.Visibility == Visibility.Collapsed, "indicator hides when queue finishes");
            record = coordinator.Enqueue("commit", root, target, new());
            Wait(coordinator.Run("commit", root, target, c => { using(c.BeginCommit()) { Call("RefreshMiniDownloads", true); Assert(!((Button)Field("miniDownloadToggle")).IsEnabled, "pause disabled during atomic commit"); } return Task.CompletedTask; }, record));
            // Buffered HTTP also counts once, with response headers preserved.
            record = coordinator.Enqueue("buffer", root, target, new());
            Wait(coordinator.Run("buffer", root, target, async c => { using var response = await http.GetAsync("https://test.invalid/file"); Assert(response.Content.Headers.ContentLength == bytes.Length, "meter preserves length header"); await response.Content.ReadAsByteArrayAsync(); Assert(c.TransferredBytes == bytes.Length, "buffered response is not double counted"); }, record));
            Console.WriteLine("ALL_MINI_DOWNLOAD_CHECKS_PASSED");
        }
        catch(Exception ex) { Console.WriteLine(ex); Environment.ExitCode = 1; }
        finally { ((DispatcherTimer)Field("miniDownloadTimer")).Stop(); ((FileStream)Field("queueLease")).Dispose(); app.Shutdown(); }
    }
    sealed class Fixture(byte[] data) : HttpMessageHandler
    {
        public long LastOffset;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            LastOffset = request.Headers.Range?.Ranges.First().From ?? 0;
            var content = new StreamContent(new SlowStream(data, (int)LastOffset)); content.Headers.ContentLength = data.Length - LastOffset;
            if(LastOffset > 0) content.Headers.ContentRange = new ContentRangeHeaderValue(LastOffset, data.Length - 1, data.Length);
            return Task.FromResult(new HttpResponseMessage(LastOffset > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK) { Content = content });
        }
    }
    sealed class SlowStream(byte[] data, int offset) : MemoryStream(data, offset, data.Length-offset, false)
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) { await Task.Delay(30,token); return await base.ReadAsync(buffer[..Math.Min(buffer.Length,16384)],token); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => ReadAsync(buffer.AsMemory(offset,count),token).AsTask();
    }
}
