using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class ExperienceController
{
    readonly LauncherWorkspace workspace;
    internal ExperienceController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal string? nextServerAddress;
    internal string SaveBackupRoot => Path.Combine(workspace.LaunchCoordinator.settingsDir, "world-backups");

    internal void BuildExperienceUI()
    {
        var downloadTasks = new MenuItem
        {
            Header = "下载任务"
        };
        downloadTasks.Click += (_, _) => workspace.DownloadTaskController.OpenDownloadTasks();
        workspace.CompactShellController.moreMenu.Items.Add(downloadTasks);
        var tools = new MenuItem
        {
            Header = "Java 管理"
        };
        tools.Click += (_, _) => workspace.JavaManagerController.OpenJavaManager(workspace.LaunchCoordinator.prefs.Root, workspace.Controls.Versions.SelectedValue as string ?? "", path =>
        {
            if (workspace.LaunchCoordinator.busy)
                throw new IOException("请等待任务完成再更改 Java。");
            workspace.LaunchCoordinator.prefs.Java = path;
            workspace.LaunchCoordinator.prefs.Launch.Java = path;
            workspace.Controls.JavaPath.Text = path;
            workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
        });
        workspace.CompactShellController.moreMenu.Items.Add(tools);
        var backups = new MenuItem
        {
            Header = "存档保护与恢复"
        };
        backups.Click += (_, _) => OpenSaveProtection();
        workspace.CompactShellController.moreMenu.Items.Add(backups);
        var servers = new MenuItem
        {
            Header = "服务器收藏"
        };
        servers.Click += (_, _) => OpenServers();
        workspace.CompactShellController.moreMenu.Items.Add(servers);
        var appearance = new MenuItem
        {
            Header = "界面与辅助功能"
        };
        appearance.Click += (_, _) => workspace.AppearanceController.OpenAppearance();
        workspace.CompactShellController.moreMenu.Items.Add(appearance);
        var queue = (WrapPanel)((Grid)((TabItem)workspace.Window.MainTabs.Items[6]).Content).Children.OfType<WrapPanel>().Single();
        foreach (var pair in new[]
        {
            ("上移", -1),
            ("下移", 1)
        }

        )
            queue.Children.Add(InstancePagesController.ActionButton(pair.Item1, (_, _) =>
            {
                if (workspace.Controls.TaskHistoryList.SelectedItem is OperationRecord record)
                {
                    workspace.LaunchCoordinator.operations.MovePending(record.Id, pair.Item2);
                    workspace.StabilityController.RefreshOperations();
                }
            }));
        queue.Children.Add(InstancePagesController.ActionButton("下载限速", (_, _) => OpenDownloadLimit()));
        workspace.Window.Width = Math.Min(workspace.Window.Width, Math.Max(780, SystemParameters.WorkArea.Width - 24));
        workspace.Window.Height = Math.Min(workspace.Window.Height, Math.Max(540, SystemParameters.WorkArea.Height - 24));
        workspace.Window.MinWidth = 780;
        workspace.Window.MinHeight = 540;
        workspace.MiniDownloadController.miniDownloadProgress.Foreground = (Brush)workspace.Window.FindResource("AccentBrush");
        workspace.Window.Loaded += (_, _) => workspace.ExperienceThemeController.ThemeWindow(workspace.Window);
    }

    internal async Task ProtectWorlds(string directory, string reason, CancellationToken token)
    {
        if (!workspace.LaunchCoordinator.prefs.ProtectSaves)
            return;
        workspace.ContentController.RequireStopped();
        var registry = Path.Combine(workspace.LaunchCoordinator.prefs.Root, "instances");
        var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
        var registered = Directory.Exists(registry) && Directory.EnumerateDirectories(registry).Any(d => File.Exists(Path.Combine(d, ".blocklaunch-instance.json")) && Path.GetFullPath(catalog.DirectoryFor(Path.GetFileName(d))).Equals(Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase));
        if (!registered && !InstanceUninstaller.Inside(directory, workspace.LaunchCoordinator.prefs.Root) && !Path.GetFullPath(directory).Equals(Path.GetFullPath(workspace.LaunchCoordinator.prefs.Root), StringComparison.OrdinalIgnoreCase))
            throw new IOException("存档保护目录未登记在当前游戏目录中。");
        var backup = await Task.Run(() => SaveProtection.Create(directory, SaveBackupRoot, reason, workspace.LaunchCoordinator.prefs.SaveBackupCount, token), token);
        if (backup != null)
            workspace.LaunchCoordinator.Log($"存档保护完成：{backup.Worlds.Count} 个世界 · {reason}");
    }

    internal bool ConfirmModScan(ModScan scan, string title)
    {
        var panel = new StackPanel();
        panel.Children.Add(InstancePagesController.Heading(scan.Summary));
        panel.Children.Add(InstancePagesController.Hint("只检查已启用 JAR 的可识别元数据；未知条件不当作兼容结论。不会自动移动或删除模组。"));
        foreach (var finding in scan.Findings.Take(200))
            panel.Children.Add(InstancePagesController.Hint($"{finding.Severity} · {finding.File}\n{finding.Message}"));
        var dialog = workspace.InstancePagesController.FeatureDialog(title, panel);
        var actions = new WrapPanel();
        actions.Children.Add(InstancePagesController.ActionButton("返回检查", (_, _) => dialog.DialogResult = false));
        actions.Children.Add(InstancePagesController.ActionButton("了解风险，继续启动", (_, _) => dialog.DialogResult = true));
        panel.Children.Add(actions);
        return dialog.ShowDialog() == true;
    }

    internal async Task ShowModScan(string key, string directory, object? owner = null)
    {
        try
        {
            var descriptor = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).GetOrCreate(key);
            var req = workspace.InstancePagesController.RequirementsFor(descriptor.LaunchProfileId);
            var scan = await Task.Run(() => ModPreflight.Scan(directory, req.Minecraft, req.Loader, JavaDiscovery.RequiredMajor(workspace.LaunchCoordinator.prefs.Root, descriptor.LaunchProfileId), req.LoaderVersion));
            var panel = new StackPanel();
            panel.Children.Add(InstancePagesController.Heading(scan.Summary));
            panel.Children.Add(InstancePagesController.Hint("扫描启用的 JAR，含 Fabric 内嵌模组；复杂条件或未知元数据会提示人工核对。"));
            foreach (var finding in scan.Findings.Take(200))
                panel.Children.Add(workspace.InstancePagesController.FeatureCard(new StackPanel { Children = { InstancePagesController.Heading(finding.Severity + " · " + finding.File), InstancePagesController.Hint(finding.Message) } }));
            if (scan.Findings.Count == 0)
                panel.Children.Add(InstancePagesController.Hint("未发现已知元数据问题；仍需由加载器完成实际兼容性验证。"));
            var dialog = workspace.InstancePagesController.FeatureDialog("本地 Mod 检查", panel);
            dialog.Owner = owner ?? workspace.Window;
            panel.Children.Add(InstancePagesController.ActionButton("打开 Mod 文件夹", (_, _) => workspace.LaunchCoordinator.OpenFolder(Path.Combine(directory, "mods"))));
            panel.Children.Add(InstancePagesController.ActionButton("修复后重新检查", async (_, _) =>
            {
                dialog.Close();
                await ShowModScan(key, directory, owner);
            }));
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(owner ?? workspace.Window, ex.Message, "Mod 检查失败");
        }
    }

    internal void OpenDownloadLimit()
    {
        var body = new StackPanel();
        body.Children.Add(InstancePagesController.Heading("下载限速"));
        body.Children.Add(InstancePagesController.Hint("单位 KiB/s，0 为不限速。对启动器 HTTP 下载立即生效；外部 Forge/NeoForge 安装器自身的网络请求不受此限制。"));
        var input = new TextBox
        {
            Text = workspace.LaunchCoordinator.prefs.Network.LimitKibPerSecond.ToString()
        };
        body.Children.Add(input);
        var dialog = workspace.InstancePagesController.FeatureDialog("下载限速", body);
        dialog.Height = 290;
        body.Children.Add(InstancePagesController.ActionButton("应用", (_, _) =>
        {
            try
            {
                if (!int.TryParse(input.Text, out var limit) || limit is < 0 or > 1048576)
                    throw new IOException("请输入 0–1048576。");
                workspace.LaunchCoordinator.prefs.Network.LimitKibPerSecond = limit;
                workspace.LaunchCoordinator.runtimeNetwork.LimitKibPerSecond = limit;
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                dialog.Close();
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(dialog, ex.Message);
            }
        }));
        dialog.ShowDialog();
    }

    internal void OpenSaveProtection()
    {
        try
        {
            var key = workspace.LaunchCoordinator.Selected();
            var directory = workspace.LaunchCoordinator.Instance();
            var body = new StackPanel();
            body.Children.Add(InstancePagesController.Heading("存档保护与恢复"));
            body.Children.Add(InstancePagesController.Hint("修改加载器和确认整合包升级前备份。恢复创建新世界，不覆盖现有存档。备份按当前实例独立保留。"));
            var enabled = new CheckBox
            {
                Content = "操作前自动备份存档",
                IsChecked = workspace.LaunchCoordinator.prefs.ProtectSaves
            };
            var count = new TextBox
            {
                Text = workspace.LaunchCoordinator.prefs.SaveBackupCount.ToString()
            };
            body.Children.Add(enabled);
            body.Children.Add(InstancePagesController.Hint("保留最近几次备份（1–100）"));
            body.Children.Add(count);
            var choices = new ListBox
            {
                MinHeight = 70,
                MaxHeight = 220,
                DisplayMemberPath = "Label"
            };
            body.Children.Add(choices);
            var preview = InstancePagesController.Hint("选择备份查看内容。");
            body.Children.Add(preview);
            void RefreshList()
            {
                choices.ItemsSource = SaveProtection.List(SaveBackupRoot, directory).SelectMany(b => b.Worlds.Select(w => new SaveChoice(b, w, $"{b.Created.ToLocalTime():yyyy-MM-dd HH:mm} · {w.World} · {w.Version} · {b.Reason}"))).ToList();
            }

            RefreshList();
            choices.SelectionChanged += (_, _) =>
            {
                if (choices.SelectedItem is SaveChoice item)
                {
                    var path = SaveProtection.ArchivePath(SaveBackupRoot, directory, item.Backup, item.World);
                    using var zip = System.IO.Compression.ZipFile.OpenRead(path);
                    preview.Text = $"将恢复为新世界：{item.World.World}\n保存版本：{item.World.Version} · {zip.Entries.Count} 个归档条目 · {new FileInfo(path).Length / 1048576d:0.0} MiB\n原世界保持不变。";
                }
            };
            var dialog = workspace.InstancePagesController.FeatureDialog("存档保护", body);
            body.Children.Add(InstancePagesController.ActionButton("保存策略", (_, _) =>
            {
                if (!int.TryParse(count.Text, out var keep) || keep is < 1 or > 100)
                {
                    InlineMessageBox.Show(dialog, "保留数量应为 1–100。");
                    return;
                }

                workspace.LaunchCoordinator.prefs.ProtectSaves = enabled.IsChecked == true;
                workspace.LaunchCoordinator.prefs.SaveBackupCount = keep;
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                preview.Text = "策略已保存。";
            }));
            body.Children.Add(InstancePagesController.ActionButton("立即备份所有世界", async (_, _) =>
            {
                if (workspace.LaunchCoordinator.busy)
                {
                    preview.Text = "请等待当前任务完成。";
                    return;
                }

                await workspace.LaunchCoordinator.Run(async () =>
                {
                    workspace.ContentController.RequireStopped();
                    await Task.Run(() => SaveProtection.Create(directory, SaveBackupRoot, "手动备份", workspace.LaunchCoordinator.prefs.SaveBackupCount, workspace.LaunchCoordinator.CurrentToken), workspace.LaunchCoordinator.CurrentToken);
                }, "备份存档");
                RefreshList();
            }));
            body.Children.Add(InstancePagesController.ActionButton("确认预览，恢复为新世界", async (_, _) =>
            {
                if (choices.SelectedItem is not SaveChoice item || workspace.LaunchCoordinator.busy)
                    return;
                var path = SaveProtection.ArchivePath(SaveBackupRoot, directory, item.Backup, item.World);
                await workspace.LaunchCoordinator.Run(async () =>
                {
                    workspace.ContentController.RequireStopped();
                    var restored = await Task.Run(() => WorldArchives.Import(directory, path, item.World.World + "-恢复", token: workspace.LaunchCoordinator.CurrentToken), workspace.LaunchCoordinator.CurrentToken);
                    preview.Text = "已恢复为独立世界：" + restored;
                }, "恢复存档副本");
            }));
            body.Children.Add(InstancePagesController.ActionButton("打开备份目录", (_, _) => workspace.LaunchCoordinator.OpenFolder(SaveProtection.DirectoryFor(SaveBackupRoot, directory))));
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(workspace.Window, ex.Message, "存档保护");
        }
    }

    internal record SaveChoice(SaveBackup Backup, SaveBackupItem World, string Label);
    internal void OpenServers()
    {
        var body = new StackPanel();
        body.Children.Add(InstancePagesController.Heading("服务器收藏"));
        body.Children.Add(InstancePagesController.Hint("收藏保存在启动器配置中。点击查询才连接服务器，显示 MOTD、版本、人数和 Ping；暂不解析 DNS SRV，必要时填写实际端口。"));
        var list = new ListBox
        {
            MinHeight = 70,
            MaxHeight = 220,
            DisplayMemberPath = "Name"
        };
        body.Children.Add(list);
        var name = new TextBox
        {
            ToolTip = "服务器名称"
        };
        var address = new TextBox
        {
            ToolTip = "域名:端口 或 [IPv6]:端口"
        };
        var profiles = new ComboBox
        {
            ItemsSource = workspace.LaunchCoordinator.LocalVersions().Order().ToList(),
            SelectedItem = workspace.Controls.Versions.SelectedValue as string
        };
        body.Children.Add(InstancePagesController.Hint("名称 / 地址 / 启动实例"));
        body.Children.Add(name);
        body.Children.Add(address);
        body.Children.Add(profiles);
        var status = InstancePagesController.Hint("选择或添加服务器。");
        body.Children.Add(status);
        void Reload()
        {
            var id = (list.SelectedItem as ServerFavorite)?.Id;
            list.ItemsSource = workspace.LaunchCoordinator.prefs.Servers.ToList();
            list.SelectedItem = workspace.LaunchCoordinator.prefs.Servers.FirstOrDefault(s => s.Id == id);
        }

        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is ServerFavorite item)
            {
                name.Text = item.Name;
                address.Text = item.Address;
                profiles.SelectedItem = item.Profile;
                status.Text = "尚未查询服务器状态。";
            }
        };
        Reload();
        var dialog = workspace.InstancePagesController.FeatureDialog("服务器收藏", body);
        using var cts = new CancellationTokenSource();
        dialog.Closed += (_, _) => cts.Cancel();
        body.Children.Add(InstancePagesController.ActionButton("新增 / 保存", (_, _) =>
        {
            try
            {
                var endpoint = ServerEndpoint.Parse(address.Text);
                if (string.IsNullOrWhiteSpace(name.Text) || name.Text.Length > 100)
                    throw new IOException("名称应为 1–100 个字符。");
                if (profiles.SelectedItem is not string profile)
                    throw new IOException("请选择已安装实例。");
                var old = list.SelectedItem as ServerFavorite;
                var item = new ServerFavorite(old?.Id ?? Guid.NewGuid().ToString("N"), name.Text.Trim(), endpoint.ToString(), profile);
                if (old != null)
                    workspace.LaunchCoordinator.prefs.Servers.Remove(old);
                workspace.LaunchCoordinator.prefs.Servers.Add(item);
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                Reload();
                list.SelectedItem = item;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }));
        body.Children.Add(InstancePagesController.ActionButton("清空选择，添加新收藏", (_, _) =>
        {
            list.SelectedItem = null;
            name.Clear();
            address.Clear();
        }));
        body.Children.Add(InstancePagesController.ActionButton("查询在线状态", async (_, _) =>
        {
            var captured = address.Text;
            status.Text = "正在查询…";
            try
            {
                var result = await ServerFavorites.Ping(captured, cts.Token);
                if (address.Text == captured && !cts.IsCancellationRequested)
                    status.Text = $"{result.Description}\n{result.Version} · {result.Online}/{result.Maximum} 人 · {result.LatencyMs} ms";
            }
            catch (OperationCanceledException)
            {
                if (!cts.IsCancellationRequested)
                    status.Text = "查询超时；不代表服务器一定离线。";
            }
            catch (Exception ex)
            {
                status.Text = "无法查询：" + ex.Message;
            }
        }));
        body.Children.Add(InstancePagesController.ActionButton("启动并连接所选服务器", async (_, _) =>
        {
            if (workspace.LaunchCoordinator.busy || list.SelectedItem is not ServerFavorite item)
                return;
            if (!workspace.LaunchCoordinator.LocalVersions().Contains(item.Profile))
            {
                status.Text = "对应实例已不存在，请重新选择并保存。";
                return;
            }

            workspace.Controls.Versions.SelectedValue = item.Profile;
            dialog.Close();
            nextServerAddress = item.Address;
            try
            {
                await workspace.LaunchCoordinator.Run(workspace.LaunchCoordinator.Launch, "连接收藏服务器");
            }
            finally
            {
                nextServerAddress = null;
            }
        }));
        body.Children.Add(InstancePagesController.ActionButton("移除收藏", (_, _) =>
        {
            if (list.SelectedItem is ServerFavorite item)
            {
                workspace.LaunchCoordinator.prefs.Servers.Remove(item);
                workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
                Reload();
            }
        }));
        dialog.ShowDialog();
    }
}
