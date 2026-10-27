using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace BlockLaunch;

public partial class MainWindow
{
    string? managedWorld;

    internal string ManagedDirectory() => managedWorld ?? Instance();

    int searchOffset, searchTotal;
    CancellationTokenSource? browseCancellation;
    int browseGeneration;

    async Task RunBrowse(Func<Task> action)
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
                ContentSearchHintControl.Text = "读取失败，可重新搜索：" + ex.Message;
                ContentSearchHintControl.Visibility = Visibility.Visible;
                Log("资源浏览失败：" + ex.Message);
            }
        }
        finally
        {
            if (ReferenceEquals(browseCancellation, cts))
                browseCancellation = null;
            cts.Dispose();
        }
    }

    ContentService Modrinth => new(http, CurrentToken);

    void Navigate(object sender, RoutedEventArgs e)
    {
        if (xamlReady && MainTabControl != null)
            pageNavigationController?.Navigate(sender, e);
    }

    void GoDiscover(object s, RoutedEventArgs e) => ContentDiscoverNavigation.IsChecked = true;
    void GoManage(object s, RoutedEventArgs e) => ContentManageNavigation.IsChecked = true;

    internal void OpenModSearch(string key, InlineDialog owner)
    {
        if (!LocalVersions().Contains(key))
        {
            InlineMessageBox.Show(owner, "请先完成版本安装。");
            return;
        }

        VersionSelectorControl.SelectedValue = key;
        ContentKindSelector.SelectedIndex = 0;
        owner.Close();
        ContentDiscoverNavigation.IsChecked = true;
    }

    internal InstanceTarget Target()
    {
        var selected = Selected();
        if (!File.Exists(Path.Combine(prefs.Root, "versions", selected, selected + ".json")) && rows.Any(r => r.Id == selected && (r.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || r.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase))))
            return new(selected, selected, "vanilla");
        var descriptor = new InstanceCatalog(prefs.Root).GetOrCreate(selected);
        var info = RequirementsFor(descriptor.LaunchProfileId);
        return new(selected, info.Minecraft, info.Loader);
    }

    void VersionChanged(object s, SelectionChangedEventArgs e)
    {
        if (!xamlReady)
            return;
        managedWorld = null;
        if (ContentTargetLabel == null)
            return;
        ResetSearch();
        UpdateTargets();
    }

    void UpdateTargets()
    {
        UpdateHomeLaunchLabel();
        LocalVersionCountLabel.Text = "本地版本  " + LocalVersions().Count;
        InstallTargetLabel.Text = downloadVersion ?? (VersionSelectorControl.SelectedItem is VersionRow selected ? "修复：" + selected.Label : "请先选择要下载的游戏版本");
        try
        {
            var target = Target();
            ContentTargetLabel.Text = $"安装目标：{target.Version}   /   Minecraft {target.GameVersion} · {target.Loader}";
            ManageTargetLabel.Text = "当前实例：" + target.Version;
        }
        catch
        {
            ContentTargetLabel.Text = "请在首页选择一个可识别的游戏版本";
            ManageTargetLabel.Text = "请先选择游戏版本";
        }

        ContentTargetLabel.Text = "下载目录：" + prefs.ResourceDownloadDirectory;
        RefreshLocalContent();
    }

    void ContentTypeChanged(object s, SelectionChangedEventArgs e)
    {
        if (!xamlReady)
            return;
        if (ContentSearchResultsControl == null)
            return;
        ResetSearch();
        UpdateTargets();
        ContentInstallButtonControl.Content = "↓  下载所选文件";
    }

    void ResetSearch()
    {
        browseCancellation?.Cancel();
        browseCancellation = null;
        browseGeneration++;
        ContentSearchResultsControl.ItemsSource = null;
        ContentProjectVersionsControl.ItemsSource = null;
        ContentInstallButtonControl.IsEnabled = false;
        ContentProjectTitleLabel.Text = "选择一个项目";
        ContentProjectDescriptionLabel.Text = "选择左侧项目，查看与当前游戏匹配的版本。";
        ContentSearchHintControl.Text = "搜一搜，发现新的玩法。";
        ContentSearchHintControl.Visibility = Visibility.Visible;
        ContentSearchSummaryLabel.Text = "内容来自 Modrinth";
        searchOffset = 0;
        searchTotal = 0;
        ResetResourceDisplayFilters();
        ShowResourceResults();
    }

    async Task SearchContent()
    {
        var token = browseCancellation?.Token ?? CancellationToken.None;
        var target = SearchTarget();
        OperationStatusLabel.Text = "搜索 Modrinth…";
        if (resourceSource.SelectedIndex == 1)
            throw new NotSupportedException("CurseForge 未启用：当前没有 API Key。请使用 Modrinth。");
        ShowResourceResults();
        var service = new ContentService(http, token);
        var page = resourceFavoritesOnly
            ? await SearchFavoriteResources(token)
            : await service.Search(ContentSearchQueryInput.Text.Trim(), target.GameVersion, target.Loader, ContentKind, searchOffset, ResourceSearchSort());
        token.ThrowIfCancellationRequested();
        ContentSearchResultsControl.ItemsSource = DecorateProjects(page.Items);
        searchTotal = page.Total;
        ContentProjectVersionsControl.ItemsSource = null;
        ContentInstallButtonControl.IsEnabled = false;
        ContentProjectTitleLabel.Text = "选择一个项目";
        ContentProjectDescriptionLabel.Text = "点击项目，选择需要下载的版本。";
        ContentSearchSummaryLabel.Text = page.Total == 0 ? "没有匹配的项目" : $"{searchOffset + 1}–{searchOffset + page.Items.Count} / {page.Total:N0} 个项目 · Modrinth" + (resourceFavoritesOnly ? " · 收藏筛选" : "");
        ContentSearchHintControl.Text = "没有找到匹配内容，试试其他关键词。";
        ContentSearchHintControl.Visibility = page.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    async void SearchClick(object s, RoutedEventArgs e)
    {
        searchOffset = 0;
        await RunBrowse(SearchContent);
    }

    async void SearchKeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            searchOffset = 0;
            await RunBrowse(SearchContent);
        }
    }

    async void PreviousSearch(object s, RoutedEventArgs e)
    {
        if (searchOffset == 0)
            return;
        searchOffset = Math.Max(0, searchOffset - 15);
        await RunBrowse(SearchContent);
    }

    async void NextSearch(object s, RoutedEventArgs e)
    {
        if (searchOffset + 15 >= searchTotal)
            return;
        searchOffset += 15;
        await RunBrowse(SearchContent);
    }

    async void ProjectChanged(object s, SelectionChangedEventArgs e)
    {
        if (ContentSearchResultsControl.SelectedItem is not ContentProject project)
            return;
        ShowResourceDetail(project);
        await RunBrowse(async () =>
        {
            var token = browseCancellation?.Token ?? CancellationToken.None;
            var target = SearchTarget();
            ContentProjectTitleLabel.Text = project.Title;
            ContentProjectDescriptionLabel.Text = project.Description;
            ContentProjectVersionsControl.ItemsSource = null;
            ContentInstallButtonControl.IsEnabled = false;
            var type = ContentKind;
            OperationStatusLabel.Text = "读取兼容版本…";
            var presentation = LoadResourcePresentation(project, token);
            var list = await new ContentService(http, token).Versions(project.Id, target.GameVersion, target.Loader, type);
            token.ThrowIfCancellationRequested();
            if (releaseChannel.SelectedIndex > 0)
                list = list.Where(v => v.VersionType == new[] { "", "release", "beta", "alpha" }[releaseChannel.SelectedIndex]).ToList();
            ContentProjectVersionsControl.ItemsSource = list;
            ContentProjectVersionsControl.SelectedIndex = list.Count > 0 ? 0 : -1;
            ContentInstallButtonControl.IsEnabled = list.Count > 0;
            UpdateResourceVersionFacts();
            await presentation;
            if (list.Count == 0)
                ContentProjectDescriptionLabel.Text += "\n\n没有符合筛选条件的可下载版本。";
        });
    }

    internal void RequireStopped()
    {
        if (game is
            {
                HasExited: false
            })
            throw new IOException("请先关闭游戏，再修改实例内容。");
    }

    async Task InstallVersion(ContentVersion version)
    {
        RequireStopped();
        var target = Target();
        if (!LocalVersions().Contains(target.Version))
            throw new IOException("请先安装并选择本地游戏版本。");
        OperationStatusLabel.Text = "解析必需依赖…";
        var plan = await Modrinth.Resolve(version, target.GameVersion, ContentKind == "datapack" ? "datapack" : target.Loader);
        var destination = Instance();
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

        var count = await Modrinth.Install(plan, destination, text => operations.Current?.Report(text));
        Log($"安装完成：{version.Name}，共 {count} 项（含必需依赖）。更新前的文件保留在实例备份目录。");
        RefreshLocalContent();
        InlineMessageBox.Show($"已安装 {count} 项内容（含必需依赖）。\n手动导入的模组无法自动识别重复项目，请在内容管理中检查。", "安装完成");
    }

    async void InstallContentClick(object s, RoutedEventArgs e)
    {
        if (ContentProjectVersionsControl.SelectedItem is ContentVersion version)
            QueueContent(version);
        await Task.CompletedTask;
    }

    void ProjectPageClick(object s, RoutedEventArgs e)
    {
        if (ContentSearchResultsControl.SelectedItem is ContentProject project)
            Process.Start(new ProcessStartInfo("https://modrinth.com/project/" + Uri.EscapeDataString(project.Id)) { UseShellExecute = true });
    }

    internal void RefreshLocalContent()
    {
        if (InstalledContentListControl == null)
            return;
        try
        {
            localSnapshot = ContentService.ListLocal(ManagedDirectory());
            ApplyLocalSearch();
            if (managedWorld != null)
                ManageTargetLabel.Text = "数据包目标存档：" + managedWorld;
        }
        catch (Exception ex)
        {
            localSnapshot = [];
            InstalledContentListControl.ItemsSource = null;
            localSummary.Text = "";
            if (VersionSelectorControl.SelectedValue != null)
                Log("内容列表读取失败：" + ex.Message);
        }
    }

    void RefreshContentClick(object s, RoutedEventArgs e) => RefreshLocalContent();
    async void ToggleContentClick(object s, RoutedEventArgs e) => await Run(() =>
    {
        RequireStopped();
        if (InstalledContentListControl.SelectedItem is LocalContent item)
        {
            ContentService.Toggle(item);
            Log("切换内容状态：" + item.Name);
            RefreshLocalContent();
        }

        return Task.CompletedTask;
    });
    async void RemoveContentClick(object s, RoutedEventArgs e) => await Run(() =>
    {
        RequireStopped();
        if (InstalledContentListControl.SelectedItem is LocalContent item)
        {
            ContentService.MoveToBackup(ManagedDirectory(), item);
            Log("已移至备份：" + item.Name + "。其他模组可能依赖此项目，请检查依赖。");
            RefreshLocalContent();
        }

        return Task.CompletedTask;
    });

    async void UpdateContentClick(object s, RoutedEventArgs e)
    {
        if (busy)
            return;
        try
        {
            if (InstalledContentListControl.SelectedItem is not LocalContent
                {
                    Managed:
                    {
                    } managed
                })
                throw new IOException("请选择通过 Modrinth 安装的内容。");
            var target = Target();
            await OpenContentUpdates(ManagedDirectory(), target.Version, target.GameVersion, target.Loader, managed.ProjectId);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "内容更新");
        }
    }

    void TrashFolderClick(object s, RoutedEventArgs e)
    {
        try
        {
            OpenFolder(Path.Combine(ManagedDirectory(), ".blocklaunch", "backups"));
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    async void QuiltClick(object s, RoutedEventArgs e) => await Run(async () =>
    {
        var target = Target();
        if (target.Loader != "vanilla")
            throw new IOException("请先选择一个官方原版版本。");
        using var list = JsonDocument.Parse(await http.GetStringAsync("https://meta.quiltmc.org/v3/versions/loader/" + Uri.EscapeDataString(target.GameVersion), CurrentToken));
        var loader = list.RootElement.EnumerateArray().Where(e => System.Text.RegularExpressions.Regex.IsMatch(e.GetProperty("loader").GetProperty("version").GetString() ?? "", @"^\d+\.\d+\.\d+$")).OrderByDescending(e => Version.Parse(e.GetProperty("loader").GetProperty("version").GetString()!)).FirstOrDefault();
        if (loader.ValueKind == JsonValueKind.Undefined)
            throw new IOException("此游戏版本没有稳定 Quilt 加载器。");
        var version = loader.GetProperty("loader").GetProperty("version").GetString()!;
        EnqueueDownload("Quilt · " + target.GameVersion, target.GameVersion, new()
        {
            Version = target.GameVersion,
            Loader = "quilt",
            LoaderVersion = version
        });
    });
    async void LoaderJarClick(object s, RoutedEventArgs e) => await Run(async () =>
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
        var java = prefs.Java.Length > 0 ? prefs.Java : "java";
        // A profile marker allows the official installer to recognize an existing launcher directory.
        var marker = Path.Combine(prefs.Root, "launcher_profiles.json");
        if (!File.Exists(marker))
            await File.WriteAllTextAsync(marker, "{\"profiles\":{}}");
        var start = new ProcessStartInfo(java)
        {
            UseShellExecute = false,
            WorkingDirectory = prefs.Root,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-jar");
        start.ArgumentList.Add(dialog.FileName);
        start.ArgumentList.Add("--installClient");
        start.ArgumentList.Add(prefs.Root);
        using var installer = Process.Start(start) ?? throw new IOException("安装器无法启动。");
        async Task Drain(StreamReader reader)
        {
            var last = DateTime.MinValue;
            while (await reader.ReadLineAsync() is
                {
                } line)
                if ((DateTime.UtcNow - last).TotalSeconds >= 1)
                {
                    Log("安装器：" + line[..Math.Min(line.Length, 220)]);
                    last = DateTime.UtcNow;
                }
        }

        var output = Drain(installer.StandardOutput);
        var error = Drain(installer.StandardError);
        OperationStatusLabel.Text = "正在后台运行官方安装器…";
        try
        {
            await installer.WaitForExitAsync(CurrentToken);
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
        await Refresh();
        Log("官方安装器已完成，请在首页选择新增版本。");
    });
}
