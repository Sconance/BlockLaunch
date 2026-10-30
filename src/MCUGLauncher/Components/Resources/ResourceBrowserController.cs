using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class ResourceBrowserController
{
    readonly LauncherWorkspace workspace;
    internal ResourceBrowserController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal string? managedWorld;
    internal string ManagedDirectory() => managedWorld ?? workspace.LaunchCoordinator.Instance();
    internal int searchOffset, searchTotal;
    internal CancellationTokenSource? browseCancellation;
    internal int browseGeneration;
    internal async Task RunBrowse(Func<Task> action)
    {
        browseCancellation?.Cancel();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        browseCancellation = cts;
        var generation = ++browseGeneration;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (generation == browseGeneration)
            {
                workspace.ResourceWorkspaceController.ShowResourceResults();
                workspace.Controls.SearchHint.Text = "读取失败，可重新搜索：" + ex.Message;
                workspace.Controls.SearchHint.Visibility = Visibility.Visible;
                workspace.LaunchCoordinator.Log("资源浏览失败：" + ex.Message);
            }
        }
        finally
        {
            if (object.ReferenceEquals(browseCancellation, cts))
                browseCancellation = null;
            cts.Dispose();
        }
    }

    internal ContentService Modrinth => new(workspace.LaunchCoordinator.http, workspace.LaunchCoordinator.CurrentToken);

    internal void Navigate(object sender, RoutedEventArgs e)
    {
        if (workspace.Window.MainTabs == null || sender is not RadioButton button)
            return;
        var index = int.Parse(button.Tag.ToString()!);
        if (workspace.CompactShellController.compactShellReady && index == 2)
        {
            workspace.CompactShellController.NavigateDownloads(workspace.ModpackController.ContentKind switch
            {
                "resourcepack" => "资源包",
                "modpack" => "整合包",
                "shader" => "光影包",
                "datapack" => "数据包",
                _ => "Mod"
            });
            return;
        }

        if (workspace.CompactShellController.compactShellReady && index == 3)
        {
            try
            {
                workspace.VersionSettingsController.OpenVersionSettings(workspace.LaunchCoordinator.Selected());
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }

            return;
        }

        if (index != 1)
            workspace.DownloadPageController.RestoreResourcePage();
        else if (workspace.DownloadPageController.downloadCategory is not ("原版游戏" or "本地整合包" or "模组加载器"))
            workspace.DownloadPageController.SelectDownloadCategory(workspace.DownloadPageController.downloadCategory);
        workspace.Window.MainTabs.SelectedIndex = index;
        if (index == 1 && workspace.Window.IsLoaded && workspace.DownloadPageController.downloadCategory is "原版游戏" or "模组加载器")
            _ = workspace.VersionCatalogController.EnsureVersionCatalog();
        workspace.Controls.PageTitle.Text = new[]
        {
            "启动",
            "下载",
            "内容中心",
            "内容管理",
            "启动设置",
            "运行日志",
            "任务与恢复"
        }[index];
        workspace.Controls.PageSubtitle.Text = new[]
        {
            "欢迎回来，今天也去创造一点不同。",
            "准备好游戏，让每次出发都更轻松。",
            "为你的世界，加入一点新鲜感。",
            "当前实例的模组与资源包，一目了然。",
            "找到适合你的游戏节奏。",
            "每次启动，都有迹可循。",
            "查看进行中的操作和上次中断的任务。"
        }[index];
        if (index == 3)
            RefreshLocalContent();
        if (index == 6)
            workspace.TaskRecoveryController.RefreshOperations();
    }

    internal void GoDiscover(object s, RoutedEventArgs e) => workspace.Controls.DiscoverNav.IsChecked = true;
    internal void GoManage(object s, RoutedEventArgs e) => workspace.Controls.ManageNav.IsChecked = true;
    internal InstanceTarget Target()
    {
        var selected = workspace.LaunchCoordinator.Selected();
        if (!File.Exists(Path.Combine(workspace.LaunchCoordinator.prefs.Root, "versions", selected, selected + ".json")) && workspace.LaunchCoordinator.rows.Any(r => r.Id == selected && (r.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || r.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            return new(selected, selected, "vanilla");
        var descriptor = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).GetOrCreate(selected);
        var info = workspace.InstanceWorkspaceController.RequirementsFor(descriptor.LaunchProfileId);
        return new(selected, info.Minecraft, info.Loader);
    }

    internal void VersionChanged(object s, SelectionChangedEventArgs e)
    {
        managedWorld = null;
        if (workspace.Controls.ContentTarget == null)
            return;
        ResetSearch();
        UpdateTargets();
    }

    internal void UpdateTargets()
    {
        workspace.CompactShellController.UpdateHomeLaunchLabel();
        workspace.Controls.LocalCount.Text = "本地版本  " + workspace.LaunchCoordinator.LocalVersions().Count;
        workspace.Controls.InstallTarget.Text = workspace.LaunchCoordinator.downloadVersion ?? (workspace.Controls.Versions.SelectedItem is VersionRow selected ? "修复：" + selected.Label : "请先选择要下载的游戏版本");
        try
        {
            var target = Target();
            workspace.Controls.ContentTarget.Text = $"安装目标：{target.Version}   /   Minecraft {target.GameVersion} · {target.Loader}";
            workspace.Controls.ManageTarget.Text = "当前实例：" + target.Version;
        }
        catch
        {
            workspace.Controls.ContentTarget.Text = "请在首页选择一个可识别的游戏版本";
            workspace.Controls.ManageTarget.Text = "请先选择游戏版本";
        }

        workspace.Controls.ContentTarget.Text = "下载目录：" + workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory;
        workspace.ResourceWorkspaceController.downloadDestination?.SetDirectory(workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory);
        RefreshLocalContent();
    }

    internal void ContentTypeChanged(object s, SelectionChangedEventArgs e)
    {
        if (workspace.Controls.SearchResults == null)
            return;
        ResetSearch();
        UpdateTargets();
        workspace.ResourceFiltersController.UpdateResourceFilterVisibility();
        workspace.Controls.InstallContentButton.Content = "↓  下载所选文件";
    }

    internal void ResetSearch()
    {
        browseCancellation?.Cancel();
        browseCancellation = null;
        browseGeneration++;
        workspace.Controls.SearchResults.ItemsSource = null;
        workspace.Controls.ProjectVersions.ItemsSource = null;
        workspace.Controls.InstallContentButton.IsEnabled = false;
        workspace.Controls.ProjectTitle.Text = "选择一个项目";
        workspace.Controls.ProjectDescription.Text = "选择左侧项目，查看与当前游戏匹配的版本。";
        workspace.Controls.SearchHint.Text = "搜一搜，发现新的玩法。";
        workspace.Controls.SearchHint.Visibility = Visibility.Visible;
        workspace.Controls.SearchSummary.Text = "内容来自 Modrinth";
        searchOffset = 0;
        searchTotal = 0;
    }

    internal async Task SearchContent()
    {
        workspace.ResourceWorkspaceController.ShowResourceResults();
        var token = browseCancellation?.Token ?? CancellationToken.None;
        var target = workspace.ResourceFiltersController.SearchTarget();
        workspace.Window.Status.Text = "搜索 Modrinth…";
        if (workspace.ResourceFiltersController.resourceSource.SelectedIndex == 1)
            throw new NotSupportedException("CurseForge 未启用：当前没有 API Key。请使用 Modrinth。");
        var page = workspace.ResourceFavoritesController.resourceFavoritesOnly ? await workspace.ResourceFavoritesController.SearchFavoriteResources(token) : await new ContentService(workspace.LaunchCoordinator.http, token).Search(workspace.Controls.SearchQuery.Text.Trim(), target.GameVersion, target.Loader, workspace.ModpackController.ContentKind, searchOffset, workspace.ResourceFavoritesController.resourceSort.SelectedIndex switch
        {
            1 => "downloads",
            2 => "updated",
            _ => "relevance"
        });
        token.ThrowIfCancellationRequested();
        workspace.Controls.SearchResults.ItemsSource = workspace.BookmarkLibraryController.DecorateProjects(page.Items);
        searchTotal = page.Total;
        workspace.Controls.ProjectVersions.ItemsSource = null;
        workspace.Controls.InstallContentButton.IsEnabled = false;
        workspace.Controls.ProjectTitle.Text = "选择一个项目";
        workspace.Controls.ProjectDescription.Text = "点击项目，选择需要下载的版本。";
        workspace.Controls.SearchSummary.Text = page.Total == 0 ? "没有匹配的项目" : $"{searchOffset + 1}–{searchOffset + page.Items.Count} / {page.Total:N0} 个项目 · Modrinth · 本页收藏优先";
        if (workspace.ResourceFavoritesController.resourceFavoritesOnly)
            workspace.Controls.SearchSummary.Text = $"{page.Items.Count} / {page.Total:N0} 个匹配收藏";
        workspace.Controls.SearchHint.Text = workspace.ResourceFavoritesController.resourceFavoritesOnly ? "没有符合条件的收藏，可清除收藏筛选或调整关键词。" : "没有找到匹配内容，试试其他关键词。";
        workspace.Controls.SearchHint.Visibility = page.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal async void SearchClick(object s, RoutedEventArgs e)
    {
        searchOffset = 0;
        await RunBrowse(SearchContent);
    }

    internal async void SearchKeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            searchOffset = 0;
            await RunBrowse(SearchContent);
        }
    }

    internal async void PreviousSearch(object s, RoutedEventArgs e)
    {
        if (searchOffset == 0)
            return;
        searchOffset = Math.Max(0, searchOffset - 15);
        await RunBrowse(SearchContent);
    }

    internal async void NextSearch(object s, RoutedEventArgs e)
    {
        if (searchOffset + 15 >= searchTotal)
            return;
        searchOffset += 15;
        await RunBrowse(SearchContent);
    }

    internal async void ProjectChanged(object s, SelectionChangedEventArgs e)
    {
        if (workspace.Controls.SearchResults.SelectedItem is not ContentProject project)
            return;
        workspace.ResourceWorkspaceController.ShowResourceDetail(project);
        await RunBrowse(() => LoadResourceVersions(project));
    }

    internal void ReloadResourceDetail()
    {
        if (workspace.ResourceFavoritesController.resourceDetailProject is { } project)
            _ = RunBrowse(() => LoadResourceVersions(project));
    }

    internal InstanceTarget ResourceDownloadTarget()
    {
        var destination = workspace.ResourceWorkspaceController.downloadDestination;
        if (destination?.InstallToInstance != true)
            return workspace.ResourceFiltersController.SearchTarget();
        var key = destination.SelectedInstanceKey ?? throw new IOException("请选择安装目标实例。");
        var descriptor = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).GetOrCreate(key);
        var info = workspace.InstanceWorkspaceController.RequirementsFor(descriptor.LaunchProfileId);
        return new(key, info.Minecraft, info.Loader);
    }

    async Task LoadResourceVersions(ContentProject project)
    {
        var token = browseCancellation?.Token ?? CancellationToken.None;
        var target = ResourceDownloadTarget();
        workspace.Controls.ProjectTitle.Text = project.Title;
        workspace.Controls.ProjectDescription.Text = project.Description;
        workspace.Controls.ProjectVersions.ItemsSource = null;
        workspace.Controls.InstallContentButton.IsEnabled = false;
        var type = workspace.ModpackController.ContentKind;
        workspace.Window.Status.Text = "读取兼容版本…";
        var list = await new ContentService(workspace.LaunchCoordinator.http, token).Versions(project.Id, target.GameVersion, target.Loader, type);
        token.ThrowIfCancellationRequested();
        if (workspace.ResourceFiltersController.releaseChannel.SelectedIndex > 0)
            list = list.Where(v => v.VersionType == new[] { "", "release", "beta", "alpha" }[workspace.ResourceFiltersController.releaseChannel.SelectedIndex]).ToList();
        workspace.Controls.ProjectVersions.ItemsSource = list;
        workspace.Controls.ProjectVersions.SelectedIndex = list.Count > 0 ? 0 : -1;
        workspace.Controls.InstallContentButton.IsEnabled = list.Count > 0;
        if (list.Count == 0)
            workspace.Controls.ProjectDescription.Text += "\n\n没有符合筛选条件的可下载版本。";
        await workspace.ResourcePresentationController.LoadResourcePresentation(project, token);
        workspace.Controls.InstallContentButton.Content = workspace.ResourceWorkspaceController.downloadDestination?.InstallToInstance == true
            ? "↓ 安装到所选实例" : "↓ 下载所选文件";
    }

    internal void RequireStopped()
    {
        if (workspace.RunningGamesController.HasRunningGames)
            throw new IOException("请先关闭游戏，再修改实例内容。");
    }

    internal async Task InstallVersion(ContentVersion version)
    {
        RequireStopped();
        var target = Target();
        if (!workspace.LaunchCoordinator.LocalVersions().Contains(target.Version))
            throw new IOException("请先安装并选择本地游戏版本。");
        workspace.Window.Status.Text = "解析必需依赖…";
        var plan = await Modrinth.Resolve(version, target.GameVersion, workspace.ModpackController.ContentKind == "datapack" ? "datapack" : target.Loader);
        var destination = workspace.LaunchCoordinator.Instance();
        if (plan.Types.Values.Contains("datapack"))
        {
            var picker = new OpenFolderDialog
            {
                Title = "选择当前实例 saves 内的目标存档",
                InitialDirectory = Path.Combine(destination, "saves")
            };
            if (picker.ShowDialog() != true)
                return;
            var world = Path.GetFullPath(picker.FolderName);
            var saves = Path.GetFullPath(Path.Combine(destination, "saves")) + Path.DirectorySeparatorChar;
            if (!world.StartsWith(saves, StringComparison.OrdinalIgnoreCase) || !File.Exists(Path.Combine(world, "level.dat")))
                throw new IOException("请选择当前实例的有效存档目录（含 level.dat）。");
            destination = world;
            managedWorld = world;
        }

        var count = await Modrinth.Install(plan, destination, text => workspace.LaunchCoordinator.operations.Current?.Report(text));
        workspace.LaunchCoordinator.Log($"安装完成：{version.Name}，共 {count} 项（含必需依赖）。更新前的文件保留在实例备份目录。");
        RefreshLocalContent();
        InlineMessageBox.Show($"已安装 {count} 项内容（含必需依赖）。\n手动导入的模组无法自动识别重复项目，请在内容管理中检查。", "安装完成");
    }

    internal void InstallContentClick(object s, RoutedEventArgs e)
    {
        if (workspace.Controls.ProjectVersions.SelectedItem is ContentVersion version)
            workspace.ResourceInstallationController.Install(version);
    }

    internal void ProjectPageClick(object s, RoutedEventArgs e)
    {
        if ((workspace.Controls.SearchResults.SelectedItem as ContentProject ?? workspace.ResourceFavoritesController.resourceDetailProject) is ContentProject project)
            Process.Start(new ProcessStartInfo("https://modrinth.com/project/" + Uri.EscapeDataString(project.Id)) { UseShellExecute = true });
    }

    internal void RefreshLocalContent()
    {
        if (workspace.Controls.InstalledContent == null)
            return;
        try
        {
            workspace.BookmarkLibraryController.localSnapshot = ContentService.ListLocal(ManagedDirectory());
            workspace.BookmarkLibraryController.ApplyLocalSearch();
            if (managedWorld != null)
                workspace.Controls.ManageTarget.Text = "数据包目标存档：" + managedWorld;
        }
        catch (Exception ex)
        {
            workspace.BookmarkLibraryController.localSnapshot = [];
            workspace.Controls.InstalledContent.ItemsSource = null;
            workspace.BookmarkLibraryController.localSummary.Text = "";
            if (workspace.Controls.Versions.SelectedValue != null)
                workspace.LaunchCoordinator.Log("内容列表读取失败：" + ex.Message);
        }
    }

    internal void RefreshContentClick(object s, RoutedEventArgs e) => RefreshLocalContent();
    internal async void ToggleContentClick(object s, RoutedEventArgs e) => await workspace.LaunchCoordinator.Run(() =>
    {
        RequireStopped();
        if (workspace.Controls.InstalledContent.SelectedItem is LocalContent item)
        {
            ContentService.Toggle(item);
            workspace.LaunchCoordinator.Log("切换内容状态：" + item.Name);
            RefreshLocalContent();
        }

        return Task.CompletedTask;
    });
    internal async void RemoveContentClick(object s, RoutedEventArgs e) => await workspace.LaunchCoordinator.Run(() =>
    {
        RequireStopped();
        if (workspace.Controls.InstalledContent.SelectedItem is LocalContent item)
        {
            ContentService.MoveToBackup(ManagedDirectory(), item);
            workspace.LaunchCoordinator.Log("已移至备份：" + item.Name + "。其他模组可能依赖此项目，请检查依赖。");
            RefreshLocalContent();
        }

        return Task.CompletedTask;
    });
    internal async void UpdateContentClick(object s, RoutedEventArgs e)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        try
        {
            if (workspace.Controls.InstalledContent.SelectedItem is not LocalContent { Managed: { } managed })
                throw new IOException("请选择通过 Modrinth 安装的内容。");
            var target = Target();
            await workspace.ContentUpdatesController.OpenContentUpdates(ManagedDirectory(), target.Version, target.GameVersion, target.Loader, managed.ProjectId);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "内容更新");
        }
    }

    internal void TrashFolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            workspace.LaunchCoordinator.OpenFolder(Path.Combine(ManagedDirectory(), ".blocklaunch", "backups"));
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    internal async void QuiltClick(object s, RoutedEventArgs e) => await workspace.LaunchCoordinator.Run(async () =>
    {
        var target = Target();
        if (target.Loader != "vanilla")
            throw new IOException("请先选择一个官方原版版本。");
        using var list = JsonDocument.Parse(await workspace.LaunchCoordinator.http.GetStringAsync("https://meta.quiltmc.org/v3/versions/loader/" + Uri.EscapeDataString(target.GameVersion), workspace.LaunchCoordinator.CurrentToken));
        var loader = list.RootElement.EnumerateArray().Where(e => System.Text.RegularExpressions.Regex.IsMatch(e.GetProperty("loader").GetProperty("version").GetString() ?? "", @"^\d+\.\d+\.\d+$")).OrderByDescending(e => Version.Parse(e.GetProperty("loader").GetProperty("version").GetString()!)).FirstOrDefault();
        if (loader.ValueKind == JsonValueKind.Undefined)
            throw new IOException("此游戏版本没有稳定 Quilt 加载器。");
        var version = loader.GetProperty("loader").GetProperty("version").GetString()!;
        workspace.DownloadQueueController.EnqueueDownload("Quilt · " + target.GameVersion, target.GameVersion, new()
        {
            Version = target.GameVersion,
            Loader = "quilt",
            LoaderVersion = version
        });
    });
    internal async void LoaderJarClick(object s, RoutedEventArgs e) => await workspace.LaunchCoordinator.Run(async () =>
    {
        RequireStopped();
        var dialog = new OpenFileDialog
        {
            Filter = "官方 Forge / NeoForge 安装器|*.jar"
        };
        if (dialog.ShowDialog() != true)
            return;
        if (!Path.GetFileName(dialog.FileName).Contains("installer", StringComparison.OrdinalIgnoreCase))
            throw new IOException("请选择官方网站提供的 installer.jar 安装器。");
        var java = workspace.LaunchCoordinator.prefs.Java.Length > 0 ? workspace.LaunchCoordinator.prefs.Java : "java";
        // A profile marker allows the official installer to recognize an existing launcher directory.
        var marker = Path.Combine(workspace.LaunchCoordinator.prefs.Root, "launcher_profiles.json");
        if (!File.Exists(marker))
            await File.WriteAllTextAsync(marker, "{\"profiles\":{}}");
        var start = new ProcessStartInfo(java)
        {
            UseShellExecute = false,
            WorkingDirectory = workspace.LaunchCoordinator.prefs.Root,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-jar");
        start.ArgumentList.Add(dialog.FileName);
        start.ArgumentList.Add("--installClient");
        start.ArgumentList.Add(workspace.LaunchCoordinator.prefs.Root);
        using var installer = Process.Start(start) ?? throw new IOException("安装器无法启动。");
        async Task Drain(StreamReader reader)
        {
            var last = DateTime.MinValue;
            while (await reader.ReadLineAsync() is { } line)
                if ((DateTime.UtcNow - last).TotalSeconds >= 1)
                {
                    workspace.LaunchCoordinator.Log("安装器：" + line[..Math.Min(line.Length, 220)]);
                    last = DateTime.UtcNow;
                }
        }

        var output = Drain(installer.StandardOutput);
        var error = Drain(installer.StandardError);
        workspace.Window.Status.Text = "正在后台运行官方安装器…";
        try
        {
            await installer.WaitForExitAsync(workspace.LaunchCoordinator.CurrentToken);
        }
        catch (OperationCanceledException)
        {
            installer.Kill(true);
            await installer.WaitForExitAsync();
            throw;
        }
        finally
        {
            await Task.WhenAll(output, error);
        }

        if (installer.ExitCode != 0)
            throw new IOException("官方安装器返回失败，请查看运行日志。");
        await workspace.LaunchCoordinator.Refresh();
        workspace.LaunchCoordinator.Log("官方安装器已完成，请在首页选择新增版本。");
    });
}
