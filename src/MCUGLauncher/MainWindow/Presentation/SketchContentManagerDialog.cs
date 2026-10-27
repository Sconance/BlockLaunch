using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;

internal sealed class SketchContentManagerDialog(MainWindow host, ListBox instanceList, Action showSketchVersions)
{
    public void Open(string initialKey)
    {
        if (host.busy && !host.DownloadInProgress)
            return;
        host.RefreshInstanceViews();
        var page = new InlineDialog
        {
            Owner = host,
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
        top.Children.Add(MainWindow.Heading("游戏目录"));
        var directories = new ComboBox
        {
            ToolTip = host.prefs.Root,
            ItemsSource = host.prefs.GameRoots.Append(host.prefs.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SelectedItem = host.prefs.Root,
            Margin = new Thickness(0, 5, 0, 10),
            MinWidth = 0
        };
        directories.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextTrimming='CharacterEllipsis' ToolTip='{Binding}'/></DataTemplate>");
        top.Children.Add(directories);
        versions.Children.Add(top);
        var bottom = MainWindow.ActionButton("＋ 导入整合包", (_, e) =>
        {
            page.Close();
            host.ImportPackClick(host, e);
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
            ItemsSource = (instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection,
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
        var info = MainWindow.Hint("选择内容查看详情。");
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
            var catalog = new InstanceCatalog(host.prefs.Root);
            var d = catalog.GetOrCreate(current);
            return (d.Launch ?? host.prefs.Launch).Isolation == "共享根目录" ? host.prefs.Root : catalog.DirectoryFor(current);
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
            host.sketchManagerKey = current;
            var actions = new WrapPanel();
            actions.Children.Add(MainWindow.ActionButton("版本设置", (_, _) => host.OpenVersionSettings(current)));
            actions.Children.Add(MainWindow.ActionButton("导出", (_, _) => host.OpenVersionSection(current, "导出")));
            actions.Children.Add(MainWindow.ActionButton("删除版本", (_, _) =>
            {
                page.Close();
                host.UninstallInstance(current);
            }));
            bar.Children.Add(MainWindow.Heading(selected.Name));
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
            var folder = MainWindow.ActionButton("打开文件夹", (_, _) =>
            {
                try
                {
                    host.OpenFolder(directory);
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
                bar.Children.Add(MainWindow.ActionButton("服务器收藏", (_, _) => host.OpenServers()));
                return;
            }

            files.ItemsSource = Directory.Exists(directory) ? new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(f => f.Name).ToList() : null;
            if (files.Items.Count == 0)
                info.Text = "此分类暂无内容。";
            if (category == "存档" || category == "截图")
                bar.Children.Add(MainWindow.ActionButton("备份、导入与预览", (_, _) => host.OpenInstanceLibrary(current)));
            if (category == "模组")
                bar.Children.Add(MainWindow.ActionButton("启用、禁用与更新", (_, _) => host.OpenVersionSection(current, "Mod 管理")));
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

        host.refreshSketchManager = SafeRender;
        foreach (var name in new[] {
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
                Style = NavigationMenuStyle.Create(),
                Margin = new Thickness(0, 4, 0, 4)
            };
            nav.Checked += (_, _) =>
            {
                category = name;
                SafeRender();
            };
            categories.Children.Add(nav);
        }

        files.SelectionChanged += (_, _) =>
        {
            preview.Source = null;
            if (files.SelectedItem is not FileSystemInfo entry)
                return;
            info.Text = entry.Name + "\n修改时间：" + entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            if (category == "截图" && entry is FileInfo file && file.Length < 20 * 1024 * 1024 && new[] {
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
            if (host.busy || directories.SelectedItem is not string root || root.Equals(host.prefs.Root, StringComparison.OrdinalIgnoreCase))
                return;
            directories.IsEnabled = false;
            list.IsEnabled = false;
            categories.IsEnabled = false;
            bar.IsEnabled = false;
            try
            {
                await host.SwitchRoot(root);
                directories.SelectedItem = host.prefs.Root;
                directories.ToolTip = host.prefs.Root;
                list.ItemsSource = (instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection;
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
            host.sketchManagerKey = null;
            host.refreshSketchManager = null;
            host.RefreshInstanceViews();
            showSketchVersions();
        };
        page.ShowDialog();
    }

    static Style CompactVersionScrollStyle() => (Style)System.Windows.Markup.XamlReader.Parse("""
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
