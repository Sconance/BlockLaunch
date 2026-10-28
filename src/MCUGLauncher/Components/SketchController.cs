using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;
internal sealed class SketchController
{
    readonly LauncherWorkspace workspace;
    internal SketchController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal Grid? sketchHome;
    internal Border? sketchWelcome;
    internal Border? sketchVersions;
    internal Button? sketchToggle;
    internal bool sketchExpanded;
    internal void ToggleSketchVersions()
    {
        sketchExpanded = !sketchExpanded;
        if (sketchExpanded)
            workspace.CompactShellController.ShowHomeVersions();
        else
        {
            if (sketchVersions != null)
                sketchVersions.Visibility = Visibility.Collapsed;
            if (sketchWelcome != null)
                sketchWelcome.Visibility = Visibility.Collapsed;
        }

        UpdateSketchToggle();
    }

    internal void UpdateSketchToggle()
    {
        if (sketchToggle != null)
        {
            sketchToggle.Content = sketchExpanded ? "<" : ">";
            sketchToggle.ToolTip = sketchExpanded ? "收起版本列表" : "展开版本列表";
        }
    }

    internal void BuildSketchUI()
    {
        sketchHome = (Grid)((TabItem)workspace.Window.MainTabs.Items[0]).Content;
        sketchHome.ColumnDefinitions[0].Width = new GridLength(255);
        sketchWelcome = sketchHome.Children.OfType<Border>().FirstOrDefault(b => Grid.GetColumn(b) == 2);
        var left = (DockPanel)sketchHome.Children[0];
        var launch = (StackPanel)left.Children[0];
        var oldButtons = launch.Children.OfType<Grid>().First();
        launch.Children.Remove(oldButtons);
        var row = new Grid();
        row.ColumnDefinitions.Add(new());
        row.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(40)
        });
        row.RowDefinitions.Add(new());
        row.RowDefinitions.Add(new());
        row.Margin = new Thickness(0, 6, 0, 0);
        launch.Children.Remove(workspace.Controls.LaunchGameButton);
        Grid.SetRowSpan(workspace.Controls.LaunchGameButton, 2);
        row.Children.Add(workspace.Controls.LaunchGameButton);
        var choose = InstancePagesController.ActionButton(">", (_, _) => ToggleSketchVersions());
        sketchToggle = choose;
        choose.ToolTip = "展开版本列表";
        choose.FontSize = 18;
        choose.Padding = new Thickness(0);
        choose.HorizontalContentAlignment = HorizontalAlignment.Center;
        choose.VerticalContentAlignment = VerticalAlignment.Center;
        choose.Margin = new Thickness(5, 0, 0, 3);
        Grid.SetColumn(choose, 1);
        row.Children.Add(choose);
        var manage = InstancePagesController.ActionButton("⚙", (_, _) =>
        {
            try
            {
                OpenSketchManager(workspace.LaunchCoordinator.Selected());
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }
        });
        manage.ToolTip = "管理当前版本";
        manage.FontSize = 16;
        manage.Padding = new Thickness(0);
        manage.HorizontalContentAlignment = HorizontalAlignment.Center;
        manage.VerticalContentAlignment = VerticalAlignment.Center;
        manage.Margin = new Thickness(5, 3, 0, 0);
        Grid.SetColumn(manage, 1);
        Grid.SetRow(manage, 1);
        row.Children.Add(manage);
        launch.Children.Insert(0, row);
        workspace.Controls.LaunchGameButton.FontSize = 15;
        if (sketchWelcome != null)
            sketchWelcome.Visibility = Visibility.Collapsed;
        var main = (Grid)workspace.Window.MainTabs.Parent;
        main.RowDefinitions[0].Height = new GridLength(8);
        var heading = (Grid)((StackPanel)workspace.Controls.PageTitle.Parent).Parent;
        heading.Visibility = Visibility.Collapsed;
        var header = (DockPanel)((Grid)workspace.Window.Content).Children[0];
        var logo = new BitmapImage(new Uri("pack://application:,,,/MCUGLauncher;component/Assets/MCUGLogo.png"));
        workspace.Window.Icon = logo;
        header.Children.Insert(0, new Image { Source = logo, Width = 30, Height = 30, Margin = new Thickness(12, 6, 0, 6) });
        if (header.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock brand)
        {
            brand.Width = 160;
            brand.Margin = new Thickness(8, 0, 0, 0);
        }

        var greeting = new TextBlock
        {
            Text = "选择版本，开始游戏。",
            Foreground = Brushes.White,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 8, 0)
        };
        var navigation = (StackPanel)header.Children[header.Children.Count - 1];
        navigation.Children.Add(greeting);
        UpdateSketchToggle();
    }

    internal void ShowSketchVersions()
    {
        if (sketchHome == null)
            return;
        if (!sketchExpanded)
            return;
        if (sketchWelcome != null)
            sketchWelcome.Visibility = Visibility.Collapsed;
        if (sketchVersions != null)
            sketchHome.Children.Remove(sketchVersions);
        var body = new DockPanel();
        var toolbar = new StackPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        var directory = new DockPanel();
        var add = InstancePagesController.ActionButton("＋", workspace.InstancePagesController.AddRootClick);
        add.ToolTip = "添加已有游戏目录";
        DockPanel.SetDock(add, Dock.Right);
        directory.Children.Add(add);
        if (workspace.InstancePagesController.rootList.Parent is Panel parent)
            parent.Children.Remove(workspace.InstancePagesController.rootList);
        workspace.InstancePagesController.rootList.MinWidth = 80;
        workspace.InstancePagesController.rootList.HorizontalAlignment = HorizontalAlignment.Stretch;
        workspace.InstancePagesController.rootList.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextTrimming='CharacterEllipsis' ToolTip='{Binding}'/></DataTemplate>");
        directory.Children.Add(workspace.InstancePagesController.rootList);
        var titleRow = new DockPanel();
        var tools = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(tools, Dock.Right);
        tools.Children.Add(InstancePagesController.ActionButton("导入整合包", workspace.PackController.ImportPackClick));
        tools.Children.Add(InstancePagesController.ActionButton("刷新版本", async (_, _) => await workspace.LaunchCoordinator.Run(async () =>
        {
            await workspace.LaunchCoordinator.Refresh();
            workspace.InstancePagesController.RefreshInstanceViews();
            ShowSketchVersions();
        })));
        titleRow.Children.Add(tools);
        titleRow.Children.Add(InstancePagesController.Heading("游戏目录"));
        toolbar.Children.Add(titleRow);
        toolbar.Children.Add(directory);
        var searchRow = new Grid();
        var search = new TextBox
        {
            Text = workspace.InstancePagesController.instanceQuery.Text,
            ToolTip = "搜索已安装版本",
            Margin = new Thickness(0, 4, 0, 10)
        };
        searchRow.Children.Add(search);
        var watermark = new TextBlock
        {
            Text = "点此处搜索",
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 12, 6)
        };
        watermark.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
        searchRow.Children.Add(watermark);
        toolbar.Children.Add(searchRow);
        body.Children.Add(toolbar);
        var groups = new StackPanel();
        void RenderGroups()
        {
            groups.Children.Clear();
            var query = search.Text.Trim();
            var items = (workspace.InstancePagesController.instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection.Cast<InstanceView>() ?? Enumerable.Empty<InstanceView>();
            watermark.Visibility = search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            var matches = items.Where(i => (i.Name + " " + i.Detail).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var group in matches.GroupBy(i => i.Group))
            {
                var entries = new StackPanel();
                foreach (var item in group)
                {
                    var line = new DockPanel
                    {
                        Margin = new Thickness(0, 2, 0, 2),
                        MinHeight = 54
                    };
                    var actions = new StackPanel
                    {
                        Orientation = Orientation.Horizontal
                    };
                    DockPanel.SetDock(actions, Dock.Right);
                    var play = InstancePagesController.ActionButton("▷", async (_, _) =>
                    {
                        if (workspace.LaunchCoordinator.busy)
                            return;
                        workspace.Controls.Versions.SelectedValue = item.Key;
                        await workspace.LaunchCoordinator.Run(workspace.LaunchCoordinator.Launch);
                    });
                    play.ToolTip = "启动此版本";
                    actions.Children.Add(play);
                    var settingsButton = InstancePagesController.ActionButton("⚙", (_, _) => OpenSketchManager(item.Key));
                    settingsButton.ToolTip = "管理此版本";
                    actions.Children.Add(settingsButton);
                    line.Children.Add(actions);
                    var label = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    label.Children.Add(new TextBlock { Text = item.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = item.Name });
                    label.Children.Add(new TextBlock { Text = item.Detail, FontSize = 11, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = item.Detail });
                    var select = InstancePagesController.ActionButton("", (_, _) =>
                    {
                        if (workspace.LaunchCoordinator.busy && !workspace.DownloadTaskController.DownloadInProgress)
                            return;
                        workspace.Controls.Versions.SelectedValue = item.Key;
                        workspace.CompactShellController.UpdateHomeLaunchLabel();
                        RenderGroups();
                    });
                    select.ContentTemplate = null;
                    select.Content = label;
                    select.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    select.ToolTip = item.Name + "\n" + item.Detail;
                    select.Padding = new Thickness(12, 7, 12, 7);
                    select.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' CornerRadius='8' Padding='{TemplateBinding Padding}' BorderThickness='2,0,0,0' BorderBrush='{TemplateBinding BorderBrush}'><ContentPresenter HorizontalAlignment='Stretch' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='0.8'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
                    select.BorderBrush = Brushes.Transparent;
                    if (workspace.Controls.Versions.SelectedValue as string == item.Key)
                        select.SetResourceReference(Control.BackgroundProperty, "ThemeSelectedBrush");
                    if (workspace.Controls.Versions.SelectedValue as string == item.Key)
                        select.SetResourceReference(Control.BorderBrushProperty, "AccentBrush");
                    line.Children.Add(select);
                    entries.Children.Add(line);
                }

                var groupCard = workspace.InstancePagesController.FeatureCard(new Expander { Header = group.Key + "（" + group.Count() + "）", IsExpanded = true, Content = entries, Padding = new Thickness(2), FontSize = 12 });
                groupCard.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");
                groupCard.Padding = new Thickness(12);
                groupCard.CornerRadius = new CornerRadius(10);
                groupCard.Margin = new Thickness(0, 0, 0, 8);
                groups.Children.Add(groupCard);
            }

            if (matches.Count == 0)
                groups.Children.Add(InstancePagesController.Hint(query.Length > 0 ? "没有匹配的已安装版本。" : "还没有安装游戏，请到「下载」获取版本，或添加已有游戏目录。"));
        }

        search.TextChanged += (_, _) =>
        {
            workspace.InstancePagesController.instanceQuery.Text = search.Text;
            RenderGroups();
        };
        RenderGroups();
        body.Children.Add(new ScrollViewer { Content = groups, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        sketchVersions = new Border
        {
            Padding = new Thickness(4, 0, 0, 0),
            Child = body
        };
        Grid.SetColumn(sketchVersions, 2);
        sketchHome.Children.Add(sketchVersions);
        UpdateSketchToggle();
    }

    internal void OpenSketchManager(string initialKey)
    {
        if (workspace.LaunchCoordinator.busy && !workspace.DownloadTaskController.DownloadInProgress)
            return;
        workspace.InstancePagesController.RefreshInstanceViews();
        var page = new InlineDialog
        {
            Owner = workspace.Window,
            Title = "版本内容管理",
            Tag = "SketchManager"
        };
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(224)
        });
        layout.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(110)
        });
        layout.ColumnDefinitions.Add(new());
        page.Content = layout;
        var versions = new DockPanel
        {
            Margin = new Thickness(0, 0, 12, 0)
        };
        layout.Children.Add(versions);
        var top = new StackPanel();
        DockPanel.SetDock(top, Dock.Top);
        top.Children.Add(InstancePagesController.Heading("游戏目录"));
        var directories = new ComboBox
        {
            ToolTip = workspace.LaunchCoordinator.prefs.Root,
            ItemsSource = workspace.LaunchCoordinator.prefs.GameRoots.Append(workspace.LaunchCoordinator.prefs.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SelectedItem = workspace.LaunchCoordinator.prefs.Root,
            Margin = new Thickness(0, 5, 0, 10),
            MinWidth = 0
        };
        directories.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextTrimming='CharacterEllipsis' ToolTip='{Binding}'/></DataTemplate>");
        top.Children.Add(directories);
        versions.Children.Add(top);
        var bottom = InstancePagesController.ActionButton("＋ 导入整合包", (_, e) =>
        {
            page.Close();
            workspace.PackController.ImportPackClick(workspace.Window, e);
        });
        bottom.Margin = new Thickness(0, 9, 0, 0);
        bottom.MinHeight = 36;
        var footer = new Border
        {
            BorderThickness = new Thickness(0, 2, 0, 0),
            Padding = new Thickness(0, 7, 0, 0),
            Child = bottom
        };
        footer.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");
        DockPanel.SetDock(footer, Dock.Bottom);
        versions.Children.Add(footer);
        var list = new ListBox
        {
            ItemsSource = (workspace.InstancePagesController.instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection,
            Padding = new Thickness(0, 0, 3, 0)
        };
        list.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Name}' TextTrimming='CharacterEllipsis' ToolTip='{Binding Name}' VerticalAlignment='Center' FontSize='12'/></DataTemplate>");
        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
        itemStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 44.0));
        itemStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 2, 1, 2)));
        list.ItemContainerStyle = itemStyle;
        itemStyle.Setters.Add(new Setter(Control.TemplateProperty, System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListBoxItem'><Border x:Name='Surface' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Background='{DynamicResource ThemeSurfaceBrush}' BorderBrush='{DynamicResource ThemeBorderBrush}' BorderThickness='1' CornerRadius='8' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Stretch' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsSelected' Value='True'><Setter TargetName='Surface' Property='Background' Value='{DynamicResource ThemeSelectedBrush}'/><Setter TargetName='Surface' Property='BorderBrush' Value='{DynamicResource AccentBrush}'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='BorderBrush' Value='{DynamicResource AccentBrush}'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));
        list.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = CompactVersionScrollStyle();
        list.Resources["ManagerScrollBar"] = list.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)];
        list.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ListBox">
          <ScrollViewer x:Name="PART_ScrollViewer" Focusable="False" CanContentScroll="False" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
            <ScrollViewer.Template><ControlTemplate TargetType="ScrollViewer"><Grid><Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="8"/></Grid.ColumnDefinitions>
              <ScrollContentPresenter x:Name="PART_ScrollContentPresenter" CanContentScroll="{TemplateBinding CanContentScroll}" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"/>
              <ScrollBar x:Name="PART_VerticalScrollBar" Grid.Column="1" Width="8" MinWidth="0" MaxWidth="8" Orientation="Vertical" Style="{DynamicResource ManagerScrollBar}" Maximum="{TemplateBinding ScrollableHeight}" ViewportSize="{TemplateBinding ViewportHeight}" Value="{Binding VerticalOffset,RelativeSource={RelativeSource TemplatedParent},Mode=OneWay}" Visibility="{TemplateBinding ComputedVerticalScrollBarVisibility}"/>
            </Grid></ControlTemplate></ScrollViewer.Template>
            <ItemsPresenter/>
          </ScrollViewer>
        </ControlTemplate>
        """);
        ScrollViewer.SetCanContentScroll(list, false);
        VirtualizingPanel.SetScrollUnit(list, ScrollUnit.Pixel);
        versions.Children.Add(list);
        var categories = new StackPanel
        {
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(categories, 1);
        layout.Children.Add(categories);
        var right = new DockPanel();
        Grid.SetColumn(right, 2);
        layout.Children.Add(right);
        var bar = new StackPanel();
        DockPanel.SetDock(bar, Dock.Top);
        right.Children.Add(bar);
        var files = new ListBox
        {
            DisplayMemberPath = "Name"
        };
        ScrollViewer.SetCanContentScroll(files, false);
        var info = InstancePagesController.Hint("选择内容查看详情。");
        var preview = new Image
        {
            Height = 180,
            Stretch = Stretch.Uniform
        };
        var detail = new StackPanel();
        detail.Children.Add(info);
        detail.Children.Add(preview);
        DockPanel.SetDock(detail, Dock.Bottom);
        right.Children.Add(detail);
        right.Children.Add(files);
        var current = initialKey;
        var category = "存档";
        string directory = "";
        string DataRoot()
        {
            var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
            var d = catalog.GetOrCreate(current);
            return (d.Launch ?? workspace.LaunchCoordinator.prefs.Launch).Isolation == "共享根目录" ? workspace.LaunchCoordinator.prefs.Root : catalog.DirectoryFor(current);
        }

        void Render()
        {
            bar.Children.Clear();
            files.ItemsSource = null;
            preview.Source = null;
            info.Text = "选择内容查看详情。";
            if (list.SelectedItem is not InstanceView selected)
                return;
            current = selected.Key;
            workspace.DropFeedbackController.sketchManagerKey = current;
            var actions = new WrapPanel();
            actions.Children.Add(InstancePagesController.ActionButton("版本设置", (_, _) => workspace.VersionSettingsController.OpenVersionSettings(current)));
            actions.Children.Add(InstancePagesController.ActionButton("导出", (_, _) => workspace.VersionSettingsController.OpenVersionSection(current, "导出")));
            actions.Children.Add(InstancePagesController.ActionButton("删除版本", (_, _) =>
            {
                page.Close();
                workspace.UninstallController.UninstallInstance(current);
            }));
            bar.Children.Add(InstancePagesController.Heading(selected.Name));
            bar.Children.Add(actions);
            directory = Path.Combine(DataRoot(), category switch
            {
                "存档" => "saves",
                "模组" => "mods",
                "资源包" => "resourcepacks",
                "光影" => "shaderpacks",
                "截图" => "screenshots",
                "蓝图" => "schematics",
                _ => ""
            });
            var folder = InstancePagesController.ActionButton("打开文件夹", (_, _) =>
            {
                try
                {
                    workspace.LaunchCoordinator.OpenFolder(directory);
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            });
            bar.Children.Add(folder);
            if (category == "服务器")
            {
                files.ItemsSource = null;
                info.Text = "服务器地址保存于实例 servers.dat；点击下方管理启动器服务器收藏。";
                bar.Children.Add(InstancePagesController.ActionButton("服务器收藏", (_, _) => workspace.ExperienceController.OpenServers()));
                return;
            }

            files.ItemsSource = Directory.Exists(directory) ? new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(f => f.Name).ToList() : null;
            if (files.Items.Count == 0)
                info.Text = "此分类暂无内容。";
            if (category == "存档" || category == "截图")
                bar.Children.Add(InstancePagesController.ActionButton("备份、导入与预览", (_, _) => workspace.InstanceLibraryController.OpenInstanceLibrary(current)));
            if (category == "模组")
                bar.Children.Add(InstancePagesController.ActionButton("启用、禁用与更新", (_, _) => workspace.VersionSettingsController.OpenVersionSection(current, "Mod 管理")));
        }

        void SafeRender()
        {
            try
            {
                Render();
            }
            catch (Exception ex)
            {
                files.ItemsSource = null;
                preview.Source = null;
                info.Text = "无法读取此分类：" + ex.Message;
            }
        }

        workspace.DropFeedbackController.refreshSketchManager = SafeRender;
        foreach (var name in new[]
        {
            "存档",
            "模组",
            "资源包",
            "光影",
            "服务器",
            "截图",
            "蓝图"
        }

        )
        {
            var nav = new RadioButton
            {
                Content = name,
                GroupName = "SketchCategories",
                Style = VersionSettingsController.VersionMenuStyle(),
                Margin = new Thickness(0, 4, 0, 4)
            };
            nav.Checked += (_, _) =>
            {
                category = name;
                SafeRender();
            };
            categories.Children.Add(nav);
        }

        workspace.MenuExtensionsController.AttachExtensionButtons(LauncherMenuArea.InstanceContent, categories, lifetime: page, instanceIdProvider: () => current);
        files.SelectionChanged += (_, _) =>
        {
            preview.Source = null;
            if (files.SelectedItem is not FileSystemInfo entry)
                return;
            info.Text = entry.Name + "\n修改时间：" + entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            if (category == "截图" && entry is FileInfo file && file.Length < 20 * 1024 * 1024 && new[]
            {
                ".png",
                ".jpg",
                ".jpeg"
            }.Contains(file.Extension.ToLowerInvariant()))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(file.FullName);
                    bitmap.DecodePixelWidth = 900;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    preview.Source = bitmap;
                }
                catch
                {
                    info.Text += "\n此图片暂时无法预览。";
                }
            }
        };
        list.SelectionChanged += (_, _) => SafeRender();
        list.SelectedItem = list.Items.Cast<InstanceView>().FirstOrDefault(i => i.Key == initialKey);
        ((RadioButton)categories.Children[0]).IsChecked = true;
        directories.SelectionChanged += async (_, _) =>
        {
            if (workspace.LaunchCoordinator.busy || directories.SelectedItem is not string root || root.Equals(workspace.LaunchCoordinator.prefs.Root, StringComparison.OrdinalIgnoreCase))
                return;
            directories.IsEnabled = false;
            list.IsEnabled = false;
            categories.IsEnabled = false;
            bar.IsEnabled = false;
            try
            {
                await workspace.InstancePagesController.SwitchRoot(root);
                directories.SelectedItem = workspace.LaunchCoordinator.prefs.Root;
                directories.ToolTip = workspace.LaunchCoordinator.prefs.Root;
                list.ItemsSource = (workspace.InstancePagesController.instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection;
                list.SelectedItem = list.Items.Cast<InstanceView>().FirstOrDefault();
                SafeRender();
                if (list.SelectedItem == null)
                    info.Text = "此游戏目录暂无已安装版本。";
            }
            finally
            {
                directories.IsEnabled = true;
                list.IsEnabled = true;
                categories.IsEnabled = true;
                bar.IsEnabled = true;
            }
        };
        page.Closed += (_, _) =>
        {
            workspace.DropFeedbackController.sketchManagerKey = null;
            workspace.DropFeedbackController.refreshSketchManager = null;
            workspace.InstancePagesController.RefreshInstanceViews();
            ShowSketchVersions();
        };
        page.ShowDialog();
    }

    internal static Style CompactVersionScrollStyle() => (Style)System.Windows.Markup.XamlReader.Parse("""
    <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ScrollBar">
      <Setter Property="Width" Value="8"/><Setter Property="MinWidth" Value="0"/><Setter Property="MaxWidth" Value="8"/><Setter Property="Background" Value="Transparent"/>
      <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ScrollBar">
        <Grid Background="Transparent"><Track x:Name="PART_Track" Orientation="Vertical" IsDirectionReversed="True" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" Value="{TemplateBinding Value}" ViewportSize="{TemplateBinding ViewportSize}">
          <Track.DecreaseRepeatButton><RepeatButton Command="ScrollBar.PageUpCommand" Opacity="0" Focusable="False"/></Track.DecreaseRepeatButton>
          <Track.Thumb><Thumb MinHeight="24"><Thumb.Template><ControlTemplate TargetType="Thumb"><Border x:Name="Grip" Margin="2,0" CornerRadius="3" Background="{DynamicResource UiSecondaryTextBrush}" Opacity="0.45"/><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Grip" Property="Opacity" Value="0.8"/></Trigger><Trigger Property="IsDragging" Value="True"><Setter TargetName="Grip" Property="Background" Value="{DynamicResource AccentBrush}"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
          <Track.IncreaseRepeatButton><RepeatButton Command="ScrollBar.PageDownCommand" Opacity="0" Focusable="False"/></Track.IncreaseRepeatButton>
        </Track></Grid>
      </ControlTemplate></Setter.Value></Setter>
    </Style>
    """);
}
