using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class ContentSearchFilterBuilder
{
    public ComboBox GameVersion
    {
        get;
    } = new ResourceVersionSelector()
    {
        Width = 130,
        IsEditable = true,
        IsTextSearchEnabled = true,
        Text = "全部",
        ItemsSource = new[] { "全部" },
        SelectedIndex = 0,
        ToolTip = "选择或搜索 Minecraft 版本"
    };

    public ComboBox Loader
    {
        get;
    } = new()
    {
        Width = 135,
        ItemsSource = new[] {
            "跟随实例",
            "全部加载器",
            "fabric",
            "forge",
            "neoforge",
            "quilt"
        },
        SelectedIndex = 0
    };

    public ComboBox Source
    {
        get;
    } = new()
    {
        Width = 140,
        ItemsSource = new[] {
            "Modrinth",
            "CurseForge（未启用）"
        },
        SelectedIndex = 0
    };

    public ComboBox ReleaseChannel
    {
        get;
    } = new()
    {
        Width = 100,
        ItemsSource = new[] {
            "全部类型",
            "正式版",
            "测试版",
            "开发版"
        },
        SelectedIndex = 0
    };

    public StackPanel Build(TextBox searchQuery, ComboBox contentType, Func<bool> isBusy, Action resetSearch)
    {
        contentType.Items.Add(new ComboBoxItem { Content = "光影包" });
        contentType.Items.Add(new ComboBoxItem { Content = "数据包" });
        var old = (Grid)searchQuery.Parent;
        var parent = (Grid)old.Parent;
        parent.Children.Remove(old);
        var filters = new StackPanel();
        Grid.SetRow(filters, 1);
        parent.Children.Add(filters);
        filters.Children.Add(old);
        Grid.SetRow(old, 0);
        var row = new WrapPanel();
        row.Children.Add(new TextBlock { Text = "游戏版本", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        row.Children.Add(GameVersion);
        row.Children.Add(Loader);
        row.Children.Add(Source);
        row.Children.Add(ReleaseChannel);
        row.Children.Add(FeatureUiElements.ActionButton("重置条件", (_, _) =>
        {
            if (isBusy())
                return;
            searchQuery.Clear();
            GameVersion.Text = "全部";
            Loader.SelectedIndex = 1;
            Source.SelectedIndex = 0;
            ReleaseChannel.SelectedIndex = 0;
            resetSearch();
        }));
        filters.Children.Add(row);
        return filters;
    }
}
