using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;

namespace BlockLaunch;

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
        fileImportController.Build();
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

    internal async Task SwitchRoot(string root) => await Run(async () =>
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

    internal void RefreshInstanceViews()
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

    internal PackRequirements RequirementsFor(string profile) => ProfileInspector.Read(prefs.Root, profile);

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

    internal void ExportScript(string key)
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
            var account = AccountMode.SelectedIndex switch
            {
                1 => "microsoft",
                2 => "mua",
                _ => "offline"
            };
            var powershell = Path.GetExtension(picker.FileName).Equals(".ps1", StringComparison.OrdinalIgnoreCase);
            var command = LaunchScriptBuilder.Build(Environment.ProcessPath!, prefs.Root, key, account, powershell);
            if (powershell)
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
        => new InstancePackExportDialog(this).Show(key);

    void GlobalOptionsClick(object s, RoutedEventArgs e)
    {
        EditLaunchOptions(null);
    }

    void EditLaunchOptions(InstanceDescriptor? descriptor) => launchOptionsDialog.Show(descriptor);

    void ApplyPersonalization()
    {
        var accent = AccentColor(prefs.Accent);
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
        ApplyBackground();
        playerIdentityController.Refresh();
    }

    void PrepareOfflineSkin(string directory, string baseVersion)
    {
        OfflineSkinPackBuilder.Prepare(prefs.Root, directory, prefs.OfflineSkin, baseVersion, AccountMode.SelectedIndex == 0);
    }
}
