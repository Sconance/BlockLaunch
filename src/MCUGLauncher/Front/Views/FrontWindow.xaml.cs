using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public partial class FrontWindow : Window
{
    readonly ResourceBrowserViewModel resources;
    readonly ResourceBrowserView browser;
    readonly SettingsViewModel settings;
    readonly SettingsView settingsView;

    public FrontWindow(IFrontResourceBrowser resourceBrowser, IFrontSettings settingsService, string mode)
    {
        InitializeComponent();
        ModeLabel.Text = mode;
        resources = new ResourceBrowserViewModel(resourceBrowser);
        browser = new ResourceBrowserView(resources);
        settings = new SettingsViewModel(settingsService);
        settingsView = new SettingsView(settings);
        Closed += (_, _) =>
        {
            resources.Dispose();
            settings.Dispose();
        };
        HomeNavigation.IsChecked = true;
    }

    void Navigate(object sender, RoutedEventArgs args)
    {
        if (sender is not RadioButton { Tag: string page })
            return;
        if (page == "resources")
        {
            PageTitle.Text = "内容中心";
            PageSubtitle.Text = "搜索、筛选和查看兼容版本，通过接口接入现有资源服务。";
            PageHost.Content = browser;
            return;
        }
        if (page == "home")
        {
            ShowHome();
            return;
        }
        if (page == "settings")
        {
            PageTitle.Text = "启动设置";
            PageSubtitle.Text = "通过独立接口读取、校验和保存设置，不依赖旧窗口。";
            PageHost.Content = settingsView;
            return;
        }
        var description = page switch
        {
            "install" => ("游戏安装", "版本列表、加载器选择与安装任务将在下一阶段接入。"),
            "content" => ("内容管理", "本地资源列表、启用/禁用和更新预览尚未迁移。"),
            "logs" => ("运行日志", "游戏输出与诊断尚未接入；这里不会读取真实日志。"),
            _ => ("任务与恢复", "下载进度、暂停、继续和事务回退尚未接入。")
        };
        PageTitle.Text = description.Item1;
        PageSubtitle.Text = "导航已复现 · 功能尚未迁移";
        var panel = new StackPanel();
        panel.Children.Add(Heading(description.Item1));
        panel.Children.Add(Hint(description.Item2));
        panel.Children.Add(Hint("请使用原启动器执行这项操作；Front 预览不提供未接入的业务按钮。"));
        PageHost.Content = Card(panel);
    }

    void ShowHome()
    {
        PageTitle.Text = "开始游戏";
        PageSubtitle.Text = "欢迎回来，今天也去创造一点不同。";
        var panel = new StackPanel();
        var hero = new StackPanel();
        hero.Children.Add(Heading("你的下一段冒险，从这里开始"));
        hero.Children.Add(Hint("沿用现有浅色、蓝色强调和卡片布局；当前为迁移预览，不读取账号与本地实例。"));
        panel.Children.Add(Card(hero));
        var columns = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        columns.ColumnDefinitions.Add(new() { Width = new GridLength(2, GridUnitType.Star) });
        columns.ColumnDefinitions.Add(new() { Width = new GridLength(18) });
        columns.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        var launch = new StackPanel();
        launch.Children.Add(Heading("选择你的世界"));
        launch.Children.Add(Hint("示例实例 · Minecraft 1.21.1 · Fabric"));
        launch.Children.Add(new ComboBox { ItemsSource = new[] { "演示实例（模拟数据）" }, SelectedIndex = 0, Margin = new Thickness(0, 14, 0, 8) });
        launch.Children.Add(new Button { Content = "启动游戏 · 尚未接入", IsEnabled = false });
        launch.Children.Add(Hint("启动控制器尚未接入，避免预览期间执行真实操作。"));
        columns.Children.Add(Card(launch));
        var identity = new StackPanel();
        identity.Children.Add(Heading("玩家档案"));
        identity.Children.Add(new TextBlock { Text = "Player", FontSize = 24, Margin = new Thickness(0, 20, 0, 8) });
        identity.Children.Add(Hint("模拟玩家 · 不读取真实账号"));
        var identityCard = Card(identity);
        Grid.SetColumn(identityCard, 2);
        columns.Children.Add(identityCard);
        panel.Children.Add(columns);
        PageHost.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    static TextBlock Heading(string text) => new() { Text = text, FontSize = 20, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };

    static TextBlock Hint(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0), Style = (Style)Application.Current.FindResource("Caption") };

    static Border Card(UIElement content) => new() { Child = content, Style = (Style)Application.Current.FindResource("Card"), VerticalAlignment = VerticalAlignment.Top };
}
