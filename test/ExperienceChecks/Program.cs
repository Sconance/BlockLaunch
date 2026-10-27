using BlockLaunch;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

static class Program
{
    static void Check(bool ok, string label)
    {
        if (!ok)
            throw new Exception(label);
        Console.WriteLine("PASS " + label);
    }

    [STAThread]
    static void Main()
    {
        var root = Path.GetFullPath("work/v016-experience-中文 空格-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Core(root).GetAwaiter().GetResult();
            Ui(root);
            Console.WriteLine("ALL_EXPERIENCE_CHECKS_PASSED");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Environment.ExitCode = 1;
        }
    }

    static async Task Core(string root)
    {
        foreach (var pair in new[] {
            ("1.12.2", 8),
            ("1.17.1", 16),
            ("1.20.4", 17),
            ("1.20.5", 21),
            ("1.21.1", 21)
        }

        )
        {
            AtomicFile.WriteJson(Path.Combine(root, "versions", pair.Item1, pair.Item1 + ".json"), new
            {
                id = pair.Item1
            });
            Check(JavaDiscovery.RequiredMajor(root, pair.Item1) == pair.Item2, "legacy Java requirement fallback " + pair.Item1);
        }

        var directory = Path.Combine(root, "instance");
        var mods = Path.Combine(directory, "mods");
        Directory.CreateDirectory(mods);
        void Jar(string name, Dictionary<string, byte[]> files)
        {
            using var zip = ZipFile.Open(Path.Combine(mods, name), ZipArchiveMode.Create);
            foreach (var pair in files)
            {
                using var output = zip.CreateEntry(pair.Key).Open();
                output.Write(pair.Value);
            }
        }

        byte[] Json(object obj) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
        Jar("base.jar", new()
        {
            ["fabric.mod.json"] = Json(new
            {
                schemaVersion = 1,
                id = "base",
                version = "1.2.3",
                depends = new
                {
                    minecraft = ">=1.20 <1.22",
                    java = ">=17"
                }
            })
        });
        var clean = ModPreflight.Scan(directory, "1.21.1", "fabric", 21, "0.16.12");
        Check(clean.Findings.Count == 0, "Fabric metadata requirements match game and Java");
        Jar("dependent.jar", new()
        {
            ["fabric.mod.json"] = Json(new
            {
                id = "dependent",
                version = "1",
                depends = new
                {
                    missing = "*"
                }
            })
        });
        Check(ModPreflight.Scan(directory, "1.21.1", "fabric", 21).Findings.Any(f => f.Message.Contains("缺少前置 missing")), "missing local dependency identifies exact file and ID");
        Jar("duplicate.jar", new()
        {
            ["fabric.mod.json"] = Json(new
            {
                id = "base",
                version = "2"
            })
        });
        Check(ModPreflight.Scan(directory, "1.21.1", "fabric", 21).Findings.Any(f => f.Message.Contains("重复的 Mod ID")), "duplicate enabled IDs detected");
        File.Move(Path.Combine(mods, "duplicate.jar"), Path.Combine(mods, "duplicate.jar.disabled"));
        Check(!ModPreflight.Scan(directory, "1.21.1", "fabric", 21).Findings.Any(f => f.Message.Contains("重复的")), "disabled JAR excluded");
        using (var nested = new MemoryStream())
        {
            using (var zip = new ZipArchive(nested, ZipArchiveMode.Create, true))
            {
                using var stream = zip.CreateEntry("fabric.mod.json").Open();
                stream.Write(Json(new
                {
                    id = "missing",
                    version = "1"
                }));
            }

            Jar("bundler.jar", new()
            {
                ["fabric.mod.json"] = Json(new
                {
                    id = "bundler",
                    version = "1",
                    jars = new[] { new { file = "META-INF/jars/library.jar" } }
                }),
                ["META-INF/jars/library.jar"] = nested.ToArray()
            });
        }

        Check(!ModPreflight.Scan(directory, "1.21.1", "fabric", 21).Findings.Any(f => f.Message.Contains("缺少前置 missing")), "nested JAR satisfies bundled dependency");
        Check(ModPreflight.Scan(directory, "1.19.4", "forge", 17).Findings.Any(f => f.Message.Contains("当前为 forge")), "wrong loader reported");
        Jar("broken.jar", new()
        {
            ["fabric.mod.json"] = Encoding.UTF8.GetBytes("{}")
        });
        Check(ModPreflight.Scan(directory, "1.21.1", "fabric", 21).Findings.Any(f => f.Severity == "提示" && f.File == "broken.jar"), "malformed metadata produces unknown without aborting scan");
        var forge = Path.Combine(root, "forge");
        Directory.CreateDirectory(Path.Combine(forge, "mods"));
        using (var zip = ZipFile.Open(Path.Combine(forge, "mods", "forge.jar"), ZipArchiveMode.Create))
        {
            using var stream = new StreamWriter(zip.CreateEntry("META-INF/mods.toml").Open());
            stream.Write("[[mods]]\nmodId=\"forge_test\"\nversion=\"1.0\"\n[[dependencies.forge_test]]\nmodId=\"minecraft\"\nmandatory=true\nversionRange=\"[1.20,1.21)\"\nside=\"BOTH\"\n[[dependencies.forge_test]]\nmodId=\"server_only\"\nmandatory=true\nversionRange=\"[1,)\"\nside=\"SERVER\"\n");
        }

        var forgeScan = ModPreflight.Scan(forge, "1.21.1", "forge", 21, "52.1.20");
        Check(forgeScan.Findings.Count == 1 && forgeScan.Findings[0].Message.Contains("minecraft"), "Forge TOML Maven range enforced; server-only requirement ignored");
        foreach (var test in new[] {
            ("1.2.3", "^1.2", true),
            ("2.0", "^1.2", false),
            ("0.3.0", "^0.2.1", false),
            ("1.2.9", "~1.2", true),
            ("1.3", "~1.2", false),
            ("1.21.1", "1.21.x", true),
            ("1.21", "[1.20,1.21)", false),
            ("1.20.5", ">=1.20 <1.21", true),
            ("1.2-beta.2", ">1.2-beta.1", true)
        }

        )
            Check(ModPreflight.Matches(test.Item1, test.Item2) == test.Item3, "version predicate " + test.Item1 + " " + test.Item2);
        Check(ModPreflight.Matches("snapshot-name", ">=1.20") == null, "unsupported version remains unknown");
        var world = Path.Combine(directory, "saves", "测试世界");
        Directory.CreateDirectory(world);
        WriteWorld(Path.Combine(world, "level.dat"));
        File.WriteAllText(Path.Combine(world, "data.txt"), "original");
        var metadata = WorldMetadata.Read(Path.Combine(world, "level.dat"));
        Check(metadata.Name == "测试世界" && metadata.Version == "1.21.1" && metadata.DataVersion == 3955, "bounded NBT reads world name version and data version");
        Check(metadata.Warning("1.20.1").Contains("降级"), "world downgrade warning");
        var backups = Path.Combine(root, "backups");
        var backup = await SaveProtection.Create(directory, backups, "test", 2);
        Check(backup!.Worlds.Count == 1, "backup captures all worlds and manifest");
        var original = SaveProtection.ArchivePath(backups, directory, backup, backup.Worlds[0]);
        var originalBytes = File.ReadAllBytes(original);
        using (var locked = new FileStream(Path.Combine(world, "session.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            try
            {
                await SaveProtection.Create(directory, backups, "locked", 2);
                throw new Exception("locked world accepted");
            }
            catch (IOException)
            {
                Check(File.ReadAllBytes(original).SequenceEqual(originalBytes), "active world backup rejected without altering prior backup");
            }
        }

        await SaveProtection.Create(directory, backups, "second", 2);
        var third = await SaveProtection.Create(directory, backups, "third", 2);
        Check(SaveProtection.List(backups, directory).Count == 2 && !File.Exists(original), "retention rotates only completed owned snapshots");
        var archive = SaveProtection.ArchivePath(backups, directory, third!, third!.Worlds[0]);
        File.WriteAllText(Path.Combine(world, "data.txt"), "changed");
        var restored = await WorldArchives.Import(directory, archive, "恢复");
        Check(restored != world && File.ReadAllText(Path.Combine(restored, "data.txt")) == "original" && File.ReadAllText(Path.Combine(world, "data.txt")) == "changed", "restore creates independent world and preserves current save");
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            try
            {
                await SaveProtection.Create(directory, backups, "cancelled", 2, cancelled.Token);
                throw new Exception("cancellation ignored");
            }
            catch (OperationCanceledException)
            {
                Check(SaveProtection.List(backups, directory).Count == 2, "cancelled backup leaves complete snapshots intact");
            }
        }

        Check(ServerEndpoint.Parse("[::1]:25566") == new ServerEndpoint("::1", 25566) && ServerEndpoint.Parse("example.com").Port == 25565, "server address IPv6 and default port");
        foreach (var bad in new[] {
            "host:0",
            "https://example.com",
            "foo@host",
            "[x]:12",
            "host:65536"
        }

        )
        {
            try
            {
                ServerEndpoint.Parse(bad);
                throw new Exception("invalid endpoint accepted");
            }
            catch (IOException)
            {
                Console.WriteLine("PASS reject endpoint " + bad);
            }
        }

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            await ReadPacket(stream);
            await ReadPacket(stream);
            var json = Encoding.UTF8.GetBytes("{\"version\":{\"name\":\"Fixture\",\"protocol\":767},\"players\":{\"online\":3,\"max\":20},\"description\":{\"text\":\"Hello\",\"extra\":[{\"text\":\" world\"}]}}");
            using var data = new MemoryStream();
            data.WriteByte(0);
            Var(data, json.Length);
            data.Write(json);
            await SendPacket(stream, data.ToArray());
            var ping = await ReadPacket(stream);
            await SendPacket(stream, ping);
        });
        var status = await ServerFavorites.Ping("127.0.0.1:" + port);
        await server;
        listener.Stop();
        Check(status.Description == "Hello world" && status.Online == 3 && status.Maximum == 20 && status.LatencyMs >= 0, "Minecraft status handshake JSON and ping echo round trip");
        var coordinator = new OperationCoordinator(Path.Combine(root, "tasks"));
        var a = coordinator.Enqueue("a", root, "", new());
        var b = coordinator.Enqueue("b", root, "", new());
        Check(coordinator.MovePending(b.Id, -1), "queued task can move before older task");
        Check(coordinator.History().Where(r => r.State == OperationState.Queued).OrderBy(r => r.QueueOrder).First().Id == b.Id, "queue ordering persists on disk");
        var fixture = new BytesHandler(new byte[64 * 1024]);
        using var http = new HttpClient(new DownloadTransferHandler(fixture, () => 64));
        var watch = Stopwatch.StartNew();
        await coordinator.Run("rate", root, "", async c =>
        {
            using var response = await http.GetAsync("https://fixture.invalid/file", HttpCompletionOption.ResponseHeadersRead, c.Token);
            await response.Content.CopyToAsync(Stream.Null, c.Token);
            Check(c.TransferredBytes == 65536, "metered limited transfer counts exact bytes");
        }, a);
        Check(watch.ElapsedMilliseconds >= 850 && watch.ElapsedMilliseconds < 6000, "aggregate transfer limiter enforces configured rate");
    }

    static void Ui(string root)
    {
        new SettingsStore(root).Save(new()
        {
            Root = Path.Combine(root, "minecraft")
        });
        var source = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));
        var ns = source.Root!.Name.Namespace;
        var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), source.Root.Element(ns + "Application.Resources")!.Nodes());
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
            Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString())
        };
        var window = new MainWindow(root);
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        object Field(string name) => typeof(MainWindow).GetField(name, flags)!.GetValue(window)!;
        object? Call(string name, params object[] args) => typeof(MainWindow).GetMethod(name, flags)!.Invoke(window, args);
        void Wait(Task task)
        {
            var frame = new DispatcherFrame();
            task.ContinueWith(_ => window.Dispatcher.BeginInvoke(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
        }

        try
        {
            var coordinator = (OperationCoordinator)Field("operations");
            var record = coordinator.Enqueue("background", root, "", new()
            {
                Kind = "resource"
            });
            var completion = new TaskCompletionSource();
            var download = coordinator.Run("background", root, "", async c =>
            {
                c.Report("fixture", 30);
                await completion.Task;
            }, record);
            typeof(MainWindow).GetField("busy", flags)!.SetValue(window, true);
            Call("SetInputs", false);
            Check(((TextBox)Field("SearchQuery")).IsEnabled && ((ListBox)Field("SearchResults")).IsEnabled && !((TextBox)Field("GameRoot")).IsEnabled, "background download allows browsing but locks mutable root");
            var previous = (HttpClient)Field("http");
            using var http = new HttpClient(new SearchHandler());
            typeof(MainWindow).GetField("http", flags)!.SetValue(window, http);
            Call("NavigateDownloads", "Mod");
            Wait((Task)Call("RunBrowse", (Func<Task>)(() => (Task)Call("SearchContent")!))!);
            Check(((ListBox)Field("SearchResults")).Items.Count == 1 && coordinator.Current?.Record.Id == record.Id, "resource search completes concurrently without replacing download context");
            ((ListBox)Field("SearchResults")).SelectedIndex = 0;
            Wait(Task.Delay(100));
            Check(((ComboBox)Field("ProjectVersions")).Items.Count == 1, "project details load during background download");
            Call("QueueContent", new ContentVersion { Id = "v", ProjectId = "p", Files = [new() { Filename = "mod.jar", Url = "https://cdn.modrinth.com/test.jar", Size = 1, Hashes = new() { ["sha512"] = new string('a', 128) } }] });
            Check(coordinator.History().Any(r => r.Download?.Content?.Id == "v" && r.State == OperationState.Queued), "additional resource queued during active transfer");
            Call("ResetSearch");
            Wait((Task)Call("RunBrowse", (Func<Task>)(() => (Task)Call("SearchContent")!))!);
            Check(((ListBox)Field("SearchResults")).Items.Count == 1, "browse cancellation lifecycle can search again");
            Call("RefreshOperations");
            var tasks = (ListBox)Field("TaskHistoryList");
            var pending = coordinator.History().First(r => r.State == OperationState.Queued);
            tasks.SelectedItem = tasks.Items.OfType<OperationRecord>().First(r => r.Id == pending.Id);
            var prefs = (Preferences)Field("prefs");
            prefs.DarkMode = true;
            Call("ThemeWindow", window);
            Check(((SolidColorBrush)window.Background).Color.R < 50, "dark theme applies");
            var content = (Grid)window.Content;
            void Capture(string name, int width, int height)
            {
                content.Background = window.Background;
                content.Measure(new Size(width, height));
                content.Arrange(new Rect(0, 0, width, height));
                content.UpdateLayout();
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(content);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create("work/verify/" + name);
                encoder.Save(output);
            }

            Capture("v016-dark-download.png", 1060, 700);
            prefs.DarkMode = false;
            Call("ThemeWindow", window);
            Capture("v016-compact-download.png", 820, 600);
            Check(((SolidColorBrush)window.Background).Color.R > 200, "light theme restores original brushes");
            var menu = (MenuItem)Field("moreMenu");
            Check(menu.Items.OfType<MenuItem>().Any(i => Equals(i.Header, "Java 管理")) && menu.Items.OfType<MenuItem>().Any(i => Equals(i.Header, "服务器收藏")), "utility features reachable through More menu");
            completion.SetResult();
            Wait(download);
            typeof(MainWindow).GetField("busy", flags)!.SetValue(window, false);
            typeof(MainWindow).GetField("http", flags)!.SetValue(window, previous);
            Wait(coordinator.Run("UI contextual backup", root, "", async context =>
            {
                await Task.Run(() => SaveProtection.Create(Path.Combine(root, "instance"), Path.Combine(root, "ui-backups"), "UI context", 2, context.Token));
            }));
            Check(SaveProtection.List(Path.Combine(root, "ui-backups"), Path.Combine(root, "instance")).Count == 1, "backup commit works while UI observes operation history");
            foreach (var item in coordinator.History().Where(r => r.State == OperationState.Queued))
                coordinator.ChangePending(item.Id, OperationState.Cancelled);
            prefs.ResourceDownloadDirectory = Path.Combine(root, "downloaded-resources");
            typeof(MainWindow).GetField("http", flags)!.SetValue(window, http);
            var data = new byte[65536];
            Call("QueueContent", new ContentVersion { Id = "pump", ProjectId = "p", Files = [new() { Filename = "pump.jar", Url = "https://cdn.modrinth.com/pump.jar", Size = data.Length, Hashes = new() { ["sha512"] = Convert.ToHexString(System.Security.Cryptography.SHA512.HashData(data)) } }] });
            typeof(MainWindow).GetField("queueReady", flags)!.SetValue(window, true);
            var pump = (Task)Call("PumpDownloads")!;
            Check(((TextBox)Field("SearchQuery")).IsEnabled && coordinator.Current?.Record.Download?.Kind == "resource", "real queue pump leaves search enabled after acquiring operation");
            Wait((Task)Call("RunBrowse", (Func<Task>)(() => (Task)Call("SearchContent")!))!);
            Wait(pump);
            Check(coordinator.History().Any(r => r.Download?.Content?.Id == "pump" && r.State == OperationState.Completed), "real pump completes verified resource while search runs");
            typeof(MainWindow).GetField("queueReady", flags)!.SetValue(window, false);
            typeof(MainWindow).GetField("http", flags)!.SetValue(window, previous);
        }
        finally
        {
            ((DispatcherTimer)Field("miniDownloadTimer")).Stop();
            ((FileStream)Field("queueLease")).Dispose();
            app.Shutdown();
        }
    }

    static void WriteWorld(string file)
    {
        using var output = File.Create(file);
        using var zip = new GZipStream(output, CompressionLevel.Fastest);
        using var writer = new BinaryWriter(zip);
        void Text(string s)
        {
            var b = Encoding.UTF8.GetBytes(s);
            writer.Write((byte)(b.Length >> 8));
            writer.Write((byte)b.Length);
            writer.Write(b);
        }

        void Int(int n)
        {
            var b = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(b, n);
            writer.Write(b);
        }

        writer.Write((byte)10);
        Text("");
        writer.Write((byte)10);
        Text("Data");
        writer.Write((byte)8);
        Text("LevelName");
        Text("测试世界");
        writer.Write((byte)3);
        Text("DataVersion");
        Int(3955);
        writer.Write((byte)10);
        Text("Version");
        writer.Write((byte)8);
        Text("Name");
        Text("1.21.1");
        writer.Write((byte)1);
        Text("Snapshot");
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
    }

    static void Var(Stream stream, int n)
    {
        do
        {
            var b = (byte)(n & 127);
            n >>= 7;
            stream.WriteByte((byte)(n > 0 ? b | 128 : b));
        }
        while (n > 0);
    }

    static async Task<byte[]> ReadPacket(Stream stream)
    {
        int n = 0;
        var b = new byte[1];
        for (var i = 0; i < 5; i++)
        {
            await stream.ReadExactlyAsync(b);
            n |= (b[0] & 127) << (i * 7);
            if (b[0] < 128)
                break;
        }

        var data = new byte[n];
        await stream.ReadExactlyAsync(data);
        return data;
    }

    static async Task SendPacket(Stream stream, byte[] data)
    {
        using var packet = new MemoryStream();
        Var(packet, data.Length);
        packet.Write(data);
        foreach (var b in packet.ToArray())
            await stream.WriteAsync(new[] { b });
    }

    sealed class BytesHandler(byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
    }

    sealed class SearchHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken token)
        {
            if (r.RequestUri!.Host == "cdn.modrinth.com")
            {
                await Task.Delay(300, token);
                return new(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[65536])
                };
            }

            await Task.Delay(20, token);
            var body = r.RequestUri!.AbsolutePath.EndsWith("search") ? "{\"hits\":[{\"project_id\":\"p\",\"slug\":\"fixture\",\"title\":\"Fixture Mod\",\"description\":\"Fixture result\",\"author\":\"test\",\"downloads\":10}],\"total_hits\":1}" : "[{\"id\":\"v\",\"project_id\":\"p\",\"name\":\"Fixture version\",\"files\":[],\"game_versions\":[\"1.21.1\"],\"loaders\":[\"fabric\"]}]";
            return new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}
