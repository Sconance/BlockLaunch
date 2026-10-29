using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace BlockLaunch;

public static class OfflineSkinPackBuilder
{
    public static void Prepare(string root, string directory, string skinFile, string baseVersion, bool isOffline)
    {
        if (!isOffline || !File.Exists(skinFile))
            return;
        var packDir = Path.Combine(directory, "resourcepacks");
        Directory.CreateDirectory(packDir);
        var file = Path.Combine(packDir, "BlockLaunch Offline Skin.zip");
        var format = 15;
        var jar = Path.Combine(root, "versions", baseVersion, baseVersion + ".jar");
        if (File.Exists(jar))
        {
            using var gameJar = ZipFile.OpenRead(jar);
            var version = gameJar.GetEntry("version.json");
            if (version != null)
            {
                using var doc = JsonDocument.Parse(version.Open());
                if (doc.RootElement.TryGetProperty("pack_version", out var p) && p.TryGetProperty("resource", out var r))
                {
                    if (r.ValueKind == JsonValueKind.Number)
                        format = r.GetInt32();
                    else if (r.TryGetProperty("major", out var major))
                        format = major.GetInt32();
                }
            }
        }

        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var archive = ZipFile.Open(temp, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("pack.mcmeta").Open()))
                writer.Write(JsonSerializer.Serialize(new
                {
                    pack = new
                    {
                        pack_format = format,
                        description = "BlockLaunch Offline Skin (local default-player replacement)"
                    }
                }));
            var paths = new List<string>
            {
                "assets/minecraft/textures/entity/steve.png",
                "assets/minecraft/textures/entity/alex.png"
            };
            foreach (var model in new[] {
                "wide",
                "slim"
            }

            )
                foreach (var name in new[] {
                    "steve",
                    "alex",
                    "ari",
                    "efe",
                    "kai",
                    "makena",
                    "noor",
                    "sunny",
                    "zuri"
                }

                )
                    paths.Add("assets/minecraft/textures/entity/player/" + model + "/" + name + ".png");
            foreach (var path in paths)
                archive.CreateEntryFromFile(skinFile, path);
        }

        File.Move(temp, file, true);
    }
}
