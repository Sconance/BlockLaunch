using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;

public partial class MainWindow
{
    internal void OpenInstanceLibrary(string? key = null) => new InstanceLibraryDialog(this, key).Show();

    async void HandleFileDrop(object sender, DragEventArgs e)
    {
        if (dropFeedback != null)
            dropFeedback.Visibility = Visibility.Collapsed;
        if (busy || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;
        e.Handled = true;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        await ImportDroppedFiles(files);
    }

    async Task ImportDroppedFiles(string[] files)
    {
        if (busy || files.Length == 0)
            return;
        // Pack confirmation must happen before a busy operation is started.
        try
        {
            if (files.Length == 1 && await Task.Run(() => LocalImportClassifier.Identify(files[0])) == LocalImportKind.Modpack)
            {
                await QueuePackImport(files[0]);
                return;
            }
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "无法导入");
            return;
        }

        string targetDirectory;
        try
        {
            targetDirectory = DropTargetDirectory();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "请选择目标实例");
            return;
        }

        await Run(async () =>
        {
            RequireStopped();
            var planned = await Task.Run(() => files.Select(file => (File: file, Kind: LocalImportClassifier.Identify(file))).ToList());
            if (planned.Any(p => p.Kind == LocalImportKind.Unknown))
                throw new IOException("有无法识别的文件。支持模组 JAR、Modrinth/MultiMC 整合包 ZIP、存档 ZIP、资源包、光影和数据包 ZIP。");
            if (planned.Any(p => p.Kind == LocalImportKind.Modpack))
            {
                if (planned.Count != 1)
                    throw new IOException("整合包请单独拖入，其他资源请分开导入。");
                await QueuePackImport(planned[0].File);
                return;
            }

            var destination = targetDirectory;
            string? dataWorld = null;
            if (planned.Any(p => p.Kind == LocalImportKind.DataPack))
            {
                var saves = Path.Combine(destination, "saves");
                Directory.CreateDirectory(saves);
                var picker = new OpenFolderDialog
                {
                    Title = "为拖入的数据包选择当前实例的存档",
                    InitialDirectory = saves
                };
                if (picker.ShowDialog() != true)
                    return;
                dataWorld = Path.GetFullPath(picker.FolderName);
                if (!dataWorld.StartsWith(Path.GetFullPath(saves).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(Path.Combine(dataWorld, "level.dat")))
                    throw new IOException("请选择当前实例 saves 内含 level.dat 的存档。");
            }

            foreach (var group in planned.GroupBy(p => p.Kind))
            {
                if (group.Key == LocalImportKind.World)
                {
                    foreach (var world in group)
                    {
                        var imported = await Task.Run(() => WorldArchives.Import(destination, world.File, progress: text => operations.Current?.Report(text), token: CurrentToken));
                        Log("拖入存档已导入：" + imported);
                    }

                    continue;
                }

                var child = group.Key switch
                {
                    LocalImportKind.Mod => "mods",
                    LocalImportKind.ResourcePack => "resourcepacks",
                    LocalImportKind.ShaderPack => "shaderpacks",
                    LocalImportKind.DataPack => "datapacks",
                    _ => throw new IOException("不支持的文件类型。")
                };
                var count = await new ContentService(http, CurrentToken).ImportFiles(group.Key == LocalImportKind.DataPack ? dataWorld! : destination, child, group.Select(p => p.File));
                Log($"拖拽导入 {child}：新增 {count} 项，同名项目保留原文件。");
            }

            RefreshLocalContent();
            refreshSketchManager?.Invoke();
        }, "拖拽导入文件");
    }
}
