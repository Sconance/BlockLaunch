using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class JavaManagerController(MainWindow host)
{
    public void Open(string root, string profile, Action<string> select, object? owner = null)
    {
        var panel = new StackPanel();
        var required = profile.Length > 0 ? JavaDiscovery.RequiredMajor(root, profile) : 0;
        panel.Children.Add(MainWindow.Heading("Java 管理"));
        panel.Children.Add(MainWindow.Hint(required > 0 ? $"当前版本需要 Java {required}。自动选择优先匹配大版本与 x64；手动选择在启动前再次校验。" : "选择实例后可显示 Java 版本要求；未选择时可以浏览、搜索和手动指定。"));
        panel.Children.Add(MainWindow.Hint("筛选 Java · 输入版本、发行商或路径"));
        var filters = new WrapPanel();
        var query = new TextBox
        {
            Width = 230,
            ToolTip = "搜索 Java 版本、发行商或路径"
        };
        var onlyCompatible = new CheckBox
        {
            Content = "只看兼容版本",
            IsEnabled = required > 0,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8)
        };
        filters.Children.Add(query);
        filters.Children.Add(onlyCompatible);
        panel.Children.Add(filters);
        var list = new ListBox
        {
            MinHeight = 100,
            MaxHeight = 300,
            DisplayMemberPath = "Label"
        };
        ScrollViewer.SetCanContentScroll(list, false);
        panel.Children.Add(list);
        var state = MainWindow.Hint("常见位置扫描包括环境变量、安装目录、已保存路径和游戏 runtime。全盘搜索仅遍历本地固定磁盘，跳过无权限目录与目录链接，最多搜索两分钟。遇到上限会明确提示。");
        panel.Children.Add(state);
        var dialog = host.FeatureDialog("Java 管理", panel);
        dialog.Owner = owner ?? host;
        using var lifetime = new CancellationTokenSource();
        CancellationTokenSource? active = null;
        dialog.Closed += (_, _) =>
        {
            lifetime.Cancel();
            active?.Cancel();
        };
        var items = new List<JavaInstallation>();
        void Filter()
        {
            var terms = query.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var filtered = items.Where(j => (onlyCompatible.IsChecked != true || j.Error.Length == 0 && j.Major == required) && terms.All(t => j.Label.Contains(t, StringComparison.OrdinalIgnoreCase))).OrderBy(j => required > 0 && j.Major == required ? 0 : 1).ThenBy(j => j.Error.Length > 0 ? 1 : 0).ThenBy(j => j.Architecture == "x64" ? 0 : 1).ThenByDescending(j => j.Major).ToList();
            list.ItemsSource = filtered;
            list.SelectedIndex = filtered.Count > 0 ? 0 : -1;
        }

        query.TextChanged += (_, _) => Filter();
        onlyCompatible.Checked += (_, _) => Filter();
        onlyCompatible.Unchecked += (_, _) => Filter();
        var tools = new WrapPanel();
        panel.Children.Add(tools);
        var starts = new List<Button>();
        async Task Scan(string[]? directories)
        {
            if (active != null)
                return;
            active = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            var scanToken = active.Token;
            foreach (var button in starts)
                button.IsEnabled = false;
            state.Text = "正在搜索 Java…";
            try
            {
                JavaSearchResult? found = null;
                if (directories == null)
                    items = await Task.Run(() => JavaDiscovery.Scan(root, host.prefs.ExtraJavaPaths.Append(host.prefs.Java), scanToken), scanToken);
                else
                {
                    var progress = new Progress<JavaSearchProgress>(p =>
                    {
                        if (!scanToken.IsCancellationRequested && active != null)
                            state.Text = $"已扫描 {p.Directories:N0} 个目录，发现 {p.Candidates} 个 Java 候选。可随时取消。";
                    });
                    found = await Task.Run(() => JavaDiscovery.SearchDirectories(directories, scanToken, progress), scanToken);
                    state.Text = $"搜索完成，正在校验 {found.Paths.Count} 个候选的版本…";
                    var inspected = await JavaDiscovery.InspectCandidates(found.Paths, scanToken);
                    items = items.Concat(inspected).GroupBy(j => j.Path, StringComparer.OrdinalIgnoreCase).Select(g => g.Last()).ToList();
                }

                scanToken.ThrowIfCancellationRequested();
                Filter();
                state.Text = $"共 {items.Count} 个候选，{items.Count(j => j.Error.Length == 0)} 个有效 Java。" + (found?.LimitReached == true ? "本次达到搜索上限，仅显示已找到结果；可选择具体目录继续搜索。" : "") + (items.Count == 0 ? "可尝试全盘搜索、指定目录，或添加 java.exe / javaw.exe。" : "");
            }
            catch (OperationCanceledException)
            {
                if (!lifetime.IsCancellationRequested)
                    state.Text = "已取消搜索，原有结果保留。";
            }
            catch (Exception ex)
            {
                if (!lifetime.IsCancellationRequested)
                    state.Text = "搜索失败：" + ex.Message;
            }
            finally
            {
                var completed = active;
                active = null;
                completed?.Dispose();
                if (!lifetime.IsCancellationRequested)
                    foreach (var button in starts)
                        button.IsEnabled = true;
            }
        }

        void StartButton(string label, RoutedEventHandler action)
        {
            var button = MainWindow.ActionButton(label, action);
            starts.Add(button);
            tools.Children.Add(button);
        }

        StartButton("扫描本地 Java", async (_, _) => await Scan(null));
        StartButton("全盘搜索 Java", async (_, _) => await Scan(JavaDiscovery.FixedDriveRoots().ToArray()));
        StartButton("搜索指定目录", async (_, _) =>
        {
            var picker = new OpenFolderDialog
            {
                Title = "选择 Java 搜索目录"
            };
            if (picker.ShowDialog(dialog) == true)
                await Scan([picker.FolderName]);
        });
        tools.Children.Add(MainWindow.ActionButton("取消搜索", (_, _) => active?.Cancel()));
        StartButton("添加 Java 路径", async (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "Java 程序|java.exe;javaw.exe"
            };
            if (picker.ShowDialog(dialog) != true)
                return;
            try
            {
                var inspected = await JavaDiscovery.InspectCandidates([picker.FileName], lifetime.Token);
                if (inspected.Count == 0 || inspected[0].Error.Length > 0)
                    throw new IOException(inspected.FirstOrDefault()?.Error ?? "无效 Java 路径。");
                if (!host.prefs.ExtraJavaPaths.Contains(picker.FileName, StringComparer.OrdinalIgnoreCase))
                    host.prefs.ExtraJavaPaths.Add(picker.FileName);
                host.settings.Save(host.prefs);
                items = items.Where(j => !j.Path.Equals(inspected[0].Path, StringComparison.OrdinalIgnoreCase)).Concat(inspected).ToList();
                Filter();
                state.Text = "Java 路径已校验并保存，请选择后应用。";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                state.Text = ex.Message;
            }
        });
        panel.Children.Add(MainWindow.ActionButton("使用所选 Java", (_, _) =>
        {
            try
            {
                if (active != null)
                    throw new IOException("请先完成或取消搜索。");
                if (list.SelectedItem is not JavaInstallation java || java.Error.Length > 0 || required > 0 && java.Major != required)
                    throw new IOException("请选择有效且匹配游戏要求的 Java。");
                select(java.Path);
                dialog.Close();
            }
            catch (Exception ex)
            {
                state.Text = ex.Message;
            }
        }));
        panel.Children.Add(MainWindow.ActionButton("恢复自动选择", (_, _) =>
        {
            try
            {
                select("");
                dialog.Close();
            }
            catch (Exception ex)
            {
                state.Text = ex.Message;
            }
        }));
        dialog.ShowDialog();
    }
}
