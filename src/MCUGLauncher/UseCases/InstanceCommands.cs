using MCUGLauncher.Models;
using BlockLaunch;
using System.Diagnostics;
using MCUGLauncher.Services;
using static MCUGLauncher.UseCases.ProtocolValues;

namespace MCUGLauncher.UseCases;

// Handles instance use cases; no WPF controls or browser access.
internal sealed class InstanceCommands(LauncherContext context)
{
    public void OpenFolder(string kind)
    {
        var catalog = VersionCatalog.Load(context.Config.Root, context.Config.LastInstance);
        var instance = VersionCatalog.InstanceDirectory(context.Config.Root, catalog.Selected);
        var path = kind switch
        {
            "mods" => Path.Combine(instance, "mods"),
            "saves" => Path.Combine(instance, "saves"),
            "screenshots" => Path.Combine(instance, "screenshots"),
            "version" => instance,
            _ => context.Config.Root
        };
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    public void ToggleMod(string file, bool enabled)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.SetEnabled(mod, enabled);
        context.RefreshState();
    }

    public void DeleteMod(string file)
    {
        var mod = FindMod(file);
        if (mod == null)
            return;
        VersionCatalog.Delete(mod);
        context.RefreshState();
    }

    public void DeleteVersion(string version)
    {
        var source = InstancePath(context.Config.Root, version);
        if (!Directory.Exists(source))
        {
            context.Send(new
            {
                type = "toast",
                message = "版本目录不存在。"
            });
            return;
        }
        try
        {
            var trash = Path.Combine(context.Store.DataDirectory, "trash");
            Directory.CreateDirectory(trash);
            var target = Path.Combine(trash, $"{version}-{DateTime.Now:yyyyMMdd-HHmmss}");
            Directory.Move(source, target);
            context.Log($"Version moved to trash: {version} -> {target}");
            if (context.Config.LastVersion.Equals(version, StringComparison.OrdinalIgnoreCase))
                context.Config.LastVersion = "";
            context.Store.Save(context.Config);
            context.Send(new
            {
                type = "toast",
                message = $"版本已移到回收目录：{target}"
            });
            context.RefreshState();
        }
        catch (Exception ex)
        {
            context.Send(new
            {
                type = "toast",
                message = "删除版本失败：" + ex.Message
            });
        }
    }

    public ModInfo? FindMod(string file)
    {
        var catalog = VersionCatalog.Load(context.Config.Root, context.Config.LastInstance);
        return VersionCatalog.ReadMods(context.Config.Root, catalog.Selected)
            .FirstOrDefault(item => item.File.Equals(file, StringComparison.OrdinalIgnoreCase));
    }
}
