using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class LaunchOptionsDialog(
    MainWindow host,
    string settingsDirectory,
    Action applyPersonalization,
    Action openAppearance)
{
    public void Show(InstanceDescriptor? descriptor)
    {
        if (host.busy)
            return;
        var original = descriptor?.Launch ?? host.prefs.Launch;
        var value = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(original))!;
        var body = new StackPanel();
        body.Children.Add(MainWindow.Heading(descriptor == null ? "全局启动与个性化" : "版本设置 · " + descriptor.DisplayName));
        body.Children.Add(MainWindow.Hint("版本独立设置保存后覆盖全局启动选项。共享根目录会共享 mods/config/saves；独立实例为推荐默认值。"));
        var options = new LaunchOptionsForm(value);
        body.Children.Add(options.Panel);
        var dialog = host.FeatureDialog("启动设置", body);
        if (descriptor == null)
            AddPersonalization(body);
        else
            body.Children.Add(MainWindow.ActionButton("恢复跟随全局设置", (_, _) =>
            {
                try
                {
                    host.RequireStopped();
                    descriptor.Launch = null;
                    new InstanceCatalog(host.prefs.Root).Save(descriptor);
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(ex.Message);
                }
            }));
        body.Children.Add(MainWindow.ActionButton("保存设置", (_, _) =>
        {
            try
            {
                host.RequireStopped();
                var updated = options.Read();
                updated.Validate();
                if (descriptor != null)
                {
                    descriptor.Launch = updated;
                    new InstanceCatalog(host.prefs.Root).Save(descriptor);
                }
                else
                {
                    host.prefs.Launch = updated;
                    host.prefs.Ram = updated.RamMb;
                    host.prefs.Java = updated.Java;
                    host.prefs.Width = updated.Width;
                    host.prefs.Height = updated.Height;
                    host.Memory.Text = host.prefs.Ram.ToString();
                    host.JavaPath.Text = host.prefs.Java;
                    host.GameWidth.Text = host.prefs.Width.ToString();
                    host.GameHeight.Text = host.prefs.Height.ToString();
                    host.settings.Save(host.prefs);
                }

                dialog.Close();
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message, "设置未保存");
            }
        }));
        dialog.ShowDialog();
    }

    void AddPersonalization(StackPanel body)
    {
        body.Children.Add(MainWindow.Heading("个性化"));
        body.Children.Add(MainWindow.ActionButton("背景填充、透明度与主题", (_, _) => openAppearance()));
        var colors = new ComboBox
        {
            ItemsSource = new[] {
                "地大蓝",
                "青绿",
                "石墨",
                "紫罗兰"
            },
            SelectedItem = "地大蓝"
        };
        body.Children.Add(colors);
        body.Children.Add(MainWindow.ActionButton("应用主题色", (_, _) =>
        {
            host.prefs.Accent = colors.SelectedItem as string ?? "蓝色";
            host.settings.Save(host.prefs);
            applyPersonalization();
        }));
        body.Children.Add(MainWindow.ActionButton("选择自定义背景", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "背景图片|*.png;*.jpg;*.jpeg"
            };
            if (picker.ShowDialog() != true)
                return;
            host.prefs.BackgroundPath = picker.FileName;
            host.settings.Save(host.prefs);
            applyPersonalization();
        }));
        body.Children.Add(MainWindow.ActionButton("恢复默认背景", (_, _) =>
        {
            host.prefs.BackgroundPath = "";
            host.settings.Save(host.prefs);
            applyPersonalization();
        }));
        body.Children.Add(MainWindow.Heading("离线皮肤"));
        body.Children.Add(MainWindow.Hint("本地 PNG 通过资源包覆盖默认玩家皮肤，需要在游戏内启用“BlockLaunch Offline Skin”。只影响本机显示，不提供服务器皮肤认证，其他使用默认皮肤的玩家也可能显示相同贴图。"));
        body.Children.Add(MainWindow.ActionButton("选择离线皮肤 PNG", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "Minecraft 皮肤|*.png"
            };
            if (picker.ShowDialog() != true)
                return;
            try
            {
                var image = new BitmapImage(new Uri(picker.FileName));
                if (image.PixelWidth != 64 || image.PixelHeight is not (32 or 64))
                    throw new IOException("请选择 64×64 或 64×32 皮肤。");
                var file = Path.Combine(settingsDirectory, "offline-skin.png");
                File.Copy(picker.FileName, file, true);
                host.prefs.OfflineSkin = file;
                host.settings.Save(host.prefs);
                applyPersonalization();
                InlineMessageBox.Show("离线启动时会生成皮肤资源包，请在游戏内启用。");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(ex.Message);
            }
        }));
        body.Children.Add(MainWindow.ActionButton("停止生成离线皮肤资源包", (_, _) =>
        {
            host.prefs.OfflineSkin = "";
            host.settings.Save(host.prefs);
            applyPersonalization();
            InlineMessageBox.Show("已停止生成；已有资源包可在游戏内停用。");
        }));
    }
}
