using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class InstanceLibraryController
{
    readonly LauncherWorkspace workspace;
    internal InstanceLibraryController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal void OpenInstanceLibrary(string? key = null)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        try
        {
            key ??= workspace.LaunchCoordinator.Selected();
            var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
            var descriptor = catalog.GetOrCreate(key);
            var directory = (descriptor.Launch ?? workspace.LaunchCoordinator.prefs.Launch).Isolation == "共享根目录" ? workspace.LaunchCoordinator.prefs.Root : catalog.DirectoryFor(key);
            var body = new StackPanel();
            body.Children.Add(FeatureUiElements.Heading("实例资料库 · " + descriptor.DisplayName));
            body.Children.Add(FeatureUiElements.Hint(directory));
            var tabs = new TabControl();
            body.Children.Add(tabs);
            var worldsPanel = new StackPanel();
            var worlds = new ListBox
            {
                Height = 220,
                DisplayMemberPath = "Name"
            };
            var worldInfo = FeatureUiElements.Hint("选择存档后可备份或打开目录。备份前请退出使用该存档的游戏。");
            worlds.SelectionChanged += (_, _) =>
            {
                if (worlds.SelectedItem is WorldEntry world)
                {
                    try
                    {
                        var metadata = WorldMetadata.Read(Path.Combine(world.Directory, "level.dat"));
                        var target = workspace.InstanceWorkspaceController.RequirementsFor(descriptor.LaunchProfileId).Minecraft;
                        worldInfo.Text = $"{metadata.Name} · 保存版本 {metadata.Version} · DataVersion {metadata.DataVersion}\n{metadata.Warning(target)}\n最近修改：{world.Modified:yyyy-MM-dd HH:mm}";
                    }
                    catch (Exception ex)
                    {
                        worldInfo.Text = "存档版本暂无法读取：" + ex.Message;
                    }
                }
            };
            void RefreshWorlds()
            {
                worlds.ItemsSource = WorldArchives.List(directory);
                worldInfo.Text = worlds.Items.Count == 0 ? "此实例还没有存档。可导入含 level.dat 的存档 ZIP。" : "请选择存档。";
            }

            worldsPanel.Children.Add(worlds);
            worldsPanel.Children.Add(worldInfo);
            var dialog = workspace.InstanceWorkspaceController.FeatureDialog("实例资料库", body);
            dialog.Width = 820;
            var worldActions = new WrapPanel();
            worldsPanel.Children.Add(worldActions);
            worldActions.Children.Add(FeatureUiElements.ActionButton("刷新存档", (_, _) =>
            {
                try
                {
                    RefreshWorlds();
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            }));
            worldActions.Children.Add(FeatureUiElements.ActionButton("打开存档目录", (_, _) =>
            {
                try
                {
                    if (worlds.SelectedItem is WorldEntry world)
                        workspace.LaunchCoordinator.OpenFolder(world.Directory);
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            }));
            worldActions.Children.Add(FeatureUiElements.ActionButton("备份所选存档为 ZIP", async (_, _) =>
            {
                if (worlds.SelectedItem is not WorldEntry world)
                {
                    InlineMessageBox.Show("请先选择一个存档。");
                    return;
                }

                var save = new SaveFileDialog
                {
                    Filter = "存档备份|*.zip",
                    FileName = world.Name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip"
                };
                if (save.ShowDialog() != true)
                    return;
                dialog.Close();
                await workspace.LaunchCoordinator.Run(async () =>
                {
                    workspace.ResourceBrowserController.RequireStopped();
                    await Task.Run(() => WorldArchives.Backup(world.Directory, save.FileName, text => workspace.LaunchCoordinator.operations.Current?.Report(text), workspace.LaunchCoordinator.CurrentToken));
                    workspace.LaunchCoordinator.Log("存档备份完成：" + save.FileName);
                }, "备份存档");
            }));
            worldActions.Children.Add(FeatureUiElements.ActionButton("导入存档 ZIP（新建存档）", async (_, _) =>
            {
                var open = new OpenFileDialog
                {
                    Filter = "存档 ZIP|*.zip"
                };
                if (open.ShowDialog() != true)
                    return;
                dialog.Close();
                await workspace.LaunchCoordinator.Run(async () =>
                {
                    workspace.ResourceBrowserController.RequireStopped();
                    var world = await Task.Run(() => WorldArchives.Import(directory, open.FileName, progress: text => workspace.LaunchCoordinator.operations.Current?.Report(text), token: workspace.LaunchCoordinator.CurrentToken));
                    workspace.LaunchCoordinator.Log("存档导入完成：" + world);
                }, "导入存档");
            }));
            tabs.Items.Add(new TabItem { Header = "存档与备份", Content = worldsPanel });
            var screenshotsPanel = new StackPanel();
            var screenshots = new ListBox
            {
                Height = 130
            };
            var preview = new Image
            {
                Height = 230,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 12, 0, 8)
            };
            var screenshotInfo = FeatureUiElements.Hint("选择截图，在这里预览。");
            screenshots.SelectionChanged += (_, _) =>
            {
                preview.Source = null;
                if (screenshots.SelectedItem is not ScreenshotEntry shot)
                    return;
                try
                {
                    if (shot.Bytes > 50 * 1024 * 1024)
                        throw new IOException("截图超过 50 MB，请在外部查看。");
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 1200;
                    using (var stream = File.OpenRead(shot.File))
                    {
                        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                        var frame = decoder.Frames[0];
                        if (frame.PixelWidth > 32768 || frame.PixelHeight > 32768 || (long)frame.PixelWidth * frame.PixelHeight > 100_000_000)
                            throw new IOException("截图尺寸过大，请在外部查看。");
                        stream.Position = 0;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }

                    bitmap.Freeze();
                    preview.Source = bitmap;
                    screenshotInfo.Text = $"{shot.Name} · {shot.Modified:yyyy-MM-dd HH:mm} · {shot.Bytes / 1024:N0} KB";
                }
                catch (Exception ex)
                {
                    screenshotInfo.Text = "预览失败：" + ex.Message;
                }
            };
            void RefreshScreenshots()
            {
                screenshots.ItemsSource = WorldArchives.Screenshots(directory);
                preview.Source = null;
                screenshotInfo.Text = screenshots.Items.Count == 0 ? "此实例还没有截图。游戏内按 F2 截图后可在此查看。" : "选择截图，在这里预览。";
            }

            screenshotsPanel.Children.Add(screenshots);
            screenshotsPanel.Children.Add(preview);
            screenshotsPanel.Children.Add(screenshotInfo);
            var screenshotActions = new WrapPanel();
            screenshotsPanel.Children.Add(screenshotActions);
            screenshotActions.Children.Add(FeatureUiElements.ActionButton("刷新截图", (_, _) =>
            {
                try
                {
                    RefreshScreenshots();
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            }));
            screenshotActions.Children.Add(FeatureUiElements.ActionButton("打开截图文件夹", (_, _) =>
            {
                try
                {
                    workspace.LaunchCoordinator.OpenFolder(Path.Combine(directory, "screenshots"));
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            }));
            screenshotActions.Children.Add(FeatureUiElements.ActionButton("另存所选截图", (_, _) =>
            {
                if (screenshots.SelectedItem is not ScreenshotEntry shot)
                {
                    InlineMessageBox.Show("请先选择截图。");
                    return;
                }

                var save = new SaveFileDialog
                {
                    Filter = "图片|*" + Path.GetExtension(shot.Name),
                    FileName = shot.Name
                };
                if (save.ShowDialog() == true)
                {
                    try
                    {
                        if (!Path.GetFullPath(save.FileName).Equals(shot.File, StringComparison.OrdinalIgnoreCase))
                            File.Copy(shot.File, save.FileName, true);
                    }
                    catch (Exception ex)
                    {
                        InlineMessageBox.Show(ex.Message);
                    }
                }
            }));
            tabs.Items.Add(new TabItem { Header = "截图预览", Content = screenshotsPanel });
            body.Children.Add(FeatureUiElements.Hint("此窗口始终管理打开时选定的实例。存档导入会新建目录，已有存档不会被覆盖。"));
            RefreshWorlds();
            RefreshScreenshots();
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "实例资料库");
        }
    }

    internal async void HandleFileDrop(object sender, DragEventArgs e)
    {
        if (workspace.DropFeedbackController.dropFeedback != null)
            workspace.DropFeedbackController.dropFeedback.Visibility = Visibility.Collapsed;
        if (workspace.LaunchCoordinator.busy || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;
        e.Handled = true;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        await ImportDroppedFiles(files);
    }

    internal async Task ImportDroppedFiles(string[] files)
    {
        if (workspace.LaunchCoordinator.busy || files.Length == 0)
            return;
        // Pack confirmation must happen before a busy operation is started.
        try
        {
            if (files.Length == 1 && await Task.Run(() => LocalImportClassifier.Identify(files[0])) == LocalImportKind.Modpack)
            {
                await workspace.DownloadQueueController.QueuePackImport(files[0]);
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
            targetDirectory = workspace.DropFeedbackController.DropTargetDirectory();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "请选择目标实例");
            return;
        }

        await workspace.LaunchCoordinator.Run(async () =>
        {
            workspace.ResourceBrowserController.RequireStopped();
            var planned = await Task.Run(() => files.Select(file => (File: file, Kind: LocalImportClassifier.Identify(file))).ToList());
            if (planned.Any(p => p.Kind == LocalImportKind.Unknown))
                throw new IOException("有无法识别的文件。支持模组 JAR、Modrinth/MultiMC 整合包 ZIP、存档 ZIP、资源包、光影和数据包 ZIP。");
            if (planned.Any(p => p.Kind == LocalImportKind.Modpack))
            {
                if (planned.Count != 1)
                    throw new IOException("整合包请单独拖入，其他资源请分开导入。");
                await workspace.DownloadQueueController.QueuePackImport(planned[0].File);
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
                        var imported = await Task.Run(() => WorldArchives.Import(destination, world.File, progress: text => workspace.LaunchCoordinator.operations.Current?.Report(text), token: workspace.LaunchCoordinator.CurrentToken));
                        workspace.LaunchCoordinator.Log("拖入存档已导入：" + imported);
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
                var count = await new ContentService(workspace.LaunchCoordinator.http, workspace.LaunchCoordinator.CurrentToken).ImportFiles(group.Key == LocalImportKind.DataPack ? dataWorld! : destination, child, group.Select(p => p.File));
                workspace.LaunchCoordinator.Log($"拖拽导入 {child}：新增 {count} 项，同名项目保留原文件。");
            }

            workspace.ResourceBrowserController.RefreshLocalContent();
            workspace.DropFeedbackController.refreshSketchManager?.Invoke();
        }, "拖拽导入文件");
    }
}
