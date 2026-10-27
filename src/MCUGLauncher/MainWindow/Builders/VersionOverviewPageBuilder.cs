using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class VersionOverviewPageBuilder(VersionSettingsPageContext context)
{
    readonly MainWindow host = context.Host;
    readonly InstanceDescriptor descriptor = context.Descriptor;

    public void Build()
    {
        var custom = context.Card("个性化");
        var icons = new ComboBox
        {
            ItemsSource = new[] { "自动", "选择自定义图片…" },
            SelectedIndex = File.Exists(descriptor.IconPath) ? 1 : 0
        };
        custom.Children.Add(context.Field("图标", icons));
        icons.SelectionChanged += (_, _) =>
        {
            if (host.busy)
                return;
            try
            {
                if (icons.SelectedIndex == 0)
                    descriptor.IconPath = "";
                else
                {
                    var picker = new OpenFileDialog { Filter = "版本图标|*.png;*.jpg;*.jpeg;*.ico" };
                    if (picker.ShowDialog(context.Window) != true)
                        return;
                    var image = new BitmapImage(new Uri(picker.FileName));
                    if (image.PixelWidth > 4096 || image.PixelHeight > 4096)
                        throw new IOException("图标尺寸不能超过 4096×4096。");
                    var file = Path.Combine(context.Catalog.MetadataDirectory(context.Key), "icon" + Path.GetExtension(picker.FileName));
                    File.Copy(picker.FileName, file, true);
                    descriptor.IconPath = file;
                }

                context.SaveIdentity();
                context.Render("概览");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.Window, ex.Message);
            }
        };

        var category = new ComboBox
        {
            ItemsSource = new[] { "自动", "原版游戏", "整合包", "常用版本", "测试版本", descriptor.Category }.Distinct().ToList(),
            SelectedItem = descriptor.Category
        };
        custom.Children.Add(context.Field("分类", category, context.Button("保存分类", (_, _) =>
        {
            try
            {
                descriptor.Category = category.SelectedItem as string ?? "自动";
                context.SaveIdentity();
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.Window, ex.Message);
            }
        })));

        var personalization = new WrapPanel();
        personalization.Children.Add(context.Button("修改版本名", (_, _) => EditText("修改版本名", descriptor.DisplayName, text =>
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new IOException("版本名不能为空。");
            descriptor.DisplayName = text.Trim();
        })));
        personalization.Children.Add(context.Button("修改版本描述", (_, _) => EditText("修改版本描述", descriptor.Description, text => descriptor.Description = text)));
        personalization.Children.Add(context.Button(descriptor.Favorite ? "移出收藏夹" : "加入收藏夹", (_, _) =>
        {
            try
            {
                descriptor.Favorite = !descriptor.Favorite;
                context.SaveIdentity();
                context.Render("概览");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.Window, ex.Message);
            }
        }));
        personalization.Children.Add(context.Button("自定义分类", (_, _) => EditText("自定义分类", descriptor.Category, text => descriptor.Category = string.IsNullOrWhiteSpace(text) ? "自动" : text.Trim())));
        custom.Children.Add(personalization);
        if (descriptor.Description.Length > 0)
            custom.Children.Add(MainWindow.Hint(descriptor.Description));

        var shortcuts = context.Card("快捷方式");
        var folders = new WrapPanel();
        folders.Children.Add(context.Button("存档与截图库", (_, _) =>
        {
            context.Window.Close();
            host.OpenInstanceLibrary(context.Key);
        }));
        foreach (var item in new[] { ("版本文件夹", ""), ("存档文件夹", "saves"), ("Mod 文件夹", "mods"), ("截图文件夹", "screenshots") })
            folders.Children.Add(context.Button(item.Item1, (_, _) =>
            {
                try
                {
                    host.OpenFolder(Path.Combine(context.DataDirectory(), item.Item2));
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(context.Window, ex.Message);
                }
            }));
        shortcuts.Children.Add(folders);

        var advanced = context.Card("高级管理");
        var actions = new WrapPanel();
        actions.Children.Add(context.Button("导出启动脚本", (_, _) => host.ExportScript(context.Key)));
        actions.Children.Add(context.Button("补全文件", (_, _) => host.EnqueueDownload("下载 / 修复 · " + descriptor.DisplayName, context.Key, new() { Version = descriptor.LaunchProfileId })));
        if (File.Exists(Path.Combine(context.Catalog.MetadataDirectory(context.Key), ".blocklaunch-pack.json")))
            actions.Children.Add(context.Button("升级整合包", async (_, _) => await host.UpgradePack(context.Key, context.DataDirectory(), context.Window)));
        if (File.Exists(Path.Combine(context.Catalog.MetadataDirectory(context.Key), ".blocklaunch-upgrade.json")))
            actions.Children.Add(context.Button("返回升级前实例", (_, _) => host.ReturnToOriginal(context.Key, context.Window)));
        var remove = context.Button("删除版本", (_, _) => host.UninstallInstance(context.Key));
        remove.Foreground = Brushes.Red;
        remove.BorderBrush = Brushes.Red;
        remove.BorderThickness = new Thickness(1);
        actions.Children.Add(remove);
        advanced.Children.Add(actions);
    }

    void EditText(string label, string value, Action<string> save)
    {
        if (host.busy)
            return;
        var panel = new StackPanel();
        panel.Children.Add(MainWindow.Heading(label));
        var input = new TextBox
        {
            Text = value,
            MaxLength = label.Contains("描述") ? 2000 : 100,
            AcceptsReturn = label.Contains("描述"),
            MinHeight = label.Contains("描述") ? 95 : 36
        };
        panel.Children.Add(input);
        var dialog = host.FeatureDialog(label, panel);
        dialog.Owner = context.Window;
        dialog.Height = 270;
        panel.Children.Add(context.Button("保存", (_, _) =>
        {
            try
            {
                save(input.Text);
                context.SaveIdentity();
                dialog.DialogResult = true;
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(dialog, ex.Message);
            }
        }));
        if (dialog.ShowDialog() == true)
            context.Render("概览");
    }
}
