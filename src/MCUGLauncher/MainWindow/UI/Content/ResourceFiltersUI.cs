using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly ContentSearchFilterBuilder contentSearchFilterBuilder = new();
    TextBox resourceGame => contentSearchFilterBuilder.GameVersion;
    ComboBox resourceLoader => contentSearchFilterBuilder.Loader;
    ComboBox resourceSource => contentSearchFilterBuilder.Source;
    ComboBox releaseChannel => contentSearchFilterBuilder.ReleaseChannel;

    void BuildResourceFilters()
    {
        var filters = contentSearchFilterBuilder.Build(ContentSearchQueryInput, ContentKindSelector, () => busy, ResetSearch);
        var manage = (Grid)InstalledContentListControl.Parent;
        var toolbar = manage.Children.OfType<WrapPanel>().First(p => Grid.GetRow(p) == 1);
        toolbar.Children.Add(ActionButton("导入光影", async (_, _) => await Import("shaderpacks", "光影包|*.zip")));
        toolbar.Children.Add(ActionButton("管理存档数据包", (_, _) =>
        {
            if (busy)
                return;
            var picker = new OpenFolderDialog
            {
                Title = "选择当前实例内的存档",
                InitialDirectory = System.IO.Path.Combine(Instance(), "saves")
            };
            if (picker.ShowDialog() != true)
                return;
            var saves = System.IO.Path.GetFullPath(System.IO.Path.Combine(Instance(), "saves")) + System.IO.Path.DirectorySeparatorChar;
            if (!System.IO.Path.GetFullPath(picker.FolderName).StartsWith(saves, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(System.IO.Path.Combine(picker.FolderName, "level.dat")))
            {
                InlineMessageBox.Show("请选择当前实例 saves 内含 level.dat 的存档。");
                return;
            }

            managedWorld = picker.FolderName;
            RefreshLocalContent();
        }));
        toolbar.Children.Add(ActionButton("返回实例内容", (_, _) =>
        {
            managedWorld = null;
            UpdateTargets();
        }));
        var hint = new TextBlock
        {
            Text = "光影需 Iris / OptiFine 等支持；数据包安装时选择目标存档。CurseForge 接口已预留，未接入在线服务。",
            Foreground = System.Windows.Media.Brushes.SlateGray,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        };
        filters.Children.Add(hint);
    }

    InstanceTarget SearchTarget()
    {
        var game = resourceGame.Text.Trim();
        if (game is "" or "全部" or "跟随实例")
            game = "";
        var loader = resourceLoader.SelectedIndex < 2 ? "" : resourceLoader.SelectedItem?.ToString() ?? "";
        return new("", game, ContentKind == "datapack" ? "datapack" : loader);
    }
}
