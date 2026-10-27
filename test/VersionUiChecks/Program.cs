using BlockLaunch;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--window-behavior-fixture"))
        {
            Thread.Sleep(500);
            return;
        }

        foreach (var original in new[] {
            new LaunchSettings(),
            new LaunchSettings
            {
                Isolation = "共享根目录",
                WindowTitle = "实例窗口",
                VersionInfo = "自定义",
                Visibility = "启动后最小化",
                Priority = "AboveNormal",
                FullScreen = true,
                Width = 1920,
                Height = 1080,
                AutoMemory = false,
                RamMb = 6144,
                JvmArguments = "-Dsample=\"two words\"",
                Server = "localhost:25565",
                TrimLauncher = true
            }
        }

        )
        {
            var form = new LaunchOptionsForm(original);
            var result = form.Read();
            result.Validate();
            if (JsonSerializer.Serialize(original) != JsonSerializer.Serialize(result))
                throw new Exception("Grouped settings changed values");
            Console.WriteLine("PASS grouped launch options preserve all settings: " + original.Isolation);
        }

        var method = typeof(MainWindow).GetMethod("VersionMenuStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var style = (Style)method.Invoke(null, null)!;
        var menu = new RadioButton
        {
            Style = style,
            Content = "概览",
            IsChecked = true
        };
        menu.Measure(new Size(150, 60));
        menu.Arrange(new Rect(0, 0, 150, 60));
        menu.ApplyTemplate();
        if (menu.Template == null || menu.ActualWidth != 150)
            throw new Exception("Version menu template failed");
        Console.WriteLine("PASS selected version navigation template parses and measures");
        var fixture = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BlockLaunch-defaults-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(fixture);
        var wrapped = System.IO.Path.Combine(fixture, "外层游戏包");
        var root = System.IO.Path.Combine(wrapped, ".minecraft");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root, "versions"));
        if (GameDefaults.ResolveRoot(wrapped, true) != root || GameDefaults.ResolveRoot(System.IO.Path.Combine(root, "versions"), true) != root)
            throw new Exception("Root detection failed");
        try
        {
            GameDefaults.ResolveRoot(System.IO.Path.Combine(fixture, "missing"), true);
            throw new Exception("Missing root accepted");
        }
        catch (System.IO.IOException)
        {
        }

        var modern = System.IO.Path.Combine(fixture, "modern");
        GameDefaults.EnsureChinese(modern, "1.21.1");
        var options = System.IO.Path.Combine(modern, "options.txt");
        if (!System.IO.File.ReadAllLines(options).Contains("lang:zh_cn"))
            throw new Exception("Modern language default failed");
        System.IO.File.WriteAllText(options, "music:0.5\nlang:en_us\n");
        GameDefaults.EnsureChinese(modern, "1.21.1");
        if (!System.IO.File.ReadAllText(options).Contains("lang:en_us"))
            throw new Exception("User choice overwritten");
        GameDefaults.EnsureChinese(modern, "1.21.1", true);
        if (!System.IO.File.ReadAllText(options).Contains("music:0.5") || System.IO.File.ReadAllLines(options).Count(x => x.StartsWith("lang:")) != 1 || !System.IO.File.ReadAllText(options).Contains("lang:zh_cn"))
            throw new Exception("Pack language normalization failed");
        var legacy = System.IO.Path.Combine(fixture, "legacy");
        GameDefaults.EnsureChinese(legacy, "1.10.2");
        if (!System.IO.File.ReadAllLines(System.IO.Path.Combine(legacy, "options.txt")).Contains("lang:zh_CN"))
            throw new Exception("Legacy locale failed");
        Console.WriteLine("PASS wrapper/versions root detection, missing root rejection, new game Chinese, pack overrides, existing choice and other options preserved, legacy locale");

        var skinRoot = System.IO.Path.Combine(fixture, "skin-game");
        var skinDirectory = System.IO.Path.Combine(fixture, "skin-instance");
        var skinFile = System.IO.Path.Combine(fixture, "skin.png");
        var versionDirectory = System.IO.Path.Combine(skinRoot, "versions", "1.21.1");
        System.IO.Directory.CreateDirectory(versionDirectory);
        System.IO.File.WriteAllBytes(skinFile, [1, 2, 3, 4]);
        using (var jar = ZipFile.Open(System.IO.Path.Combine(versionDirectory, "1.21.1.jar"), ZipArchiveMode.Create))
        using (var writer = new StreamWriter(jar.CreateEntry("version.json").Open()))
            writer.Write("{\"pack_version\":{\"resource\":{\"major\":34}}}");
        OfflineSkinPackBuilder.Prepare(skinRoot, skinDirectory, skinFile, "1.21.1", false);
        if (System.IO.Directory.Exists(System.IO.Path.Combine(skinDirectory, "resourcepacks")))
            throw new Exception("Online launch created an offline skin pack");
        OfflineSkinPackBuilder.Prepare(skinRoot, skinDirectory, skinFile, "1.21.1", true);
        var skinPack = System.IO.Path.Combine(skinDirectory, "resourcepacks", "BlockLaunch Offline Skin.zip");
        using (var archive = ZipFile.OpenRead(skinPack))
        {
            using var metadata = JsonDocument.Parse(archive.GetEntry("pack.mcmeta")!.Open());
            if (metadata.RootElement.GetProperty("pack").GetProperty("pack_format").GetInt32() != 34 || archive.Entries.Count != 21)
                throw new Exception("Offline skin pack metadata or texture set is invalid");
            using var texture = archive.GetEntry("assets/minecraft/textures/entity/player/wide/steve.png")!.Open();
            using var buffer = new MemoryStream();
            texture.CopyTo(buffer);
            if (!buffer.ToArray().SequenceEqual(new byte[] { 1, 2, 3, 4 }))
                throw new Exception("Offline skin texture content changed");
        }

        System.IO.File.WriteAllBytes(skinFile, [5, 6, 7, 8]);
        OfflineSkinPackBuilder.Prepare(skinRoot, skinDirectory, skinFile, "1.21.1", true);
        using (var archive = ZipFile.OpenRead(skinPack))
        using (var texture = archive.GetEntry("assets/minecraft/textures/entity/player/slim/alex.png")!.Open())
        using (var buffer = new MemoryStream())
        {
            texture.CopyTo(buffer);
            if (!buffer.ToArray().SequenceEqual(new byte[] { 5, 6, 7, 8 }))
                throw new Exception("Offline skin pack was not refreshed");
        }

        Console.WriteLine("PASS offline skin pack generation, metadata, complete texture set, and replacement");
        var psScript = LaunchScriptBuilder.Build("C:\\Launcher\\MCUG.exe", "D:\\Game's\\Root", "1.21.1", "microsoft", true);
        var cmdScript = LaunchScriptBuilder.Build("C:\\Launcher\\100%\\MCUG.exe", "D:\\Games\\Root", "1.21.1", "mua", false);
        if (!psScript.Contains("'D:\\Game''s\\Root'") || !psScript.Contains("--account microsoft") || psScript.Contains("access_token") ||
            !cmdScript.Contains("\"C:\\Launcher\\100%%\\MCUG.exe\"") || !cmdScript.Contains("--account mua") || !cmdScript.StartsWith("@echo off\r\n"))
            throw new Exception("Launch script quoting or account arguments are invalid");
        Console.WriteLine("PASS PowerShell and command-prompt script quoting and account selection");

        var application = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        var behaviorWindow = new Window { Width = 120, Height = 80 };
        behaviorWindow.Show();
        var childStart = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            childStart.ArgumentList.Add(typeof(Program).Assembly.Location);
        childStart.ArgumentList.Add("--window-behavior-fixture");
        using var childProcess = new Process { StartInfo = childStart };
        var session = new GameSession(childProcess, Path.Combine(fixture, "window-behavior.log"), _ => { }, []);
        session.Start();
        var behavior = new GameWindowBehaviorController(behaviorWindow, _ => { });
        var completion = behavior.Apply(childProcess, new LaunchSettings
        {
            Priority = "Normal",
            Visibility = "启动后隐藏"
        }, session);
        if (behaviorWindow.Visibility != Visibility.Hidden)
            throw new Exception("Hidden launcher setting did not hide the window");
        var frame = new DispatcherFrame();
        completion.ContinueWith(_ => behaviorWindow.Dispatcher.BeginInvoke(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        completion.GetAwaiter().GetResult();
        if (!behaviorWindow.IsVisible || behaviorWindow.WindowState != WindowState.Normal)
            throw new Exception("Hidden launcher was not restored after game exit");
        behaviorWindow.Close();
        application.Shutdown();
        Console.WriteLine("PASS hidden launcher is restored after the game process exits");
        Console.WriteLine("ALL_VERSION_UI_COMPONENT_CHECKS_PASSED");
    }
}
