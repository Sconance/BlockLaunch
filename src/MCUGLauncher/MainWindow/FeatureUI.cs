using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;

public record InstanceView(string Key, string Name, string Group, string Detail, string Icon, int JavaMajor = 0);

public partial class MainWindow
{
    ListBox instanceList = new();
    readonly TextBox instanceQuery = new()
    {
        ToolTip = "搜索已安装版本的名称、版本号和加载器"
    };
    readonly TextBlock instanceSearchSummary = new()
    {
        FontSize = 11,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 3, 0, 8)
    };

    ComboBox rootList = new();
    StackPanel instanceDetail = new();
    RadioButton instanceNav = null!;

    internal static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 5, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    internal static TextBlock Hint(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.SlateGray,
        Margin = new Thickness(0, 5, 0, 10)
    };

    internal static Button ActionButton(string title, RoutedEventHandler action)
    {
        var b = new Button
        {
            Content = title
        };
        b.Click += action;
        return b;
    }

    Border FeatureCard(UIElement child) => new()
    {
        Style = (Style)FindResource("Card"),
        Child = child,
        Margin = new Thickness(0, 0, 0, 15)
    };

    void BuildFeaturePages()
    {
        BuildResourceFilters();
        BuildCollectionsUI();
        instanceNav = new RadioButton
        {
            Style = (Style)FindResource("Nav"),
            Content = "▣     版本管理",
            GroupName = "Navigation",
            Tag = "7"
        };
        var nav = (StackPanel)ManageNav.Parent;
        nav.Children.Insert(nav.Children.IndexOf(ManageNav) + 1, instanceNav);
        instanceNav.Checked += (_, _) =>
        {
            MainTabs.SelectedIndex = 7;
            PageTitle.Text = "版本管理";
            PageSubtitle.Text = "收藏、分类、个性化与整合包导出。";
            RefreshInstanceViews();
        };
        var panel = new Grid();
        panel.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(300)
        });
        panel.ColumnDefinitions.Add(new());
        var left = new DockPanel
        {
            Margin = new Thickness(0, 0, 18, 0)
        };
        var toolbar = new StackPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        toolbar.Children.Add(Heading("游戏文件夹"));
        rootList.SelectionChanged += async (_, _) =>
        {
            if (busy || rootList.SelectedItem is not string root || root == prefs.Root)
                return;
            await SwitchRoot(root);
        };
        instanceQuery.TextChanged += (_, _) => instanceSearchController.Apply();
        rootList.ToolTip = prefs.Root;
        rootList.SelectionChanged += (_, _) => rootList.ToolTip = rootList.SelectedItem;
        toolbar.Children.Add(rootList);
        toolbar.Children.Add(ActionButton("＋ 添加已有游戏文件夹", AddRootClick));
        toolbar.Children.Add(ActionButton("导入整合包", ImportPackClick));
        toolbar.Children.Add(ActionButton("刷新版本", async (_, _) => await Run(async () =>
        {
            await Refresh();
            RefreshInstanceViews();
        })));
        left.Children.Add(toolbar);
        left.Children.Add(instanceList);
        panel.Children.Add(left);
        var detailScroll = new ScrollViewer
        {
            Content = instanceDetail,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(detailScroll, 1);
        panel.Children.Add(detailScroll);
        instanceList.SelectionChanged += (_, _) => ShowInstanceDetail();
        instanceList.MouseDoubleClick += (_, _) =>
        {
            if (instanceList.SelectedItem is InstanceView item)
                OpenVersionSettings(item.Key);
        };
        var template = new DataTemplate(typeof(InstanceView));
        var row = new FrameworkElementFactory(typeof(StackPanel));
        var image = new FrameworkElementFactory(typeof(Image));
        image.SetValue(Image.WidthProperty, 32.0);
        image.SetValue(Image.HeightProperty, 32.0);
        image.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        image.SetBinding(Image.SourceProperty, new Binding("Icon"));
        row.AppendChild(image);
        var name = new FrameworkElementFactory(typeof(TextBlock));
        name.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        name.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        name.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        row.AppendChild(name);
        var detail = new FrameworkElementFactory(typeof(TextBlock));
        detail.SetBinding(TextBlock.TextProperty, new Binding("Detail"));
        detail.SetValue(TextBlock.FontSizeProperty, 11.0);
        detail.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        row.AppendChild(detail);
        template.VisualTree = row;
        instanceList.ItemTemplate = template;
        var groupTemplate = new DataTemplate();
        var header = new FrameworkElementFactory(typeof(TextBlock));
        header.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        header.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        header.SetValue(TextBlock.MarginProperty, new Thickness(2, 10, 0, 10));
        groupTemplate.VisualTree = header;
        instanceList.GroupStyle.Add(new GroupStyle { HeaderTemplate = groupTemplate });
        MainTabs.Items.Add(new TabItem { Header = "版本管理", Content = panel });
        var installStack = (StackPanel)((ScrollViewer)((TabItem)MainTabs.Items[1]).Content).Content;
        InstallActions.Children.Add(ActionButton("改为修复首页选中的已安装版本", (_, _) =>
        {
            if (busy)
                return;
            downloadVersion = null;
            UpdateTargets();
        }));
        foreach (var loader in new[] {
            "forge",
            "neoforge"
        }

        )
        {
            var button = ActionButton("选择并自动安装 " + (loader == "forge" ? "Forge" : "NeoForge"), loaderController.AutomaticLoaderClick);
            button.Tag = loader;
            LoaderActions.Children.Add(button);
        }

        BuildDownloadPage(installStack);
        var settingsStack = (StackPanel)((ScrollViewer)((TabItem)MainTabs.Items[4]).Content).Content;
        settingsStack.Children.Insert(0, ActionButton("高级启动选项、内存与个性化", GlobalOptionsClick));
        var home = (StackPanel)((ScrollViewer)((TabItem)MainTabs.Items[0]).Content).Content;
        AllowDrop = true;
        PreviewDrop += HandleFileDrop;
        fileDropController.Build();
    }

    async Task HandleScriptLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--root");
        var instance = Array.IndexOf(args, "--instance");
        if (index >= 0 && index + 1 < args.Length && instance >= 0 && instance + 1 < args.Length)
        {
            await SwitchRoot(args[index + 1]);
            Versions.SelectedValue = args[instance + 1];
            var account = Array.IndexOf(args, "--account");
            if (account >= 0 && account + 1 < args.Length)
                AccountMode.SelectedIndex = args[account + 1] switch
                {
                    "microsoft" => 1,
                    "mua" => 2,
                    _ => 0
                };
            if (Versions.SelectedValue != null)
                await Run(Launch, "脚本启动");
            else
                InlineMessageBox.Show("脚本指定的实例不存在，请重新选择。");
        }
    }

    async Task SwitchRoot(string root) => await Run(async () =>
    {
        RequireStopped();
        prefs.Root = GameDefaults.ResolveRoot(root, true);
        GameRoot.Text = prefs.Root;
        if (!prefs.GameRoots.Contains(prefs.Root))
            prefs.GameRoots.Add(prefs.Root);
        settings.Save(prefs);
        SetupLauncher();
        await StartupRecovery();
        RefreshInstanceViews();
    }, "切换游戏目录");

    async void AddRootClick(object s, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog
        {
            Title = "选择 .minecraft 或其外层游戏文件夹（自动识别）"
        };
        if (picker.ShowDialog() == true)
            await SwitchRoot(picker.FolderName);
    }

    void RefreshInstanceViews()
    {
        rootList.ItemsSource = prefs.GameRoots.Append(prefs.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        rootList.SelectedItem = prefs.Root;
        var catalog = new InstanceCatalog(prefs.Root);
        var result = new List<InstanceView>();
        foreach (var key in ProfileKeys())
        {
            try
            {
                var d = catalog.GetOrCreate(key);
                var requirements = RequirementsFor(d.LaunchProfileId);
                var group = d.Favorite ? "★ 收藏夹" : d.Category != "自动" ? d.Category : requirements.Loader == "vanilla" ? "原版游戏" : requirements.Loader.ToUpperInvariant() + " 版本";
                result.Add(new(key, d.DisplayName, group, requirements.Minecraft + " · " + requirements.Loader + " " + requirements.LoaderVersion, File.Exists(d.IconPath) ? d.IconPath : "", JavaDiscovery.RequiredMajor(prefs.Root, d.LaunchProfileId)));
            }
            catch (Exception ex)
            {
                Log("读取版本 " + key + "：" + ex.Message);
            }
        }

        var view = CollectionViewSource.GetDefaultView(result.OrderBy(r => r.Group.StartsWith('★') ? 0 : 1).ThenBy(r => r.Group).ThenBy(r => r.Name).ToList());
        view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        instanceList.ItemsSource = view;
        instanceSearchController.Apply();
        instanceList.SelectedItem = result.FirstOrDefault(r => r.Key == Versions.SelectedValue as string) ?? result.FirstOrDefault();
        if (sketchHome != null)
            ShowSketchVersions();
    }

    PackRequirements RequirementsFor(string profile) => ProfileInspector.Read(prefs.Root, profile);

    void ShowInstanceDetail()
    {
        instanceDetail.Children.Clear();
        if (instanceList.SelectedItem is not InstanceView item)
            return;
        var card = new StackPanel();
        card.Children.Add(Heading(item.Name));
        card.Children.Add(Hint(item.Detail));
        card.Children.Add(Hint("打开版本设置，管理个性化、启动选项、加载器、Mod 和整合包导出。"));
        card.Children.Add(ActionButton("版本设置 →", (_, _) => OpenVersionSettings(item.Key)));
        card.Children.Add(ActionButton("选中并启动", async (_, _) =>
        {
            if (busy)
                return;
            Versions.SelectedValue = item.Key;
            HomeNav.IsChecked = true;
            await Run(Launch);
        }));
        instanceDetail.Children.Add(FeatureCard(card));
    }

    void ExportScript(string key)
    {
        try
        {
            var picker = new SaveFileDialog
            {
                Filter = "Windows 启动脚本|*.cmd|PowerShell 启动脚本|*.ps1",
                FileName = "启动游戏.cmd"
            };
            if (picker.ShowDialog() != true)
                return;
            static string Q(string text) => "'" + text.Replace("'", "''") + "'";
            static string Cmd(string text) => "\"" + text.Replace("%", "%%") + "\"";
            var account = AccountMode.SelectedIndex switch
            {
                1 => "microsoft",
                2 => "mua",
                _ => "offline"
            };
            var command = Path.GetExtension(picker.FileName).Equals(".ps1", StringComparison.OrdinalIgnoreCase) ? "# 账号授权由启动器完成，脚本不保存令牌。\r\n& " + Q(Environment.ProcessPath!) + " --root " + Q(prefs.Root) + " --instance " + Q(key) + " --account " + account + "\r\n" : "@echo off\r\nchcp 65001 >nul\r\nsetlocal DisableDelayedExpansion\r\n" + Cmd(Environment.ProcessPath!) + " --root " + Cmd(prefs.Root) + " --instance " + Cmd(key) + " --account " + account + "\r\n";
            if (Path.GetExtension(picker.FileName).Equals(".ps1", StringComparison.OrdinalIgnoreCase))
                File.WriteAllText(picker.FileName, command, new System.Text.UTF8Encoding(true));
            else
                File.WriteAllText(picker.FileName, command, new System.Text.UTF8Encoding(false));
            InlineMessageBox.Show("脚本已导出；请保持启动器路径有效。微软账号仍需在启动器内授权。");
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    void ExportPackDialog(string key)
    {
        if (busy)
            return;
        var d = new InstanceCatalog(prefs.Root).GetOrCreate(key);
        var body = new StackPanel();
        body.Children.Add(Heading("导出 MultiMC / Prism 兼容整合包"));
        body.Children.Add(Hint("按所选内容打包当前实例，自动生成组件清单。关闭游戏后导出；分享模组本体前请确认分发许可。"));
        CheckBox Check(string title, bool selected)
        {
            var c = new CheckBox
            {
                Content = title,
                IsChecked = selected,
                Margin = new Thickness(0, 9, 0, 9)
            };
            body.Children.Add(c);
            return c;
        }

        var mods = Check("包含模组本体（不勾选时仅导出配置）", true);
        var servers = Check("包含服务器列表 servers.dat", false);
        var shaders = Check("包含光影", true);
        var resources = Check("包含资源包", true);
        var saves = Check("包含存档", false);
        var dialog = FeatureDialog("导出整合包", body);
        body.Children.Add(ActionButton("选择保存位置并导出", async (_, _) =>
        {
            var picker = new SaveFileDialog
            {
                Filter = "整合包 ZIP|*.zip",
                FileName = "BlockLaunch-Pack.zip"
            };
            if (picker.ShowDialog() != true)
                return;
            var options = new ExportOptions(mods.IsChecked == true, servers.IsChecked == true, shaders.IsChecked == true, resources.IsChecked == true, saves.IsChecked == true);
            dialog.Close();
            await Run(async () =>
            {
                RequireStopped();
                var requirements = RequirementsFor(d.LaunchProfileId);
                var directory = (d.Launch ?? prefs.Launch).Isolation == "共享根目录" ? prefs.Root : new InstanceCatalog(prefs.Root).DirectoryFor(key);
                await InstanceManagement.Export(directory, picker.FileName, d.DisplayName, requirements, options, CurrentToken);
                Log("整合包已导出：" + picker.FileName);
            }, "导出整合包");
        }));
        dialog.ShowDialog();
    }

    internal InlineDialog FeatureDialog(string title, StackPanel content)
    {
        var window = new InlineDialog
        {
            Owner = this,
            Title = title,
            Width = 700,
            Height = 700,
            MinWidth = 540,
            MinHeight = 300,
            SizeToContent = SizeToContent.Height,
            Background = new SolidColorBrush(Color.FromRgb(243, 247, 249)),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new ScrollViewer
            {
                Content = content,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(16)
            }
        };
        window.PrepareContent = () =>
        {
            content.MaxWidth = 920;
            if (content.Children.OfType<TextBlock>().FirstOrDefault() is
                {
                } heading && heading.Text == title)
                heading.Visibility = Visibility.Collapsed;
            for (var i = 0; i < content.Children.Count; i++)
                if (content.Children[i] is Button)
                {
                    var actions = new WrapPanel
                    {
                        Margin = new Thickness(0, 6, 0, 0)
                    };
                    while (i < content.Children.Count && content.Children[i] is Button button)
                    {
                        content.Children.RemoveAt(i);
                        button.MinWidth = Math.Max(button.MinWidth, 145);
                        button.HorizontalAlignment = HorizontalAlignment.Left;
                        actions.Children.Add(button);
                    }

                    content.Children.Insert(i, actions);
                }
        };
        ConfigureUtilityWindow(window);
        return window;
    }

    void GlobalOptionsClick(object s, RoutedEventArgs e)
    {
        if (!busy)
            EditLaunchOptions(null);
    }

    void EditLaunchOptions(InstanceDescriptor? descriptor)
    {
        if (busy)
            return;
        var original = descriptor?.Launch ?? prefs.Launch;
        var value = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(original))!;
        var body = new StackPanel();
        body.Children.Add(Heading(descriptor == null ? "全局启动与个性化" : "版本设置 · " + descriptor.DisplayName));
        body.Children.Add(Hint("版本独立设置保存后覆盖全局启动选项。共享根目录会共享 mods/config/saves；独立实例为推荐默认值。"));
        var options = new LaunchOptionsForm(value);
        body.Children.Add(options.Panel);
        var dialog = FeatureDialog("启动设置", body);
        if (descriptor == null)
            AddPersonalization(body);
        else
            body.Children.Add(ActionButton("恢复跟随全局设置", (_, _) =>
            {
                try
                {
                    RequireStopped();
                    descriptor.Launch = null;
                    new InstanceCatalog(prefs.Root).Save(descriptor);
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            }));
        body.Children.Add(ActionButton("保存设置", (_, _) =>
        {
            try
            {
                RequireStopped();
                var updated = options.Read();
                updated.Validate();
                if (descriptor != null)
                {
                    descriptor.Launch = updated;
                    new InstanceCatalog(prefs.Root).Save(descriptor);
                }
                else
                {
                    prefs.Launch = updated;
                    prefs.Ram = updated.RamMb;
                    prefs.Java = updated.Java;
                    prefs.Width = updated.Width;
                    prefs.Height = updated.Height;
                    Memory.Text = prefs.Ram.ToString();
                    JavaPath.Text = prefs.Java;
                    GameWidth.Text = prefs.Width.ToString();
                    GameHeight.Text = prefs.Height.ToString();
                    settings.Save(prefs);
                }

                dialog.Close();
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message, "设置未保存");
            }
        }));
        dialog.ShowDialog();
    }

    void AddPersonalization(StackPanel body)
    {
        body.Children.Add(Heading("个性化"));
        body.Children.Add(ActionButton("背景填充、透明度与主题", (_, _) => OpenAppearance()));
        var colors = new ComboBox
        {
            ItemsSource = new[] {
                "地大蓝",
                "青绿",
                "石墨",
                "紫罗兰"
            },
            SelectedItem = "地大蓝"
        };
        body.Children.Add(colors);
        body.Children.Add(ActionButton("应用主题色", (_, _) =>
        {
            prefs.Accent = colors.SelectedItem as string ?? "蓝色";
            settings.Save(prefs);
            ApplyPersonalization();
        }));
        body.Children.Add(ActionButton("选择自定义背景", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "背景图片|*.png;*.jpg;*.jpeg"
            };
            if (picker.ShowDialog() != true)
                return;
            prefs.BackgroundPath = picker.FileName;
            settings.Save(prefs);
            ApplyPersonalization();
        }));
        body.Children.Add(ActionButton("恢复默认背景", (_, _) =>
        {
            prefs.BackgroundPath = "";
            settings.Save(prefs);
            ApplyPersonalization();
        }));
        body.Children.Add(Heading("离线皮肤"));
        body.Children.Add(Hint("本地 PNG 通过资源包覆盖默认玩家皮肤，需要在游戏内启用“BlockLaunch Offline Skin”。只影响本机显示，不提供服务器皮肤认证，其他使用默认皮肤的玩家也可能显示相同贴图。"));
        body.Children.Add(ActionButton("选择离线皮肤 PNG", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "Minecraft 皮肤|*.png"
            };
            if (picker.ShowDialog() != true)
                return;
            try
            {
                var image = new BitmapImage(new Uri(picker.FileName));
                if (image.PixelWidth != 64 || image.PixelHeight is not (32 or 64))
                    throw new IOException("请选择 64×64 或 64×32 皮肤。");
                var file = Path.Combine(settingsDir, "offline-skin.png");
                File.Copy(picker.FileName, file, true);
                prefs.OfflineSkin = file;
                settings.Save(prefs);
                ApplyPersonalization();
                InlineMessageBox.Show("离线启动时会生成皮肤资源包，请在游戏内启用。");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }
        }));
        body.Children.Add(ActionButton("停止生成离线皮肤资源包", (_, _) =>
        {
            prefs.OfflineSkin = "";
            settings.Save(prefs);
            ApplyPersonalization();
            InlineMessageBox.Show("已停止生成；已有资源包可在游戏内停用。");
        }));
    }

    void ApplyPersonalization()
    {
        var accent = AccentColor(prefs.Accent);
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
        ApplyBackground();
        avatarController.Refresh();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool SetWindowText(IntPtr handle, string title);
    [DllImport("psapi.dll")]
    static extern bool EmptyWorkingSet(IntPtr handle);

    async void ApplyLaunchBehavior(Process process, LaunchSettings settings, GameSession session)
    {
        try
        {
            process.PriorityClass = Enum.Parse<ProcessPriorityClass>(settings.Priority);
            if (settings.TrimLauncher)
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            if (settings.Visibility == "启动后最小化")
                WindowState = WindowState.Minimized;
            if (settings.Visibility == "启动后隐藏")
                Hide();
            if (settings.WindowTitle.Length > 0)
                for (var i = 0; i < 60 && !process.HasExited; i++)
                {
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        SetWindowText(process.MainWindowHandle, settings.WindowTitle);
                        break;
                    }

                    await Task.Delay(500);
                }

            await session.Completion;
        }
        catch (Exception ex)
        {
            Log("启动外观设置：" + ex.Message);
        }
        finally
        {
            if (!Dispatcher.HasShutdownStarted && settings.Visibility == "启动后隐藏")
            {
                Show();
                WindowState = WindowState.Normal;
            }
        }
    }

    void PrepareOfflineSkin(string directory, string baseVersion)
    {
        if (AccountMode.SelectedIndex != 0 || !File.Exists(prefs.OfflineSkin))
            return;
        var packDir = Path.Combine(directory, "resourcepacks");
        Directory.CreateDirectory(packDir);
        var file = Path.Combine(packDir, "BlockLaunch Offline Skin.zip");
        var format = 15;
        var jar = Path.Combine(prefs.Root, "versions", baseVersion, baseVersion + ".jar");
        if (File.Exists(jar))
        {
            using var gameJar = ZipFile.OpenRead(jar);
            var version = gameJar.GetEntry("version.json");
            if (version != null)
            {
                using var doc = JsonDocument.Parse(version.Open());
                if (doc.RootElement.TryGetProperty("pack_version", out var p) && p.TryGetProperty("resource", out var r))
                {
                    if (r.ValueKind == JsonValueKind.Number)
                        format = r.GetInt32();
                    else if (r.TryGetProperty("major", out var major))
                        format = major.GetInt32();
                }
            }
        }

        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var archive = ZipFile.Open(temp, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("pack.mcmeta").Open()))
                writer.Write(JsonSerializer.Serialize(new
                {
                    pack = new
                    {
                        pack_format = format,
                        description = "BlockLaunch Offline Skin (local default-player replacement)"
                    }
                }));
            var paths = new List<string>
            {
                "assets/minecraft/textures/entity/steve.png",
                "assets/minecraft/textures/entity/alex.png"
            };
            foreach (var model in new[] {
                "wide",
                "slim"
            }

            )
                foreach (var name in new[] {
                    "steve",
                    "alex",
                    "ari",
                    "efe",
                    "kai",
                    "makena",
                    "noor",
                    "sunny",
                    "zuri"
                }

                )
                    paths.Add("assets/minecraft/textures/entity/player/" + model + "/" + name + ".png");
            foreach (var path in paths)
                archive.CreateEntryFromFile(prefs.OfflineSkin, path);
        }

        File.Move(temp, file, true);
    }
}

public sealed class LaunchOptionsForm
{
    public StackPanel Panel { get; } = new();

    StackPanel active = new();

    void Section(string title)
    {
        active = new StackPanel();
        active.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
        Panel.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(9), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 14), Child = active });
    }

    public void SetJava(string path) => texts["Java"].Text = path;

    readonly Dictionary<string, TextBox> texts = new();
    readonly Dictionary<string, ComboBox> combos = new();
    readonly Dictionary<string, CheckBox> checks = new();

    void Text(string id, string label, string value)
    {
        active.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 0) });
        var text = new TextBox
        {
            Text = value
        };
        texts[id] = text;
        active.Children.Add(text);
    }

    void Combo(string id, string label, string value, string[] values)
    {
        active.Children.Add(new TextBlock { Text = label });
        var combo = new ComboBox
        {
            ItemsSource = values,
            SelectedItem = value
        };
        combos[id] = combo;
        active.Children.Add(combo);
    }

    void Check(string id, string label, bool value)
    {
        var c = new CheckBox
        {
            Content = label,
            IsChecked = value,
            Margin = new Thickness(0, 8, 0, 12)
        };
        checks[id] = c;
        active.Children.Add(c);
    }

    public LaunchOptionsForm(LaunchSettings settings)
    {
        Section("启动选项");
        Combo("Isolation", "版本隔离", settings.Isolation, ["独立实例", "共享根目录"]);
        Text("WindowTitle", "游戏窗口标题（留空使用默认）", settings.WindowTitle);
        Text("VersionInfo", "自定义版本信息", settings.VersionInfo);
        Combo("Visibility", "启动器可见性", settings.Visibility, ["保持显示", "启动后最小化", "启动后隐藏"]);
        Combo("Priority", "进程优先级", settings.Priority, ["BelowNormal", "Normal", "AboveNormal", "High"]);
        Check("FullScreen", "全屏启动", settings.FullScreen);
        Text("Width", "窗口宽度", settings.Width.ToString());
        Text("Height", "窗口高度", settings.Height.ToString());
        active.Children.Add(new TextBlock { Text = "常用分辨率（可继续修改宽高）", Margin = new Thickness(0, 6, 0, 0) });
        var resolutions = new ComboBox
        {
            ItemsSource = new[] {
                "选择预设…",
                "854 × 480",
                "1280 × 720",
                "1600 × 900",
                "1920 × 1080",
                "2560 × 1440",
                "3840 × 2160"
            },
            SelectedIndex = 0
        };
        resolutions.SelectionChanged += (_, _) =>
        {
            if (resolutions.SelectedIndex > 0)
            {
                var parts = ((string)resolutions.SelectedItem).Split('×');
                texts["Width"].Text = parts[0].Trim();
                texts["Height"].Text = parts[1].Trim();
            }
        };
        active.Children.Add(resolutions);
        Section("内存分配");
        Check("AutoMemory", "自动分配内存（按可用内存推荐）", settings.AutoMemory);
        Text("RamMb", "自定义内存 MB（关闭自动分配后生效）", settings.RamMb.ToString());
        var memory = MemoryPolicy.Read();
        active.Children.Add(new TextBlock { Text = $"系统已用 / 总计：{(memory.Total - memory.Available) / 1073741824.0:F1} / {memory.Total / 1073741824.0:F1} GB · 推荐 {MemoryPolicy.Recommend()} MB", TextWrapping = TextWrapping.Wrap });
        Section("Java 与高级参数");
        Check("TrimLauncher", "启动后收缩启动器工作集（不清理其他进程）", settings.TrimLauncher);
        Text("Java", "Java 路径（留空自动）", settings.Java);
        Text("JvmArguments", "额外 JVM 参数", settings.JvmArguments);
        Text("Server", "启动后连接服务器（可选 host:port）", settings.Server);
    }

    public LaunchSettings Read() => new()
    {
        Isolation = (string)combos["Isolation"].SelectedItem,
        WindowTitle = texts["WindowTitle"].Text,
        VersionInfo = texts["VersionInfo"].Text,
        Visibility = (string)combos["Visibility"].SelectedItem,
        Priority = (string)combos["Priority"].SelectedItem,
        FullScreen = checks["FullScreen"].IsChecked == true,
        Width = int.Parse(texts["Width"].Text),
        Height = int.Parse(texts["Height"].Text),
        RamMb = int.Parse(texts["RamMb"].Text),
        AutoMemory = checks["AutoMemory"].IsChecked == true,
        TrimLauncher = checks["TrimLauncher"].IsChecked == true,
        Java = texts["Java"].Text.Trim().Trim('"'),
        JvmArguments = texts["JvmArguments"].Text,
        Server = texts["Server"].Text.Trim()
    };
}
