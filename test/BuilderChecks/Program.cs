using System.IO.Compression;
using System.Text.Json;
using BlockLaunch;

var powershell = LaunchScriptBuilder.Build("C:\\Launcher\\MCUG.exe", "D:\\Game's\\Root", "1.21.1", "microsoft", true);
var command = LaunchScriptBuilder.Build("C:\\Launcher\\100%\\MCUG.exe", "D:\\Games\\Root", "1.21.1", "mua", false);
Require(powershell.Contains("'D:\\Game''s\\Root'") && powershell.Contains("--account microsoft"), "PowerShell quoting and account");
Require(command.Contains("\"C:\\Launcher\\100%%\\MCUG.exe\"") && command.Contains("--account mua") && command.StartsWith("@echo off\r\n"), "CMD quoting and account");
Require(!powershell.Contains("access_token") && !command.Contains("access_token"), "Scripts must not contain tokens");

var fixture = Path.Combine(Path.GetTempPath(), "MCUG-BuilderChecks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixture);
try
{
    var root = Path.Combine(fixture, "game");
    var instance = Path.Combine(fixture, "instance");
    var skin = Path.Combine(fixture, "skin.png");
    var versionDirectory = Path.Combine(root, "versions", "fixture");
    Directory.CreateDirectory(versionDirectory);
    File.WriteAllBytes(skin, [1, 2, 3, 4]);
    var jarPath = Path.Combine(versionDirectory, "fixture.jar");
    using (var jar = ZipFile.Open(jarPath, ZipArchiveMode.Create))
    using (var writer = new StreamWriter(jar.CreateEntry("version.json").Open()))
        writer.Write("{\"pack_version\":{\"resource\":{\"major\":34}}}");

    OfflineSkinPackBuilder.Prepare(root, instance, skin, "fixture", false);
    OfflineSkinPackBuilder.Prepare(root, instance, Path.Combine(fixture, "missing.png"), "fixture", true);
    Require(!Directory.Exists(instance), "Online and missing-skin calls must not create files");

    OfflineSkinPackBuilder.Prepare(root, instance, skin, "fixture", true);
    var pack = Path.Combine(instance, "resourcepacks", "BlockLaunch Offline Skin.zip");
    CheckPack(pack, 34, [1, 2, 3, 4]);
    File.WriteAllBytes(skin, [5, 6, 7, 8]);
    OfflineSkinPackBuilder.Prepare(root, instance, skin, "fixture", true);
    CheckPack(pack, 34, [5, 6, 7, 8]);

    File.Delete(jarPath);
    using (var jar = ZipFile.Open(jarPath, ZipArchiveMode.Create))
    using (var writer = new StreamWriter(jar.CreateEntry("version.json").Open()))
        writer.Write("{\"pack_version\":{\"resource\":22}}");
    OfflineSkinPackBuilder.Prepare(root, instance, skin, "fixture", true);
    CheckPack(pack, 22, [5, 6, 7, 8]);
    OfflineSkinPackBuilder.Prepare(root, instance, skin, "missing-version", true);
    CheckPack(pack, 15, [5, 6, 7, 8]);
}
finally
{
    Directory.Delete(fixture, true);
}

Console.WriteLine("ALL_BUILDER_CHECKS_PASSED");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void CheckPack(string path, int format, byte[] expected)
{
    using var archive = ZipFile.OpenRead(path);
    using var metadata = JsonDocument.Parse(archive.GetEntry("pack.mcmeta")!.Open());
    Require(metadata.RootElement.GetProperty("pack").GetProperty("pack_format").GetInt32() == format, "Pack format metadata");
    Require(archive.Entries.Count == 21, "Complete skin texture set");
    foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".png")))
    {
        using var texture = entry.Open();
        using var buffer = new MemoryStream();
        texture.CopyTo(buffer);
        Require(buffer.ToArray().SequenceEqual(expected), "Skin content and replacement: " + entry.FullName);
    }
}
