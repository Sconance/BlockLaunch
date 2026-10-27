using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly TextBlock homeSelectedLabel = new()
    {
        FontSize = 11,
        Foreground = Brushes.White,
        HorizontalAlignment = HorizontalAlignment.Center
    };
    bool compactShellReady;
    RadioButton downloadNav = null!;
    MenuItem moreMenu = null!;

    internal static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in Elements(child))
                yield return nested;
        }
    }

    void NavigateDownloads(string category)
    {
        downloadNav.IsChecked = true;
        MainTabControl.SelectedIndex = 1;
        SelectDownloadCategory(category);
        PageTitleLabel.Text = "下载";
    }

    void BuildCompactShell()
    {
        var root = (Grid)Content;
        var main = (Grid)root.Children[1];
        root.Children.Remove(main);
        var oldNavigation = (StackPanel)HomeNavigation.Parent;
        var radios = oldNavigation.Children.OfType<RadioButton>().ToList();
        foreach (var radio in radios)
            oldNavigation.Children.Remove(radio);
        root.Children.Clear();
        root.ColumnDefinitions.Clear();
        root.RowDefinitions.Add(new()
        {
            Height = new GridLength(48)
        });
        root.RowDefinitions.Add(new());
        var header = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(0, 109, 184))
        };
        header.Children.Add(new TextBlock { Text = Brand.Name, Foreground = Brushes.White, FontSize = 19, FontWeight = FontWeights.SemiBold, Width = 180, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        var visible = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        var hidden = new StackPanel
        {
            Visibility = Visibility.Collapsed
        };
        var navStyle = (Style)System.Windows.Markup.XamlReader.Parse("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='RadioButton'><Setter Property='ContentTemplate'><Setter.Value><DataTemplate><TextBlock Text='{Binding}' Foreground='White' FontSize='13'/></DataTemplate></Setter.Value></Setter><Setter Property='Foreground' Value='White'/><Setter Property='Margin' Value='5,7'/><Setter Property='Padding' Value='18,7'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='RadioButton'><Border Name='Surface' Background='Transparent' CornerRadius='15' Padding='{TemplateBinding Padding}'><ContentPresenter VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Surface' Property='Background' Value='#28FFFFFF'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='Background' Value='#38FFFFFF'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");
        foreach (var radio in radios)
        {
            if (radio.Tag?.ToString() is "0" or "1" or "4")
            {
                radio.Style = navStyle;
                radio.Content = radio.Tag!.ToString() switch
                {
                    "0" => "▷  启动",
                    "1" => "↓  下载",
                    _ => "⚙  设置"
                };
                visible.Children.Add(radio);
                if (radio.Tag.ToString() == "1")
                    downloadNav = radio;
            }
            else
                hidden.Children.Add(radio);
        }

        var menu = new Menu
        {
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        var more = new MenuItem
        {
            Header = new TextBlock
            {
                Text = "▦  更多",
                Foreground = Brushes.White
            },
            Padding = new Thickness(16, 7, 16, 7)
        };
        menu.Items.Add(more);
        moreMenu = more;
        foreach (var radio in radios.Where(r => r.Tag?.ToString() is "5" or "6" || r.Tag == null))
        {
            var item = new MenuItem
            {
                Header = radio.Content
            };
            item.Click += (_, _) => radio.IsChecked = true;
            more.Items.Add(item);
        }

        visible.Children.Add(menu);
        header.Children.Add(hidden);
        header.Children.Add(visible);
        root.Children.Add(header);
        main.Margin = new Thickness(16, 0, 16, 10);
        main.RowDefinitions[0].Height = new GridLength(56);
        PageTitleLabel.FontSize = 20;
        PageSubtitleLabel.FontSize = 11;
        Grid.SetColumn(main, 0);
        Grid.SetRow(main, 1);
        root.Children.Add(main);
        Width = 1060;
        Height = 740;
        MinWidth = 940;
        MinHeight = 650;
        foreach (var box in Elements(this).OfType<TextBox>())
        {
            box.Padding = new Thickness(9, 6, 9, 6);
            box.Margin = new Thickness(0, 4, 0, 6);
        }

        foreach (var combo in Elements(this).OfType<ComboBox>())
        {
            combo.Padding = new Thickness(9, 6, 9, 6);
            combo.Margin = new Thickness(0, 4, 0, 6);
        }

        foreach (var border in Elements(this).OfType<Border>().Where(b => b.Style == FindResource("Card")))
        {
            border.Padding = new Thickness(12);
            border.CornerRadius = new CornerRadius(7);
        }

        foreach (var type in new[] {
            typeof(TextBox),
            typeof(ComboBox),
            typeof(Button)
        }

        )
        {
            var style = new Style(type, (Style)FindResource(type));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 6, 9, 6)));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 4, type == typeof(Button) ? 6 : 0, 6)));
            Application.Current.Resources[type] = style;
        }

        BuildCompactResourcePage();
        BuildHomeVersionEntry();
        compactShellReady = true;
        PageTitleLabel.Text = "启动";
        PageSubtitleLabel.Text = "选择账号和已安装版本，开始游戏。";
    }

    void BuildHomeVersionEntry()
    {
        var home = (StackPanel)((ScrollViewer)((TabItem)MainTabControl.Items[0]).Content).Content;
        var banner = (Border)home.Children[0];
        var oldGrid = (Grid)home.Children[1];
        var launchCard = (Border)oldGrid.Children[0];
        var accountCard = (Border)oldGrid.Children[1];
        home.Children.Clear();
        oldGrid.Children.Clear();
        var launchPanel = (StackPanel)launchCard.Child;
        launchPanel.Children.Remove(LaunchGameButtonControl);
        launchPanel.Children.Remove(StopGameButtonControl);
        launchPanel.Children.Remove(VersionSelectorControl);
        var left = new DockPanel();
        var launch = new StackPanel();
        DockPanel.SetDock(launch, Dock.Bottom);
        LaunchGameButtonControl.Margin = new Thickness(0, 8, 0, 4);
        LaunchGameButtonControl.Padding = new Thickness(14, 12, 14, 12);
        launch.Children.Add(LaunchGameButtonControl);
        launch.Children.Add(StopGameButtonControl);
        var buttons = new Grid();
        buttons.ColumnDefinitions.Add(new());
        buttons.ColumnDefinitions.Add(new());
        var choose = ActionButton("版本选择", (_, _) => ShowHomeVersions());
        var settingsButton = ActionButton("版本设置", (_, _) =>
        {
            try
            {
                OpenVersionSettings(Selected());
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }
        });
        choose.Margin = new Thickness(0, 4, 4, 0);
        settingsButton.Margin = new Thickness(4, 4, 0, 0);
        Grid.SetColumn(settingsButton, 1);
        buttons.Children.Add(choose);
        buttons.Children.Add(settingsButton);
        launch.Children.Add(buttons);
        VersionSelectorControl.Visibility = Visibility.Collapsed;
        launch.Children.Insert(0, VersionSelectorControl);
        var launchTitle = new StackPanel();
        launchTitle.Children.Add(new TextBlock { Text = "▷  启动游戏", FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
        launchTitle.Children.Add(homeSelectedLabel);
        LaunchGameButtonControl.ContentTemplate = null;
        LaunchGameButtonControl.Content = launchTitle;
        UpdateHomeLaunchLabel();
        left.Children.Add(launch);
        Grid.SetColumn(accountCard, 0);
        accountCard.Margin = new Thickness(0);
        accountCard.Padding = new Thickness(12);
        var accountPanel = (StackPanel)AccountModeControl.Parent;
        accountPanel.Children.Remove(AccountModeControl);
        accountPanel.Children.Insert(0, AccountModeControl);
        accountPanel.Children.OfType<TextBlock>().First(t => t.Text == "玩家档案").Visibility = Visibility.Collapsed;
        AvatarCaptionLabel.Visibility = Visibility.Collapsed;
        var avatarBorder = (Border)SkinAvatarControl.Parent;
        avatarBorder.Width = 72;
        avatarBorder.Height = 72;
        avatarBorder.Margin = new Thickness(0, 14, 0, 8);
        left.Children.Add(new ScrollViewer { Content = accountCard, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(260)
        });
        layout.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(14)
        });
        layout.ColumnDefinitions.Add(new());
        layout.Children.Add(left);
        Grid.SetColumn(banner, 2);
        foreach (var art in Elements(banner).OfType<Viewbox>())
        {
            art.Stretch = Stretch.Uniform;
            art.VerticalAlignment = VerticalAlignment.Bottom;
            art.Height = 184;
        }

        banner.Height = double.NaN;
        banner.VerticalAlignment = VerticalAlignment.Stretch;
        if (File.Exists(prefs.BackgroundPath))
        {
            banner.Background = Brushes.Transparent;
            banner.Child.Visibility = Visibility.Collapsed;
        }

        layout.Children.Add(banner);
        ((TabItem)MainTabControl.Items[0]).Content = layout;
        var chooser = (Grid)((TabItem)MainTabControl.Items[7]).Content;
        var existingLeft = (DockPanel)chooser.Children[0];
        var toolbar = (StackPanel)existingLeft.Children[0];
        existingLeft.Children.Clear();
        chooser.Children.Clear();
        chooser.ColumnDefinitions[0].Width = new GridLength(205);
        toolbar.Margin = new Thickness(0, 0, 12, 0);
        toolbar.Children.Insert(0, ActionButton("← 返回启动", (_, _) =>
        {
            MainTabControl.SelectedIndex = 0;
            PageTitleLabel.Text = "启动";
        }));
        toolbar.Children.Add(ActionButton("使用所选版本", (_, _) =>
        {
            if (instanceList.SelectedItem is not InstanceView item || busy)
                return;
            VersionSelectorControl.SelectedValue = item.Key;
            MainTabControl.SelectedIndex = 0;
            PageTitleLabel.Text = "启动";
        }));
        toolbar.Children.Add(ActionButton("所选版本设置", (_, _) =>
        {
            if (instanceList.SelectedItem is InstanceView item)
                OpenVersionSettings(item.Key);
        }));
        toolbar.Children.Add(ActionButton("新建同版本实例", NewInstanceClick));
        toolbar.Children.Add(Hint("搜索已安装版本"));
        toolbar.Children.Add(instanceQuery);
        toolbar.Children.Add(instanceSearchSummary);
        instanceList.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='40'/><ColumnDefinition/></Grid.ColumnDefinitions><TextBlock Text='◇' FontSize='27' Foreground='#006DB8' VerticalAlignment='Center'/><StackPanel Grid.Column='1'><TextBlock Text='{Binding Name}' FontSize='14' FontWeight='SemiBold'/><TextBlock Text='{Binding Detail}' FontSize='11' Foreground='SlateGray' Margin='0,3,0,0'/></StackPanel></Grid></DataTemplate>");
        chooser.Children.Add(toolbar);
        Grid.SetColumn(instanceList, 1);
        chooser.Children.Add(instanceList);
    }

    void ShowHomeVersions()
    {
        sketchExpanded = true;
        HomeNavigation.IsChecked = true;
        MainTabControl.SelectedIndex = 0;
        PageTitleLabel.Text = "启动";
        PageSubtitleLabel.Text = "选择账号和已安装版本，开始游戏。";
        RefreshInstanceViews();
        ShowSketchVersions();
    }

    void BuildCompactResourcePage()
    {
        resourceGame.Text = "全部";
        resourceLoader.SelectedIndex = 1;
        resourceGame.Width = 115;
        resourceLoader.Width = 125;
        resourceSource.Width = 110;
        releaseChannel.Width = 95;
        ContentInstallButtonControl.Content = "↓ 下载所选文件";

        var directoryButtons = new WrapPanel();
        directoryButtons.Children.Add(ActionButton("选择下载目录", (_, _) =>
        {
            var picker = new OpenFolderDialog
            {
                Title = "选择独立资源下载目录"
            };
            if (picker.ShowDialog(this) != true)
                return;
            if (InstanceUninstaller.Inside(picker.FolderName, prefs.Root))
            {
                InlineMessageBox.Show(this, "请选择游戏目录之外的独立下载目录。");
                return;
            }

            prefs.ResourceDownloadDirectory = Path.GetFullPath(picker.FolderName);
            settings.Save(prefs);
            UpdateTargets();
        }));
        directoryButtons.Children.Add(ActionButton("打开下载目录", (_, _) =>
        {
            Directory.CreateDirectory(prefs.ResourceDownloadDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(prefs.ResourceDownloadDirectory) { UseShellExecute = true });
        }));
        var directoryBar = (Border)ContentTargetLabel.Parent;
        var directoryLayout = new DockPanel();
        directoryBar.Child = null;
        DockPanel.SetDock(directoryButtons, Dock.Right);
        directoryLayout.Children.Add(directoryButtons);
        directoryLayout.Children.Add(ContentTargetLabel);
        directoryBar.Child = directoryLayout;
        ContentTargetLabel.TextTrimming = TextTrimming.CharacterEllipsis;
        ContentTargetLabel.TextWrapping = TextWrapping.NoWrap;
        ContentTargetLabel.VerticalAlignment = VerticalAlignment.Center;
        directoryBar.Padding = new Thickness(8, 3, 8, 3);
        foreach (var button in directoryButtons.Children.OfType<Button>())
        {
            button.Padding = new Thickness(7, 4, 7, 4);
            button.FontSize = 11;
            button.Margin = new Thickness(4, 0, 0, 0);
        }

        BuildResourceWorkspace();
    }

    void UpdateHomeLaunchLabel()
    {
        homeSelectedLabel.Text = VersionSelectorControl.SelectedItem is VersionRow selected ? selected.Label : "尚未安装游戏，请前往下载";
        homeSelectedLabel.ToolTip = homeSelectedLabel.Text;
        if (LaunchGameButtonControl != null)
            LaunchGameButtonControl.ToolTip = homeSelectedLabel.Text;
    }
}
