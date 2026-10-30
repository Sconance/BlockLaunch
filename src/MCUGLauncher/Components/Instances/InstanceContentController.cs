using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;
internal sealed class InstanceContentController
{
    readonly LauncherWorkspace workspace;
    internal InstanceContentController(LauncherWorkspace workspace)
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
        var choose = FeatureUiElements.ActionButton(">", (_, _) => ToggleSketchVersions());
        sketchToggle = choose;
        choose.ToolTip = "展开版本列表";
        choose.FontSize = 18;
        choose.Padding = new Thickness(0);
        choose.HorizontalContentAlignment = HorizontalAlignment.Center;
        choose.VerticalContentAlignment = VerticalAlignment.Center;
        choose.Margin = new Thickness(5, 0, 0, 3);
        Grid.SetColumn(choose, 1);
        row.Children.Add(choose);
        var manage = FeatureUiElements.ActionButton("⚙", (_, _) =>
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
        var add = FeatureUiElements.ActionButton("＋", workspace.InstanceWorkspaceController.AddRootClick);
        add.ToolTip = "添加已有游戏目录";
        DockPanel.SetDock(add, Dock.Right);
        directory.Children.Add(add);
        if (workspace.InstanceWorkspaceController.rootList.Parent is Panel parent)
            parent.Children.Remove(workspace.InstanceWorkspaceController.rootList);
        workspace.InstanceWorkspaceController.rootList.MinWidth = 80;
        workspace.InstanceWorkspaceController.rootList.HorizontalAlignment = HorizontalAlignment.Stretch;
        workspace.InstanceWorkspaceController.rootList.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextTrimming='CharacterEllipsis' ToolTip='{Binding}'/></DataTemplate>");
        directory.Children.Add(workspace.InstanceWorkspaceController.rootList);
        var titleRow = new DockPanel();
        var tools = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(tools, Dock.Right);
        tools.Children.Add(FeatureUiElements.ActionButton("导入整合包", workspace.ModpackController.ImportPackClick));
        tools.Children.Add(FeatureUiElements.ActionButton("刷新版本", async (_, _) => await workspace.LaunchCoordinator.Run(async () =>
        {
            await workspace.LaunchCoordinator.Refresh();
            workspace.InstanceWorkspaceController.RefreshInstanceViews();
            ShowSketchVersions();
        })));
        titleRow.Children.Add(tools);
        titleRow.Children.Add(FeatureUiElements.Heading("游戏目录"));
        toolbar.Children.Add(titleRow);
        toolbar.Children.Add(directory);
        var searchRow = new Grid();
        var search = new TextBox
        {
            Text = workspace.InstanceWorkspaceController.instanceQuery.Text,
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
            var items = (workspace.InstanceWorkspaceController.instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection.Cast<InstanceView>() ?? Enumerable.Empty<InstanceView>();
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
                    var play = FeatureUiElements.ActionButton("▷", async (_, _) =>
                    {
                        if (workspace.LaunchCoordinator.busy)
                            return;
                        workspace.Controls.Versions.SelectedValue = item.Key;
                        await workspace.LaunchCoordinator.Run(workspace.LaunchCoordinator.Launch);
                    });
                    play.ToolTip = "启动此版本";
                    actions.Children.Add(play);
                    var settingsButton = FeatureUiElements.ActionButton("⚙", (_, _) => OpenSketchManager(item.Key));
                    settingsButton.ToolTip = "管理此版本";
                    actions.Children.Add(settingsButton);
                    line.Children.Add(actions);
                    var label = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    label.Children.Add(new TextBlock { Text = item.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = item.Name });
                    label.Children.Add(new TextBlock { Text = item.Detail, FontSize = 11, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = item.Detail });
                    var select = FeatureUiElements.ActionButton("", (_, _) =>
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

                var groupCard = workspace.InstanceWorkspaceController.FeatureCard(new Expander { Style = InstanceGroupExpanderStyle.Create(), Header = group.Key + "（" + group.Count() + "）", IsExpanded = true, Content = entries, Padding = new Thickness(2), FontSize = 12 });
                groupCard.SetResourceReference(Border.BorderBrushProperty, "ThemeBorderBrush");
                groupCard.Padding = new Thickness(12);
                groupCard.CornerRadius = new CornerRadius(10);
                groupCard.Margin = new Thickness(0, 0, 0, 8);
                groups.Children.Add(groupCard);
            }

            if (matches.Count == 0)
                groups.Children.Add(FeatureUiElements.Hint(query.Length > 0 ? "没有匹配的已安装版本。" : "还没有安装游戏，请到「下载」获取版本，或添加已有游戏目录。"));
        }

        search.TextChanged += (_, _) =>
        {
            workspace.InstanceWorkspaceController.instanceQuery.Text = search.Text;
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

    internal void OpenSketchManager(string initialKey) => workspace.CreateSketchContentManagerDialog(ShowSketchVersions).Open(initialKey);

    internal void OpenSketchManagerCategory(string key, string category) =>
        workspace.CreateSketchContentManagerDialog(ShowSketchVersions).Open(key, category);

    internal static Style CompactVersionScrollStyle() => InstanceListScrollStyle.Create();
}
