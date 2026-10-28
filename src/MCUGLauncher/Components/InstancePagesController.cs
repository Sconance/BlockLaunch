using static BlockLaunch.FeatureUiElements;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class InstancePagesController
{
    readonly LauncherWorkspace workspace;
    internal InstancePagesController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
        instanceSearch = new InstanceSearchController(instanceList, instanceQuery, instanceSearchSummary);
    }

    readonly InstanceSearchController instanceSearch;
    internal event Action? InstanceViewsChanged;

    internal ListBox instanceList = new();
    internal readonly TextBox instanceQuery = new()
    {
        ToolTip = "搜索已安装版本的名称、版本号和加载器"
    };
    internal readonly TextBlock instanceSearchSummary = new()
    {
        FontSize = 11,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 3, 0, 8)
    };
    internal void FilterInstanceViews() => instanceSearch.Apply();

    internal ComboBox rootList = new();
    internal StackPanel instanceDetail = new();
    internal RadioButton instanceNav = null!;

    internal Border FeatureCard(UIElement child) => new()
    {
        Style = (Style)workspace.Window.FindResource("Card"),
        Child = child,
        Margin = new Thickness(0, 0, 0, 15)
    };
    internal void BuildFeaturePages()
    {
        workspace.ResourceFiltersController.BuildResourceFilters();
        workspace.CollectionsController.BuildCollectionsUI();
        instanceNav = new RadioButton
        {
            Style = (Style)workspace.Window.FindResource("Nav"),
            Content = "▣     版本管理",
            GroupName = "Navigation",
            Tag = "7"
        };
        var nav = (StackPanel)workspace.Controls.ManageNav.Parent;
        nav.Children.Insert(nav.Children.IndexOf(workspace.Controls.ManageNav) + 1, instanceNav);
        instanceNav.Checked += (_, _) =>
        {
            workspace.Window.MainTabs.SelectedIndex = 7;
            workspace.Controls.PageTitle.Text = "版本管理";
            workspace.Controls.PageSubtitle.Text = "收藏、分类、个性化与整合包导出。";
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
            if (workspace.LaunchCoordinator.busy || rootList.SelectedItem is not string root || root == workspace.LaunchCoordinator.prefs.Root)
                return;
            await SwitchRoot(root);
        };
        instanceQuery.TextChanged += (_, _) => FilterInstanceViews();
        rootList.ToolTip = workspace.LaunchCoordinator.prefs.Root;
        rootList.SelectionChanged += (_, _) => rootList.ToolTip = rootList.SelectedItem;
        toolbar.Children.Add(rootList);
        toolbar.Children.Add(ActionButton("＋ 添加已有游戏文件夹", AddRootClick));
        toolbar.Children.Add(ActionButton("导入整合包", workspace.PackController.ImportPackClick));
        toolbar.Children.Add(ActionButton("刷新版本", async (_, _) => await workspace.LaunchCoordinator.Run(async () =>
        {
            await workspace.LaunchCoordinator.Refresh();
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
                workspace.VersionSettingsController.OpenVersionSettings(item.Key);
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
        workspace.Window.MainTabs.Items.Add(new TabItem { Header = "版本管理", Content = panel });
        var installStack = (StackPanel)((ScrollViewer)((TabItem)workspace.Window.MainTabs.Items[1]).Content).Content;
        workspace.Controls.InstallActions.Children.Add(ActionButton("改为修复首页选中的已安装版本", (_, _) =>
        {
            if (workspace.LaunchCoordinator.busy)
                return;
            workspace.LaunchCoordinator.downloadVersion = null;
            workspace.ContentController.UpdateTargets();
        }));
        foreach (var loader in new[]
        {
            "forge",
            "neoforge"
        }

        )
        {
            var button = ActionButton("选择并自动安装 " + (loader == "forge" ? "Forge" : "NeoForge"), workspace.LoaderController.AutomaticLoaderClick);
            button.Tag = loader;
            workspace.Controls.LoaderActions.Children.Add(button);
        }

        workspace.DownloadPageController.BuildDownloadPage(installStack);
        var settingsStack = (StackPanel)((ScrollViewer)((TabItem)workspace.Window.MainTabs.Items[4]).Content).Content;
        settingsStack.Children.Insert(0, ActionButton("高级启动选项、内存与个性化", GlobalOptionsClick));
        var home = (StackPanel)((ScrollViewer)((TabItem)workspace.Window.MainTabs.Items[0]).Content).Content;
        workspace.Window.AllowDrop = true;
        workspace.Window.PreviewDrop += workspace.InstanceLibraryController.HandleFileDrop;
        workspace.DropFeedbackController.BuildDropFeedback();
    }

    internal async Task HandleScriptLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--root");
        var instance = Array.IndexOf(args, "--instance");
        if (index >= 0 && index + 1 < args.Length && instance >= 0 && instance + 1 < args.Length)
        {
            await SwitchRoot(args[index + 1]);
            workspace.Controls.Versions.SelectedValue = args[instance + 1];
            var account = Array.IndexOf(args, "--account");
            if (account >= 0 && account + 1 < args.Length)
                workspace.Controls.AccountMode.SelectedIndex = args[account + 1] switch
                {
                    "microsoft" => 1,
                    "mua" => 2,
                    _ => 0
                };
            if (workspace.Controls.Versions.SelectedValue != null)
                await workspace.LaunchCoordinator.Run(workspace.LaunchCoordinator.Launch, "脚本启动");
            else
                InlineMessageBox.Show("脚本指定的实例不存在，请重新选择。");
        }
    }

    internal async Task SwitchRoot(string root) => await workspace.LaunchCoordinator.Run(async () =>
    {
        workspace.ContentController.RequireStopped();
        workspace.LaunchCoordinator.prefs.Root = GameDefaults.ResolveRoot(root, true);
        workspace.Controls.GameRoot.Text = workspace.LaunchCoordinator.prefs.Root;
        if (!workspace.LaunchCoordinator.prefs.GameRoots.Contains(workspace.LaunchCoordinator.prefs.Root))
            workspace.LaunchCoordinator.prefs.GameRoots.Add(workspace.LaunchCoordinator.prefs.Root);
        workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs);
        workspace.LaunchCoordinator.SetupLauncher();
        await workspace.StabilityController.StartupRecovery();
        RefreshInstanceViews();
    }, "切换游戏目录");
    internal async void AddRootClick(object s, RoutedEventArgs e)
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
        rootList.ItemsSource = workspace.LaunchCoordinator.prefs.GameRoots.Append(workspace.LaunchCoordinator.prefs.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        rootList.SelectedItem = workspace.LaunchCoordinator.prefs.Root;
        var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
        var result = new List<InstanceView>();
        foreach (var key in workspace.LaunchCoordinator.ProfileKeys())
        {
            try
            {
                var d = catalog.GetOrCreate(key);
                var requirements = RequirementsFor(d.LaunchProfileId);
                var group = d.Favorite ? "★ 收藏夹" : d.Category != "自动" ? d.Category : requirements.Loader == "vanilla" ? "原版游戏" : requirements.Loader.ToUpperInvariant() + " 版本";
                result.Add(new(key, d.DisplayName, group, requirements.Minecraft + " · " + requirements.Loader + " " + requirements.LoaderVersion, File.Exists(d.IconPath) ? d.IconPath : "", JavaDiscovery.RequiredMajor(workspace.LaunchCoordinator.prefs.Root, d.LaunchProfileId)));
            }
            catch (Exception ex)
            {
                workspace.LaunchCoordinator.Log("读取版本 " + key + "：" + ex.Message);
            }
        }

        var view = CollectionViewSource.GetDefaultView(result.OrderBy(r => r.Group.StartsWith('★') ? 0 : 1).ThenBy(r => r.Group).ThenBy(r => r.Name).ToList());
        view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        instanceList.ItemsSource = view;
        FilterInstanceViews();
        instanceList.SelectedItem = result.FirstOrDefault(r => r.Key == workspace.Controls.Versions.SelectedValue as string) ?? result.FirstOrDefault();
        if (workspace.SketchController.sketchHome != null)
            workspace.SketchController.ShowSketchVersions();
        workspace.CompactShellController.UpdateHomeLaunchLabel();
        InstanceViewsChanged?.Invoke();
    }

    internal PackRequirements RequirementsFor(string profile) => ProfileInspector.Read(workspace.LaunchCoordinator.prefs.Root, profile);
    internal void ShowInstanceDetail()
    {
        instanceDetail.Children.Clear();
        if (instanceList.SelectedItem is not InstanceView item)
            return;
        var card = new StackPanel();
        card.Children.Add(Heading(item.Name));
        card.Children.Add(Hint(item.Detail));
        card.Children.Add(Hint("打开版本设置，管理个性化、启动选项、加载器、Mod 和整合包导出。"));
        card.Children.Add(ActionButton("版本设置 →", (_, _) => workspace.VersionSettingsController.OpenVersionSettings(item.Key)));
        card.Children.Add(ActionButton("选中并启动", async (_, _) =>
        {
            if (workspace.LaunchCoordinator.busy)
                return;
            workspace.Controls.Versions.SelectedValue = item.Key;
            workspace.Controls.HomeNav.IsChecked = true;
            await workspace.LaunchCoordinator.Run(workspace.LaunchCoordinator.Launch);
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
            var account = workspace.Controls.AccountMode.SelectedIndex switch
            {
                1 => "microsoft",
                2 => "mua",
                _ => "offline"
            };
            var command = LaunchScriptBuilder.Build(Environment.ProcessPath!, workspace.LaunchCoordinator.prefs.Root,
                key, account, Path.GetExtension(picker.FileName).Equals(".ps1", StringComparison.OrdinalIgnoreCase));
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

    internal void ExportPackDialog(string key) => workspace.CreateInstancePackExportDialog().Show(key);

    internal InlineDialog FeatureDialog(string title, StackPanel content) =>
        new FeatureDialogBuilder(workspace.Window, workspace.ExperienceThemeController.ConfigureUtilityWindow).Build(title, content);

    internal void GlobalOptionsClick(object s, RoutedEventArgs e)
    {
        if (!workspace.LaunchCoordinator.busy)
            EditLaunchOptions(null);
    }

    internal void EditLaunchOptions(InstanceDescriptor? descriptor) => workspace.CreateLaunchOptionsDialog().Show(descriptor);

    internal void ApplyPersonalization()
    {
        var accent = AppearanceController.AccentColor(workspace.LaunchCoordinator.prefs.Accent);
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
        workspace.AppearanceController.ApplyBackground();
        workspace.AvatarController.RefreshAvatar();
    }
}
