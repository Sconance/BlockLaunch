using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class ResourceFiltersController
{
    readonly LauncherWorkspace workspace;
    internal ResourceFiltersController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    readonly ContentSearchFilterBuilder searchFilters = new();
    internal TextBox resourceGame => searchFilters.GameVersion;
    internal ComboBox resourceLoader => searchFilters.Loader;
    internal ComboBox resourceSource => searchFilters.Source;
    internal ComboBox releaseChannel => searchFilters.ReleaseChannel;

    internal void BuildResourceFilters()
    {
        var filters = searchFilters.Build(workspace.Controls.SearchQuery, workspace.Controls.ContentType,
            () => workspace.LaunchCoordinator.busy, () =>
            {
                workspace.ResourceFavoritesController.ResetResourceDisplayFilters();
                workspace.ResourceBrowserController.ResetSearch();
            });
        var manage = (Grid)workspace.Controls.InstalledContent.Parent;
        var toolbar = manage.Children.OfType<WrapPanel>().First(p => Grid.GetRow(p) == 1);
        toolbar.Children.Add(FeatureUiElements.ActionButton("导入光影", async (_, _) => await workspace.LaunchCoordinator.Import("shaderpacks", "光影包|*.zip")));
        toolbar.Children.Add(FeatureUiElements.ActionButton("管理存档数据包", (_, _) =>
        {
            if (workspace.LaunchCoordinator.busy)
                return;
            var picker = new OpenFolderDialog
            {
                Title = "选择当前实例内的存档",
                InitialDirectory = System.IO.Path.Combine(workspace.LaunchCoordinator.Instance(), "saves")
            };
            if (picker.ShowDialog() != true)
                return;
            var saves = System.IO.Path.GetFullPath(System.IO.Path.Combine(workspace.LaunchCoordinator.Instance(), "saves")) + System.IO.Path.DirectorySeparatorChar;
            if (!System.IO.Path.GetFullPath(picker.FolderName).StartsWith(saves, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(System.IO.Path.Combine(picker.FolderName, "level.dat")))
            {
                InlineMessageBox.Show("请选择当前实例 saves 内含 level.dat 的存档。");
                return;
            }

            workspace.ResourceBrowserController.managedWorld = picker.FolderName;
            workspace.ResourceBrowserController.RefreshLocalContent();
        }));
        toolbar.Children.Add(FeatureUiElements.ActionButton("返回实例内容", (_, _) =>
        {
            workspace.ResourceBrowserController.managedWorld = null;
            workspace.ResourceBrowserController.UpdateTargets();
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

    internal InstanceTarget SearchTarget()
    {
        var game = resourceGame.Text.Trim();
        if (game is "" or "全部" or "跟随实例")
            game = "";
        var loader = resourceLoader.SelectedIndex < 2 ? "" : resourceLoader.SelectedItem?.ToString() ?? "";
        return new("", game, workspace.ModpackController.ContentKind == "datapack" ? "datapack" : loader);
    }
}
