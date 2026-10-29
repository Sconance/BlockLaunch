using System.IO;
using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;
using Microsoft.Win32;

namespace BlockLaunch.Front;

public sealed class VersionManagerWindow : Window
{
    readonly IFrontLauncher launcher;
    readonly IFrontVersions versions;
    readonly string key;
    readonly TabControl pages = new();
    readonly StackPanel navigation = new();
    readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 6, 12, 10) };
    readonly CancellationTokenSource lifetime = new();
    bool busy;
    bool closed;
    bool closeAfterAction;
    string? upgradePreviewId;
    LaunchSettingsEditor? launchEditor;
    public string? CreatedInstance { get; private set; }

    public VersionManagerWindow(IFrontLauncher launcher, IFrontVersions versions, string key)
    {
        this.launcher = launcher;
        this.versions = versions;
        this.key = key;
        var instance = versions.ReadInstance(key);
        Title = "版本设置 · " + instance.Name;
        Width = 920;
        Height = 710;
        MinWidth = 820;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "ThemeSurfaceBrush");
        var root = new DockPanel();
        DockPanel.SetDock(status, Dock.Bottom);
        root.Children.Add(status);
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.Children.Add(navigation);
        Grid.SetColumn(pages, 1);
        pages.Style = (Style)FindResource("HiddenPages");
        layout.Children.Add(pages);
        root.Children.Add(layout);
        Content = root;
        Overview(instance);
        LaunchSettings(instance);
        Modify(instance);
        Management(instance);
        var content = Page("Mod 管理");
        var contentTabs = new TabControl();
        content.Children.Add(contentTabs);
        LocalContent(contentTabs, "模组", "mods");
        LocalContent(contentTabs, "资源包", "resourcepacks");
        LocalContent(contentTabs, "光影", "shaderpacks");
        LocalContent(contentTabs, "数据包", "datapacks");
        Worlds(contentTabs);
        Export();
        ((RadioButton)navigation.Children[0]).IsChecked = true;
        Closing += (_, args) =>
        {
            if (busy)
            {
                args.Cancel = true;
                status.Text = "正在处理文件，请等待操作完成后关闭。";
            }
        };
        Closed += (_, _) =>
        {
            closed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            try
            {
                if (upgradePreviewId != null && launcher is IFrontManagement management)
                    management.DiscardPackUpgrade(upgradePreviewId);
            }
            catch (Exception exception)
            {
                MessageBox.Show(Owner, "清理升级暂存文件失败：" + exception.Message, "清理提示");
            }
        };
    }

    StackPanel Page(string title)
    {
        var body = new StackPanel { Margin = new Thickness(18) };
        var index = pages.Items.Count;
        pages.Items.Add(new TabItem { Header = title, Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
        var button = new RadioButton { Content = title, GroupName = "VersionPages", Style = (Style)FindResource("Nav") };
        button.Checked += (_, _) => pages.SelectedIndex = index;
        navigation.Children.Add(button);
        return body;
    }

    static T Field<T>(Panel body, string label, T input) where T : UIElement
    {
        body.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 4) });
        body.Children.Add(input);
        return input;
    }

    void Button(Panel body, string label, Func<Task> action)
    {
        var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 8, 0) };
        button.Click += async (_, _) => await Run(action);
        body.Children.Add(button);
    }

    Task Done(Action action) { action(); return Task.CompletedTask; }

    async Task Run(Func<Task> action)
    {
        if (busy || closed)
            return;
        busy = true;
        pages.IsEnabled = false;
        status.Text = "正在处理…";
        try
        {
            await action();
            status.Text = launcher.Status;
        }
        catch (OperationCanceledException) { status.Text = "操作已取消。"; }
        catch (Exception exception) { status.Text = "操作失败：" + exception.Message; }
        finally
        {
            busy = false;
            pages.IsEnabled = true;
            if (closeAfterAction && !closed)
                DialogResult = true;
        }
    }

    void Overview(FrontInstance instance)
    {
        var body = Page("概览");
        var name = Field(body, "版本名称", new TextBox { Text = instance.Name });
        var description = Field(body, "版本描述", new TextBox { Text = instance.Description, AcceptsReturn = true, MinHeight = 70, TextWrapping = TextWrapping.Wrap });
        var category = Field(body, "分类", new ComboBox { ItemsSource = new[] { "自动", "原版游戏", "整合包", "常用版本", "测试版本" }, SelectedItem = instance.Category });
        var favorite = Field(body, "收藏", new CheckBox { Content = "加入收藏夹", IsChecked = instance.Favorite });
        Field(body, "实例标识", new TextBlock { Text = instance.Key });
        Field(body, "游戏目录", new TextBlock { Text = instance.Directory, TextWrapping = TextWrapping.Wrap });
        Button(body, "打开实例目录", () => Done(() => versions.OpenInstanceDirectory(key, "")));
        Button(body, "保存版本信息", () => Done(() =>
        {
            versions.SaveInstanceIdentity(key, name.Text, description.Text, category.Text, favorite.IsChecked == true);
            Title = "版本设置 · " + name.Text.Trim();
        }));
    }

    void LaunchSettings(FrontInstance instance)
    {
        var body = Page("设置");
        var editor = new LaunchSettingsEditor(instance.Launch);
        launchEditor = editor;
        body.Children.Add(new TextBlock { Text = instance.UsesGlobalSettings ? "当前跟随全局设置；保存后仅对此实例生效。" : "当前使用实例专属设置。" });
        body.Children.Add(editor);
        Button(body, "保存版本设置", () => Done(() => versions.SaveInstanceLaunch(key, editor.Read())));
        Button(body, "恢复跟随全局设置", () => Done(() =>
        {
            versions.SaveInstanceLaunch(key, null);
            editor.Load(versions.ReadInstance(key).Launch);
        }));
    }

    void Modify(FrontInstance instance)
    {
        var body = Page("修改");
        body.Children.Add(new TextBlock { Text = "安装 / 修改模组加载器", FontSize = 18, FontWeight = FontWeights.SemiBold });
        body.Children.Add(new TextBlock { Text = "Minecraft " + instance.Minecraft + " · 当前加载器 " + instance.Loader + "。安装生成新版本，原实例和存档保留，模组不会自动迁移。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
        var loader = Field(body, "选择加载器", new ComboBox { ItemsSource = new[] { "fabric", "quilt", "forge", "neoforge" }, SelectedIndex = 0 });
        var releases = Field(body, "兼容版本", new ComboBox { DisplayMemberPath = "Label" });
        string loadedLoader = "";
        loader.SelectionChanged += (_, _) => { loadedLoader = ""; releases.ItemsSource = null; };
        Button(body, "获取兼容加载器版本", async () =>
        {
            var selected = loader.Text;
            releases.ItemsSource = await launcher.ReadLoaders(instance.Minecraft, selected, lifetime.Token);
            releases.SelectedIndex = releases.Items.Count > 0 ? 0 : -1;
            loadedLoader = selected;
        });
        Button(body, "安装所选加载器", () => Done(() =>
        {
            if (loadedLoader != loader.Text || releases.SelectedItem is not FrontLoader release)
                throw new InvalidOperationException("请先读取当前加载器的兼容版本。");
            if (MessageBox.Show(this, "安装加载器并生成新版本？原实例保留。", "确认安装", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                versions.EnqueueInstanceLoader(key, loader.Text, release.Id);
        }));
        Button(body, "补全游戏文件", () => Done(() =>
        {
            if (MessageBox.Show(this, "将当前实例加入修复队列？", "确认修复", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                versions.EnqueueInstanceRepair(key);
        }));
        body.Children.Add(new TextBlock { Text = "复制为独立实例", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 24, 0, 0) });
        body.Children.Add(new TextBlock { Text = "复制模组、配置、资源和启动设置。游戏主体、加载器和公共库沿用已有安装，原实例不变。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
        var name = Field(body, "新实例名称", new TextBox { Text = instance.Name + " - 副本", MaxLength = 100 });
        var saves = Field(body, "可选内容", new CheckBox { Content = "包含存档（可能很大）" });
        var screenshots = Field(body, "", new CheckBox { Content = "包含截图" });
        var servers = Field(body, "", new CheckBox { Content = "包含服务器列表" });
        Button(body, "开始复制", async () =>
        {
            if (MessageBox.Show(this, "复制为新的独立实例？这会写入游戏目录，原实例保留。", "确认复制", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                return;
            CreatedInstance = await versions.CloneInstance(key, name.Text,
                new(saves.IsChecked == true, screenshots.IsChecked == true, servers.IsChecked == true), lifetime.Token);
            status.Text = "独立实例已复制：" + CreatedInstance + "。关闭版本设置后会刷新并选择新实例。";
        });
    }

    void Management(FrontInstance instance)
    {
        var body = Page("维护");
        if (launcher is not IFrontManagement management)
        {
            body.Children.Add(new TextBlock { Text = "当前宿主未提供维护接口。" });
            return;
        }
        body.Children.Add(new TextBlock { Text = "实例维护", FontSize = 18, FontWeight = FontWeights.SemiBold });
        var updates = new ListBox { SelectionMode = SelectionMode.Multiple, DisplayMemberPath = "Label", MinHeight = 150 };
        body.Children.Add(updates);
        Button(body, "检查内容更新", async () => updates.ItemsSource = await management.CheckContentUpdates(key, lifetime.Token));
        Button(body, "加入选中更新", async () =>
        {
            var ids = updates.SelectedItems.OfType<FrontContentUpdate>().Where(item => item.Latest.Length > 0).Select(item => item.ProjectId).ToArray();
            await management.EnqueueContentUpdates(key, ids, lifetime.Token);
        });
        Button(body, "恢复最近一次内容事务", async () =>
        {
            if (MessageBox.Show(this, "恢复最近一次内容事务？这会更改实例中的资源文件。", "确认恢复", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                await management.RestoreLatestContent(key, lifetime.Token);
        });
        var backups = new ListBox { DisplayMemberPath = "Label", MinHeight = 100 };
        body.Children.Add(new TextBlock { Text = "存档备份", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 18, 0, 6) });
        body.Children.Add(backups);
        Button(body, "读取备份列表", () => Done(() => backups.ItemsSource = management.ReadBackups(key)));
        Button(body, "立即备份所有世界", async () =>
        {
            await management.CreateBackup(key, "Front 手动备份", lifetime.Token);
            backups.ItemsSource = management.ReadBackups(key);
        });
        var worlds = Field(body, "备份中的世界", new ComboBox());
        backups.SelectionChanged += (_, _) =>
        {
            worlds.ItemsSource = (backups.SelectedItem as FrontBackup)?.Worlds;
            worlds.SelectedIndex = worlds.Items.Count > 0 ? 0 : -1;
        };
        Button(body, "将选中世界恢复为新存档", async () =>
        {
            if (backups.SelectedItem is not FrontBackup backup || worlds.SelectedItem is not string world)
                throw new InvalidOperationException("请选择备份及其中的世界。");
            if (MessageBox.Show(this, "将备份恢复为一个新世界？现有存档不会被覆盖。", "恢复存档", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                await management.RestoreBackup(key, backup.Id, world, lifetime.Token);
        });
        var java = new ListBox { DisplayMemberPath = "Label", MinHeight = 100 };
        body.Children.Add(new TextBlock { Text = "Java 检测", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 18, 0, 6) });
        body.Children.Add(java);
        Button(body, "扫描本机 Java", async () => java.ItemsSource = await management.ScanJava(lifetime.Token));
        var globalJava = Field(body, "Java 保存范围", new CheckBox { Content = "保存为全局 Java（不覆盖其他实例的独立设置）" });
        Button(body, "保存所选 Java", async () =>
        {
            if (java.SelectedItem is not FrontJava selected || selected.Error.Length > 0)
                throw new InvalidOperationException("请选择检测成功的 Java。");
            await management.SaveJava(key, selected.Path, globalJava.IsChecked == true, lifetime.Token);
            launchEditor?.Load(versions.ReadInstance(key).Launch);
        });
        PackUpgrade(body, management);
        var shared = Field(body, "危险操作", new CheckBox { Content = "同时删除共享内容（仅在没有其他实例依赖时允许）" });
        Button(body, "删除此实例", async () =>
        {
            var plan = await management.PlanUninstall(key, shared.IsChecked == true, lifetime.Token);
            var details = "删除实例：" + plan.Name + "\n" + (plan.SharedData && !plan.DeleteSharedData ? "共享资源与存档保留。\n" : "实例内容及存档将被删除。\n")
                + "关联的待执行下载将取消。以下路径会被删除，此操作不可撤销：\n" + string.Join("\n", plan.Paths);
            if (MessageBox.Show(this, details, "危险操作", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                return;
            await management.Uninstall(plan, lifetime.Token);
            closeAfterAction = true;
        });
    }

    void PackUpgrade(Panel body, IFrontManagement management)
    {
        body.Children.Add(new TextBlock { Text = "整合包升级副本", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 6) });
        var explanation = new TextBlock { Text = "选择新版整合包，预览冲突后创建升级副本；旧实例保留。勾选的文件沿用旧实例内容。", TextWrapping = TextWrapping.Wrap };
        body.Children.Add(explanation);
        var choices = new StackPanel();
        body.Children.Add(choices);
        var files = new List<(CheckBox Choice, FrontUpgradeFile File)>();
        Button(body, "选择新版整合包并预览", async () =>
        {
            var picker = new OpenFileDialog { Filter = "整合包|*.mrpack;*.zip", CheckFileExists = true };
            if (picker.ShowDialog(this) != true)
                return;
            if (upgradePreviewId != null)
            {
                management.DiscardPackUpgrade(upgradePreviewId);
                upgradePreviewId = null;
            }
            files.Clear();
            choices.Children.Clear();
            var preview = await management.PreparePackUpgrade(key, picker.FileName, lifetime.Token);
            upgradePreviewId = preview.Id;
            explanation.Text = preview.HasBaseline ? "预览就绪。勾选保留的旧文件，冲突项请逐一确认。" : "旧实例缺少基线清单，无法精确识别自定义修改，请仔细检查所有条目。";
            foreach (var file in preview.Files)
            {
                var choice = new CheckBox { Content = (file.Conflict ? "冲突 · " : "") + file.Path + " · " + file.Reason, IsChecked = file.KeepOld, Margin = new Thickness(0, 4, 0, 4) };
                files.Add((choice, file));
                choices.Children.Add(choice);
            }
        });
        Button(body, "确认创建升级副本", async () =>
        {
            if (upgradePreviewId == null)
                throw new InvalidOperationException("请先准备升级预览。");
            if (MessageBox.Show(this, "根据当前文件选择创建升级副本？原实例保留。", "确认升级", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            CreatedInstance = await management.CommitPackUpgrade(key, upgradePreviewId, files.Where(item => item.Choice.IsChecked == true).Select(item => item.File.Path).ToArray(), lifetime.Token);
            upgradePreviewId = null;
            closeAfterAction = true;
        });
    }

    static StackPanel ContentPage(TabControl tabs, string title)
    {
        var body = new StackPanel { Margin = new Thickness(10) };
        tabs.Items.Add(new TabItem { Header = title, Content = body });
        return body;
    }

    void LocalContent(TabControl tabs, string title, string kind)
    {
        var body = ContentPage(tabs, title);
        var list = new ListBox { DisplayMemberPath = "Name", MinHeight = 220, MaxHeight = 340 };
        body.Children.Add(list);
        void Refresh() => list.ItemsSource = launcher.ReadContent(key).Where(item =>
            string.Equals(Path.GetFileName(Path.GetDirectoryName(item.Path)), kind, StringComparison.OrdinalIgnoreCase)).ToArray();
        Button(body, "刷新列表", () => Done(Refresh));
        Button(body, "启用 / 禁用所选资源", () => Done(() =>
        {
            if (list.SelectedItem is not FrontLocalContent item)
                throw new InvalidOperationException("请选择本地资源。");
            launcher.ToggleContent(key, item.Path);
            Refresh();
        }));
        Button(body, "导入本地文件", async () =>
        {
            var picker = new OpenFileDialog { Multiselect = true, Filter = kind == "mods" ? "模组|*.jar" : "资源包|*.zip", CheckFileExists = true };
            if (picker.ShowDialog(this) != true)
                return;
            await versions.ImportContent(key, kind, picker.FileNames, lifetime.Token);
            Refresh();
        });
        Button(body, "打开资源目录", () => Done(() => versions.OpenInstanceDirectory(key, kind)));
        Refresh();
    }

    void Worlds(TabControl tabs)
    {
        var body = ContentPage(tabs, "存档");
        body.Children.Add(new TextBlock { Text = "仅列出含 level.dat 的有效存档；不会自动移动或删除存档。", TextWrapping = TextWrapping.Wrap });
        var list = new ListBox { DisplayMemberPath = "Name", MinHeight = 220 };
        body.Children.Add(list);
        Button(body, "刷新存档", () => Done(() => list.ItemsSource = versions.ReadWorlds(key)));
        Button(body, "打开存档目录", () => Done(() => versions.OpenInstanceDirectory(key, "saves")));
    }

    void Export()
    {
        var body = Page("导出");
        body.Children.Add(new TextBlock { Text = "导出 MultiMC / Prism 兼容 ZIP，不包含账号令牌。", TextWrapping = TextWrapping.Wrap });
        var mods = Field(body, "导出内容", new CheckBox { Content = "包含模组", IsChecked = true });
        var servers = Field(body, "", new CheckBox { Content = "包含服务器列表" });
        var shaders = Field(body, "", new CheckBox { Content = "包含光影", IsChecked = true });
        var resources = Field(body, "", new CheckBox { Content = "包含资源包", IsChecked = true });
        var saves = Field(body, "", new CheckBox { Content = "包含存档（可能含个人信息）" });
        Button(body, "选择位置并导出", async () =>
        {
            var picker = new SaveFileDialog { Filter = "整合包 ZIP|*.zip", FileName = "MCUG-Pack.zip" };
            if (picker.ShowDialog(this) == true)
                await versions.ExportInstance(key, picker.FileName,
                    new(mods.IsChecked == true, servers.IsChecked == true, shaders.IsChecked == true, resources.IsChecked == true, saves.IsChecked == true), lifetime.Token);
        });
        body.Children.Add(new TextBlock { Text = "导出启动脚本", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 24, 0, 0) });
        body.Children.Add(new TextBlock { Text = "使用当前保存的账号类型和配置目录。脚本不保存令牌，启动时由 Front 再次确认；请保持启动器路径有效。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
        Button(body, "导出启动脚本", () => Done(() =>
        {
            var picker = new SaveFileDialog { Filter = "Windows 启动脚本|*.cmd|PowerShell 启动脚本|*.ps1", FileName = "启动游戏.cmd" };
            if (picker.ShowDialog(this) == true)
                versions.ExportLaunchScript(key, picker.FileName);
        }));
    }
}
