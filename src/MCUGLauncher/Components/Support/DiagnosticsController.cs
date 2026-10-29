using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class DiagnosticsController
{
    readonly LauncherWorkspace workspace;
    internal DiagnosticsController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal DiagnosticContext? recentDiagnosticContext;
    internal DiagnosticSnapshot? diagnosticSnapshot;
    internal DiagnosticContext? lastAnalyzedContext;
    internal readonly StackPanel diagnosticResults = new();
    internal readonly TextBlock diagnosticState = FeatureUiElements.Hint("尚未分析。可以分析当前选择的实例，或最近一次启动的实例。");
    internal int diagnosticTabIndex;
    internal void BuildDiagnosticsUI()
    {
        diagnosticTabIndex = workspace.Window.MainTabs.Items.Count;
        var nav = new RadioButton
        {
            Style = (Style)workspace.Window.FindResource("Nav"),
            Content = "◇     诊断与反馈",
            GroupName = "Navigation"
        };
        ((StackPanel)workspace.Controls.ManageNav.Parent).Children.Add(nav);
        nav.Checked += (_, _) =>
        {
            workspace.Window.MainTabs.SelectedIndex = diagnosticTabIndex;
            workspace.Controls.PageTitle.Text = "诊断与反馈";
            workspace.Controls.PageSubtitle.Text = "本地分析 · 日志证据 · 导出前预览";
        };
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("游戏退出排查"));
        body.Children.Add(FeatureUiElements.Hint("规则提示是可能原因，不是确定结论。主动关闭不会自动判为崩溃。读取有限日志、模组文件名和当前实例的 JAR 元数据，不读取账号缓存或存档内容。"));
        var actions = new WrapPanel();
        actions.Children.Add(FeatureUiElements.ActionButton("分析当前实例", async (_, _) => await workspace.LaunchCoordinator.Run(async () =>
        {
            var descriptor = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root).GetOrCreate(workspace.LaunchCoordinator.Selected());
            var context = new DiagnosticContext(workspace.LaunchCoordinator.Instance(), descriptor.LaunchProfileId);
            await AnalyzeDiagnostics(context);
        }, "分析当前实例日志")));
        actions.Children.Add(FeatureUiElements.ActionButton("分析最近启动", async (_, _) =>
        {
            if (recentDiagnosticContext == null)
            {
                InlineMessageBox.Show("本次打开启动器后尚未启动游戏，请先分析当前实例。");
                return;
            }

            var context = recentDiagnosticContext;
            await workspace.LaunchCoordinator.Run(() => AnalyzeDiagnostics(context), "分析最近启动日志");
        }));
        actions.Children.Add(FeatureUiElements.ActionButton("预览并导出反馈 ZIP", (_, _) => FeedbackPreview()));
        actions.Children.Add(FeatureUiElements.ActionButton("修复后重新检查", async (_, _) =>
        {
            if (lastAnalyzedContext != null)
                await workspace.LaunchCoordinator.Run(() => AnalyzeDiagnostics(lastAnalyzedContext), "重新检查诊断");
        }));
        body.Children.Add(actions);
        body.Children.Add(diagnosticState);
        body.Children.Add(diagnosticResults);
        workspace.Window.MainTabs.Items.Add(new TabItem { Header = "诊断与反馈", Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
        var logActions = ((Grid)workspace.Controls.GameLogView.Parent).Children.OfType<WrapPanel>().Single();
        logActions.Children.Add(FeatureUiElements.ActionButton("诊断与反馈", (_, _) => nav.IsChecked = true));
    }

    internal async Task AnalyzeDiagnostics(DiagnosticContext context)
    {
        lastAnalyzedContext = context;
        var snapshot = await Task.Run(() => Diagnostics.Capture(context, workspace.LaunchCoordinator.CurrentToken), workspace.LaunchCoordinator.CurrentToken);
        try
        {
            var req = workspace.InstanceWorkspaceController.RequirementsFor(context.Profile);
            var scan = await Task.Run(() => ModPreflight.Scan(context.Directory, req.Minecraft, req.Loader, JavaDiscovery.RequiredMajor(workspace.LaunchCoordinator.prefs.Root, context.Profile), req.LoaderVersion, workspace.LaunchCoordinator.CurrentToken), workspace.LaunchCoordinator.CurrentToken);
            var extra = scan.Findings.Where(f => f.Severity == "错误").Select(f => new DiagnosticFinding("本地 Mod 检查", f.Message, f.File, "Mod 元数据")).ToList();
            var mentioned = scan.Mods.Where(m => snapshot.Logs.Any(log => System.Text.RegularExpressions.Regex.IsMatch(log.Text, @"(?<![\w])" + System.Text.RegularExpressions.Regex.Escape(m.Id) + @"(?![\w])", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(200)))).Take(20).ToList();
            if (mentioned.Count > 0)
                extra.Add(new("日志提及的模组（排查线索）", "被日志提及不代表该模组是崩溃原因；结合上方证据核对，不要直接删除。", string.Join("\n", mentioned.Select(m => m.Id + " → " + m.File)), "Mod 元数据"));
            snapshot = snapshot with
            {
                Findings = snapshot.Findings.Concat(extra).ToList()
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            snapshot = snapshot with
            {
                Notices = snapshot.Notices.Append("Mod 元数据补充检查未完成：" + ex.Message).ToList()
            };
        }

        ShowDiagnosticSnapshot(snapshot);
    }

    internal void ShowDiagnosticSnapshot(DiagnosticSnapshot snapshot)
    {
        diagnosticSnapshot = snapshot;
        diagnosticState.Text = $"{snapshot.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {snapshot.Summary}";
        diagnosticResults.Children.Clear();
        foreach (var finding in snapshot.Findings)
        {
            var card = new StackPanel();
            card.Children.Add(FeatureUiElements.Heading(finding.Title));
            card.Children.Add(FeatureUiElements.Hint(finding.Advice));
            card.Children.Add(new TextBox { IsReadOnly = true, Text = finding.Source + "\n" + finding.Evidence, TextWrapping = TextWrapping.Wrap, MaxHeight = 140, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            if (snapshot.Logs.Any(log => log.Name == finding.Source))
                card.Children.Add(FeatureUiElements.ActionButton("跳转日志证据", (_, _) => OpenDiagnosticEvidence(snapshot, finding)));
            diagnosticResults.Children.Add(workspace.InstanceWorkspaceController.FeatureCard(card));
        }

        foreach (var notice in snapshot.Notices)
            diagnosticResults.Children.Add(FeatureUiElements.Hint(notice));
        diagnosticResults.Children.Add(FeatureUiElements.Hint($"已采集 {snapshot.Logs.Count} 份有限日志快照。点击“预览并导出反馈 ZIP”可查看实际文件内容并选择导出范围。"));
    }

    internal void OpenDiagnosticEvidence(DiagnosticSnapshot snapshot, DiagnosticFinding finding)
    {
        var source = snapshot.Logs.FirstOrDefault(log => log.Name == finding.Source);
        if (source == null)
            return;
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading(source.Name));
        body.Children.Add(FeatureUiElements.Hint("以下为诊断时采集的有限脱敏快照；不会打开账号文件。"));
        var text = new TextBox
        {
            Text = source.Text,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            Height = 420,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        body.Children.Add(text);
        var dialog = workspace.InstanceWorkspaceController.FeatureDialog("日志证据", body);
        dialog.Loaded += (_, _) =>
        {
            var index = source.Text.IndexOf(finding.Evidence.TrimEnd('…'), StringComparison.Ordinal);
            if (index >= 0)
            {
                text.Focus();
                text.Select(index, Math.Min(finding.Evidence.Length, source.Text.Length - index));
                text.ScrollToLine(text.GetLineIndexFromCharacterIndex(index));
            }
        };
        dialog.ShowDialog();
    }

    internal void FeedbackPreview()
    {
        if (diagnosticSnapshot is not { } snapshot)
        {
            InlineMessageBox.Show("请先分析实例，生成可预览的快照。");
            return;
        }

        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("反馈包范围与文本预览"));
        body.Children.Add(FeatureUiElements.Hint("不会自动上传。请逐项检查下方实际文本；自动过滤无法保证完全脱敏，聊天、玩家名和服务器域名等仍可能存在。不希望分享日志时，取消日志勾选。"));
        var logs = new CheckBox
        {
            Content = "包含日志与诊断证据（最多每份 1 MiB）",
            IsChecked = true,
            Margin = new Thickness(0, 6, 0, 6)
        };
        var mods = new CheckBox
        {
            Content = "包含模组文件名清单，不含 JAR 本体",
            IsChecked = true,
            Margin = new Thickness(0, 6, 0, 6)
        };
        var environment = new CheckBox
        {
            Content = "包含系统架构、运行环境与游戏配置标识",
            IsChecked = true,
            Margin = new Thickness(0, 6, 0, 6)
        };
        body.Children.Add(logs);
        body.Children.Add(mods);
        body.Children.Add(environment);
        var fileTemplate = new DataTemplate(typeof(FeedbackDocument));
        var fileName = new FrameworkElementFactory(typeof(TextBlock));
        fileName.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
        fileTemplate.VisualTree = fileName;
        var files = new ComboBox
        {
            ItemTemplate = fileTemplate
        };
        var size = FeatureUiElements.Hint("");
        var text = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 290,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12
        };
        IReadOnlyList<FeedbackDocument> documents = [];
        void RefreshPreview()
        {
            documents = Diagnostics.Preview(snapshot, new(logs.IsChecked == true, mods.IsChecked == true, environment.IsChecked == true));
            files.ItemsSource = documents;
            files.SelectedIndex = 0;
            size.Text = $"{documents.Count} 个文件 · 未压缩文本约 {documents.Sum(d => System.Text.Encoding.UTF8.GetByteCount(d.Text)) / 1024.0:N1} KiB · 快照不会随游戏日志继续变化";
        }

        files.SelectionChanged += (_, _) => text.Text = (files.SelectedItem as FeedbackDocument)?.Text ?? "";
        foreach (var check in new[]
        {
            logs,
            mods,
            environment
        }

        )
        {
            check.Checked += (_, _) => RefreshPreview();
            check.Unchecked += (_, _) => RefreshPreview();
        }

        body.Children.Add(size);
        body.Children.Add(files);
        body.Children.Add(text);
        RefreshPreview();
        var dialog = workspace.InstanceWorkspaceController.FeatureDialog("反馈包预览（仅保存到本机）", body);
        body.Children.Add(FeatureUiElements.ActionButton("选择位置并导出这些文本", async (_, _) =>
        {
            var picker = new SaveFileDialog
            {
                Filter = "反馈 ZIP|*.zip",
                FileName = "MCUGLauncher-Feedback-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip"
            };
            if (picker.ShowDialog(dialog) != true)
                return;
            var frozen = documents.ToArray();
            dialog.Close();
            await workspace.LaunchCoordinator.Run(async () =>
            {
                await Task.Run(() => Diagnostics.Export(frozen, picker.FileName, workspace.LaunchCoordinator.CurrentToken), workspace.LaunchCoordinator.CurrentToken);
                workspace.LaunchCoordinator.Log("反馈包已保存到本机：" + picker.FileName + "；未自动上传。");
            }, "导出反馈快照");
        }));
        dialog.ShowDialog();
    }
}
