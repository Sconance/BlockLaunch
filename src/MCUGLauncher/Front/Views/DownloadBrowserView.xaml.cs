using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlockLaunch.Contracts;
using Microsoft.Win32;

namespace BlockLaunch.Front;

public partial class DownloadBrowserView : UserControl, IDisposable
{
    static readonly HashSet<string> AprilVersions = new(StringComparer.OrdinalIgnoreCase)
    {
        "15w14a", "1.RV-Pre1", "3D Shareware v1.34", "20w14infinite", "22w13oneblockatatime",
        "23w13a_or_b", "24w14potato", "25w14craftmine", "2.0"
    };
    readonly IFrontLauncher launcher;
    readonly ResourceBrowserView browser;
    readonly ResourceBrowserViewModel resources;
    readonly CancellationTokenSource lifetime = new();
    readonly Dictionary<string, (ComboBox Versions, Button Install, TextBlock State)> loaderRows = new();
    IReadOnlyList<FrontProfile> profiles = [];
    CancellationTokenSource? loaderCancellation;
    bool selecting;
    bool busy;
    bool disposed;
    bool initialized;
    string selectedVersion = "";
    public string CurrentCategory { get; private set; } = "原版游戏";

    public DownloadBrowserView(IFrontLauncher launcher, ResourceBrowserView browser, ResourceBrowserViewModel resources)
    {
        this.launcher = launcher;
        this.browser = browser;
        this.resources = resources;
        InitializeComponent();
        BuildCategories();
        BuildLoaderRows();
        RenderVanilla();
        Loaded += async (_, _) =>
        {
            if (!initialized)
                await Refresh();
        };
        Unloaded += (_, _) => loaderCancellation?.Cancel();
    }

    void BuildCategories()
    {
        foreach (var (name, glyph) in new[]
        {
            ("原版游戏", "▣"), ("Mod", "✚"), ("整合包", "▦"), ("数据包", "≡"),
            ("资源包", "▧"), ("光影包", "☼"), ("本地整合包", "⇩"), ("模组加载器", "⚙")
        })
        {
            var visual = new Grid();
            visual.ColumnDefinitions.Add(new() { Width = new GridLength(28) });
            visual.ColumnDefinitions.Add(new());
            visual.Children.Add(new TextBlock { Text = glyph, FontSize = 19, VerticalAlignment = VerticalAlignment.Center });
            var label = new TextBlock
            {
                Text = name == "Mod" ? "模组" : name,
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(label, 1);
            visual.Children.Add(label);
            var button = new RadioButton
            {
                Style = (Style)FindResource("DownloadCategory"), Content = visual,
                GroupName = "DownloadCategory", Tag = name, ToolTip = name
            };
            button.Checked += CategoryChanged;
            Categories.Children.Add(button);
        }
    }

    public void DetachContent()
    {
        loaderCancellation?.Cancel();
        CategoryHost.Content = null;
    }

    public void ShowCategory(string category)
    {
        if (disposed)
            return;
        CurrentCategory = category;
        selecting = true;
        try
        {
            foreach (var button in Categories.Children.OfType<RadioButton>())
                button.IsChecked = Equals(button.Tag, category);
        }
        finally { selecting = false; }
        loaderCancellation?.Cancel();
        CategoryHost.Content = null;
        PackPage.Visibility = category == "本地整合包" ? Visibility.Visible : Visibility.Collapsed;
        LoaderPage.Visibility = category == "模组加载器" ? Visibility.Visible : Visibility.Collapsed;
        CategoryHost.Visibility = category is "本地整合包" or "模组加载器" ? Visibility.Collapsed : Visibility.Visible;
        if (category == "原版游戏")
        {
            CategoryHost.Content = VanillaPage;
            RenderVanilla();
        }
        else if (category == "模组加载器")
            PrepareLoaders();
        else if (category != "本地整合包")
        {
            resources.Kind = category switch
            {
                "整合包" => "modpack", "数据包" => "datapack", "资源包" => "resourcepack",
                "光影包" => "shader", _ => "mod"
            };
            browser.ShowDownloadCategory(category);
            CategoryHost.Content = browser;
            browser.ShowResults();
        }
    }

    async Task Run(Func<Task> action)
    {
        if (busy || disposed)
            return;
        busy = true;
        try
        {
            DownloadStatus.Text = "正在执行…";
            await action();
            if (!disposed && DownloadStatus.Text == "正在执行…")
                DownloadStatus.Text = launcher.Status;
        }
        catch (OperationCanceledException) { if (!disposed) DownloadStatus.Text = "操作已取消。"; }
        catch (Exception exception) { if (!disposed) DownloadStatus.Text = "操作失败：" + exception.Message; }
        finally { busy = false; }
    }

    Task Refresh() => Run(async () =>
    {
        var result = await launcher.ReadProfiles(lifetime.Token);
        if (disposed)
            return;
        profiles = result.Items;
        initialized = true;
        selectedVersion = profiles.FirstOrDefault(item => item.Id == selectedVersion && IsRemote(item))?.Id
            ?? profiles.FirstOrDefault(item => IsType(item, "release") && !AprilVersions.Contains(item.Id))?.Id ?? "";
        RenderVanilla();
        if (CurrentCategory == "模组加载器")
            PrepareLoaders();
        DownloadStatus.Text = result.Warning;
    });

    static bool IsType(FrontProfile profile, string type) => profile.Type.Equals(type, StringComparison.OrdinalIgnoreCase);
    static bool IsRemote(FrontProfile profile) => IsType(profile, "release") || IsType(profile, "snapshot")
        || profile.Type.Contains("old", StringComparison.OrdinalIgnoreCase);

    void RenderVanilla()
    {
        var expanded = VanillaGroups.Children.OfType<Border>().Select(card => card.Child).OfType<Expander>()
            .Where(group => group.IsExpanded).Select(group => group.Tag as string).ToHashSet();
        VanillaGroups.Children.Clear();
        var remote = profiles.Where(IsRemote).ToArray();
        var query = VanillaQuery.Text.Trim();
        var matching = remote.Where(item => item.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        var selected = remote.FirstOrDefault(item => item.Id == selectedVersion);
        InstallTarget.Text = selected == null ? "请选择游戏版本" : "安装目标：" + selected.Id;
        InstallVanilla.IsEnabled = selected != null;
        InstallVanilla.Content = selected?.Installed == true ? "修复游戏" : "下载游戏";
        if (matching.Length == 0)
        {
            VanillaGroups.Children.Add(new TextBlock
            {
                Text = remote.Length == 0 ? "版本列表尚未加载。可刷新列表，或检查下载与网络设置。" : "没有匹配的游戏版本。",
                Style = (Style)FindResource("Caption"), TextWrapping = TextWrapping.Wrap
            });
            if (remote.Length == 0)
            {
                var refresh = new Button { Content = "刷新版本列表", HorizontalAlignment = HorizontalAlignment.Left };
                refresh.Click += RefreshClick;
                VanillaGroups.Children.Add(refresh);
            }
        }
        void Group(string title, IEnumerable<FrontProfile> values, bool latest = false)
        {
            var versions = values.ToArray();
            var items = new StackPanel();
            var populated = false;
            void Populate()
            {
                if (populated)
                    return;
                populated = true;
                foreach (var version in versions)
                    items.Children.Add(VersionButton(version));
                if (versions.Length == 0)
                    items.Children.Add(new TextBlock { Text = "没有匹配版本", Style = (Style)FindResource("Caption") });
            }
            var group = new Expander
            {
                Header = title + (latest ? "" : "（" + versions.Length + "）"), Tag = title, Content = items,
                IsExpanded = latest || expanded.Contains(title) || query.Length > 0,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            group.Expanded += (_, _) => Populate();
            if (group.IsExpanded)
                Populate();
            VanillaGroups.Children.Add(new Border { Style = (Style)FindResource("DownloadCard"), Child = group });
        }
        Group("最新版本", new[]
        {
            matching.FirstOrDefault(item => IsType(item, "release") && !AprilVersions.Contains(item.Id)),
            matching.FirstOrDefault(item => IsType(item, "snapshot") && !AprilVersions.Contains(item.Id))
        }.OfType<FrontProfile>().DistinctBy(item => item.Id), true);
        Group("正式版", matching.Where(item => IsType(item, "release") && !AprilVersions.Contains(item.Id)));
        Group("预览版", matching.Where(item => IsType(item, "snapshot") && !AprilVersions.Contains(item.Id)));
        Group("远古版", matching.Where(item => item.Type.Contains("old", StringComparison.OrdinalIgnoreCase) && !AprilVersions.Contains(item.Id)));
        Group("愚人节版", matching.Where(item => AprilVersions.Contains(item.Id)));
    }

    Button VersionButton(FrontProfile version)
    {
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = version.Id, FontSize = 14, FontWeight = FontWeights.SemiBold });
        var kind = AprilVersions.Contains(version.Id) ? "愚人节版" : IsType(version, "release") ? "正式版" : IsType(version, "snapshot") ? "预览版" : "远古版";
        text.Children.Add(new TextBlock
        {
            Text = kind + " · " + (version.Published is { } published ? "发布于 " + published.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : "发布时间未知")
                + (version.Installed ? " · 已安装" : ""),
            Foreground = Brushes.SlateGray, FontSize = 11, Margin = new Thickness(0, 4, 0, 0)
        });
        var visual = new DockPanel();
        visual.Children.Add(new Border
        {
            Width = 34, Height = 34, CornerRadius = new CornerRadius(7), Margin = new Thickness(0, 0, 13, 0),
            Background = new SolidColorBrush(IsType(version, "release") ? Color.FromRgb(221, 238, 224) : Color.FromRgb(255, 235, 211)),
            Child = new TextBlock { Text = "◇", FontSize = 25, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        });
        visual.Children.Add(text);
        var button = new Button { Style = (Style)FindResource("VersionDownloadRow"), Content = visual, ToolTip = "选择 " + version.Id };
        if (selectedVersion == version.Id)
            button.SetResourceReference(BackgroundProperty, "ThemeSelectedBrush");
        button.Click += (_, _) =>
        {
            if (busy)
                return;
            selectedVersion = version.Id;
            RenderVanilla();
        };
        return button;
    }

    void BuildLoaderRows()
    {
        foreach (var (kind, title) in new[] { ("forge", "Forge"), ("neoforge", "NeoForge"), ("fabric", "Fabric"), ("quilt", "Quilt") })
        {
            var row = new Grid();
            foreach (var width in new[] { new GridLength(52), new GridLength(1, GridUnitType.Star), new GridLength(170), new GridLength(82) })
                row.ColumnDefinitions.Add(new() { Width = width });
            var icon = new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 10, 0),
                Child = new TextBlock { Text = title[..1], FontSize = 24, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            icon.SetResourceReference(Border.BackgroundProperty, "ThemeInputBrush");
            row.Children.Add(icon);
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 15 });
            var state = new TextBlock { Text = "请选择 Minecraft 版本", Style = (Style)FindResource("Caption"), FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 8, 0) };
            text.Children.Add(state);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            var versions = new ComboBox { IsEnabled = false, DisplayMemberPath = "Label", Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(versions, 2);
            row.Children.Add(versions);
            var install = new Button { Content = "安装", Style = (Style)FindResource("Primary"), IsEnabled = false, Margin = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
            install.Click += async (_, _) => await Run(() =>
            {
                if (LoaderMinecraft.SelectedItem is FrontProfile minecraft && versions.SelectedItem is FrontLoader release
                    && Confirm("安装 " + title + " " + release.Id + " · Minecraft " + minecraft.Id + "？"))
                    launcher.EnqueueGame(minecraft.Id, kind, release.Id);
                return Task.CompletedTask;
            });
            Grid.SetColumn(install, 3);
            row.Children.Add(install);
            loaderRows[kind] = (versions, install, state);
            LoaderRows.Children.Add(new Border { Style = (Style)FindResource("DownloadCard"), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8), Child = row });
        }
    }

    void PrepareLoaders()
    {
        var previous = (LoaderMinecraft.SelectedItem as FrontProfile)?.Id;
        var available = profiles.Where(item => IsType(item, "release") || IsType(item, "snapshot")).ToArray();
        selecting = true;
        try
        {
            LoaderMinecraft.ItemsSource = available;
            LoaderMinecraft.SelectedItem = available.FirstOrDefault(item => item.Id == selectedVersion)
                ?? available.FirstOrDefault(item => item.Id == previous) ?? available.FirstOrDefault();
        }
        finally { selecting = false; }
        _ = ReadLoaders();
    }

    async Task ReadLoaders()
    {
        loaderCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        cancellation.CancelAfter(TimeSpan.FromSeconds(30));
        loaderCancellation = cancellation;
        var minecraft = LoaderMinecraft.SelectedItem as FrontProfile;
        foreach (var controls in loaderRows.Values)
        {
            controls.Versions.ItemsSource = null;
            controls.Versions.IsEnabled = controls.Install.IsEnabled = false;
            controls.State.Text = minecraft == null ? "请选择 Minecraft 版本" : "正在读取兼容版本…";
            controls.State.ToolTip = null;
        }
        try
        {
            if (minecraft == null)
                return;
            await Task.WhenAll(loaderRows.Select(async pair =>
            {
                var controls = pair.Value;
                try
                {
                    var releases = await launcher.ReadLoaders(minecraft.Id, pair.Key, cancellation.Token);
                    if (disposed || cancellation.IsCancellationRequested)
                        return;
                    controls.Versions.ItemsSource = releases;
                    controls.Versions.SelectedIndex = releases.Count > 0 ? 0 : -1;
                    controls.Versions.IsEnabled = controls.Install.IsEnabled = releases.Count > 0;
                    controls.State.Text = releases.Count > 0 ? $"{releases.Count} 个可用版本 · Minecraft {minecraft.Id}" : "没有兼容版本";
                }
                catch (OperationCanceledException)
                {
                    if (!disposed && ReferenceEquals(loaderCancellation, cancellation))
                        controls.State.Text = "读取已取消或超时，重新选择游戏版本可重试";
                }
                catch (Exception exception)
                {
                    if (!disposed && !cancellation.IsCancellationRequested)
                    {
                        controls.State.Text = "读取失败，重新选择游戏版本可重试";
                        controls.State.ToolTip = exception.Message;
                    }
                }
            }));
        }
        finally
        {
            if (ReferenceEquals(loaderCancellation, cancellation))
                loaderCancellation = null;
            cancellation.Dispose();
        }
    }

    bool Confirm(string text) => MessageBox.Show(Window.GetWindow(this), text, "确认操作", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
    async void RefreshClick(object sender, RoutedEventArgs args) => await Refresh();
    void VanillaQueryChanged(object sender, TextChangedEventArgs args) { if (VanillaGroups != null) RenderVanilla(); }
    void CategoryChanged(object sender, RoutedEventArgs args) { if (!selecting && sender is RadioButton { Tag: string category }) ShowCategory(category); }
    async void LoaderMinecraftChanged(object sender, SelectionChangedEventArgs args) { if (!selecting) await ReadLoaders(); }
    async void InstallVanillaClick(object sender, RoutedEventArgs args) => await Run(() =>
    {
        if (selectedVersion.Length > 0 && Confirm("下载 / 修复 " + selectedVersion + "？将加入安装队列。"))
            launcher.EnqueueGame(selectedVersion, "vanilla", "");
        return Task.CompletedTask;
    });
    async void ImportPackClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        var picker = new OpenFileDialog { Filter = "整合包 (*.mrpack;*.zip)|*.mrpack;*.zip", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) == true && Confirm("导入 " + Path.GetFileName(picker.FileName) + "？将创建独立实例。"))
            await launcher.ImportPack(picker.FileName, lifetime.Token, PackOptional.IsChecked == true);
    });
    async void OfficialInstallerClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        var picker = new OpenFileDialog { Filter = "官方 Forge / NeoForge 安装器|*.jar", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) != true || !Confirm("运行官方安装器 " + Path.GetFileName(picker.FileName) + "？请仅使用可信官方网站下载的文件。"))
            return;
        await launcher.RunOfficialInstaller(picker.FileName, lifetime.Token);
        if (disposed)
            return;
        var result = await launcher.ReadProfiles(lifetime.Token);
        if (!disposed)
        {
            profiles = result.Items;
            RenderVanilla();
        }
    });

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        loaderCancellation?.Cancel();
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
