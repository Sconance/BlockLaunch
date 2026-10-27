using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace BlockLaunch;

public partial class MainWindow
{
    internal void OpenFolder(string dir)
    {
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }

    async Task Import(string child, string filter)
    {
        if (busy)
            return;
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = true
        };
        if (dialog.ShowDialog() != true)
            return;
        await Run(async () =>
        {
            RequireStopped();
            if (!LocalVersions().Contains(Selected()))
                throw new IOException("请先完成游戏安装。");
            if (child == "mods" && Target().Loader == "vanilla")
                throw new IOException("请先选择已安装加载器的实例。");
            var count = await new ContentService(http, CurrentToken).ImportFiles(Instance(), child, dialog.FileNames);
            Log($"已导入 {count} 个文件；同名文件已跳过。");
            RefreshLocalContent();
        }, "导入本地内容");
    }

    async void ImportModClick(object s, RoutedEventArgs e) => await Import("mods", "Java 模组|*.jar");

    async void ImportResourceClick(object s, RoutedEventArgs e) => await Import("resourcepacks", "资源包|*.zip");

    void FolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            OpenFolder(Instance());
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    void ModsFolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            OpenFolder(Path.Combine(Instance(), "mods"));
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }
}
