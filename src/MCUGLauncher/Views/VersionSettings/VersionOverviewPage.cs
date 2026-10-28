using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed record VersionOverviewActions(
    Func<bool> IsBusy,
    Action OpenLibrary,
    Action<string> OpenFolder,
    Action ExportScript,
    Action<string, string, DownloadRequest> EnqueueDownload,
    Func<string, Task> UpgradePack,
    Action ReturnToOriginal,
    Action Uninstall);

/// <summary>Builds the 概览 page using per-instance data and explicit operations.</summary>
internal sealed class VersionOverviewPage(VersionSettingsPageContext context, VersionOverviewActions operations, VersionIdentityEditor editor)
{
    internal void Build()
    {
        var custom = context.View.Card("个性化");
        var icons = new ComboBox
        {
            ItemsSource = new[]
            {
                        "自动",
                        "选择自定义图片…"
                    },
            SelectedIndex = File.Exists(context.Descriptor.IconPath) ? 1 : 0
        };
        custom.Children.Add(VersionSettingsView.Field("图标", icons));
        icons.SelectionChanged += (_, _) =>
        {
            if (operations.IsBusy())
                return;
            try
            {
                if (icons.SelectedIndex == 0)
                    context.Descriptor.IconPath = "";
                else
                {
                    var picker = new OpenFileDialog
                    {
                        Filter = "版本图标|*.png;*.jpg;*.jpeg;*.ico"
                    };
                    if (picker.ShowDialog(context.View.Window) != true)
                        return;
                    var image = new BitmapImage(new Uri(picker.FileName));
                    if (image.PixelWidth > 4096 || image.PixelHeight > 4096)
                        throw new IOException("图标尺寸不能超过 4096×4096。");
                    var file = Path.Combine(context.Catalog.MetadataDirectory(context.Key), "icon" + Path.GetExtension(picker.FileName));
                    File.Copy(picker.FileName, file, true);
                    context.Descriptor.IconPath = file;
                }

                editor.Save();
                context.Render("概览");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.View.Window, ex.Message);
            }
        };
        var category = new ComboBox
        {
            ItemsSource = new[]
            {
                        "自动",
                        "原版游戏",
                        "整合包",
                        "常用版本",
                        "测试版本",
                        context.Descriptor.Category
                    }.Distinct().ToList(),
            SelectedItem = context.Descriptor.Category
        };
        custom.Children.Add(VersionSettingsView.Field("分类", category, VersionSettingsView.Button("保存分类", (_, _) =>
        {
            try
            {
                context.Descriptor.Category = category.SelectedItem as string ?? "自动";
                editor.Save();
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.View.Window, ex.Message);
            }
        })));
        var personalization = new WrapPanel();
        personalization.Children.Add(VersionSettingsView.Button("修改版本名", (_, _) => editor.Edit("修改版本名", context.Descriptor.DisplayName, text =>
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new IOException("版本名不能为空。");
            context.Descriptor.DisplayName = text.Trim();
        })));
        personalization.Children.Add(VersionSettingsView.Button("修改版本描述", (_, _) => editor.Edit("修改版本描述", context.Descriptor.Description, text => context.Descriptor.Description = text)));
        personalization.Children.Add(VersionSettingsView.Button(context.Descriptor.Favorite ? "移出收藏夹" : "加入收藏夹", (_, _) =>
        {
            try
            {
                context.Descriptor.Favorite = !context.Descriptor.Favorite;
                editor.Save();
                context.Render("概览");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.View.Window, ex.Message);
            }
        }));
        personalization.Children.Add(VersionSettingsView.Button("自定义分类", (_, _) => editor.Edit("自定义分类", context.Descriptor.Category, text => context.Descriptor.Category = string.IsNullOrWhiteSpace(text) ? "自动" : text.Trim())));
        custom.Children.Add(personalization);
        if (context.Descriptor.Description.Length > 0)
            custom.Children.Add(FeatureUiElements.Hint(context.Descriptor.Description));
        var shortcuts = context.View.Card("快捷方式");
        var folders = new WrapPanel();
        folders.Children.Add(VersionSettingsView.Button("存档与截图库", (_, _) =>
        {
            context.View.Window.Close();
            operations.OpenLibrary();
        }));
        foreach (var item in new[]
        {
                    ("版本文件夹", ""),
                    ("存档文件夹", "saves"),
                    ("Mod 文件夹", "mods"),
                    ("截图文件夹", "screenshots")
                }

        )
            folders.Children.Add(VersionSettingsView.Button(item.Item1, (_, _) =>
            {
                try
                {
                    operations.OpenFolder(Path.Combine(context.DataDirectory(), item.Item2));
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(context.View.Window, ex.Message);
                }
            }));
        shortcuts.Children.Add(folders);
        var advanced = context.View.Card("高级管理");
        var actions = new WrapPanel();
        actions.Children.Add(VersionSettingsView.Button("导出启动脚本", (_, _) => operations.ExportScript()));
        actions.Children.Add(VersionSettingsView.Button("补全文件", (_, _) => operations.EnqueueDownload("下载 / 修复 · " + context.Descriptor.DisplayName, context.Key, new() { Version = context.Descriptor.LaunchProfileId })));
        if (File.Exists(Path.Combine(context.Catalog.MetadataDirectory(context.Key), ".blocklaunch-pack.json")))
            actions.Children.Add(VersionSettingsView.Button("升级整合包", async (_, _) => await operations.UpgradePack(context.DataDirectory())));
        if (File.Exists(Path.Combine(context.Catalog.MetadataDirectory(context.Key), ".blocklaunch-upgrade.json")))
            actions.Children.Add(VersionSettingsView.Button("返回升级前实例", (_, _) => operations.ReturnToOriginal()));
        var remove = VersionSettingsView.Button("删除版本", (_, _) => operations.Uninstall());
        remove.Foreground = Brushes.Red;
        remove.BorderBrush = Brushes.Red;
        remove.BorderThickness = new Thickness(1);
        actions.Children.Add(remove);
        advanced.Children.Add(actions);

    }
}
