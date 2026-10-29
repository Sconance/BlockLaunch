using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;

public partial class MainWindow
{
    BitmapImage? appearanceImage;
    string appearanceFile = "";
    DateTime appearanceModified;

    static string AccentColor(string name) => name switch
    {
        "青绿" => "#147D78",
        "石墨" => "#526375",
        "紫罗兰" => "#7658A8",
        _ => "#006DB8"
    };

    internal void ApplyBackground()
    {
        Background = new SolidColorBrush(prefs.DarkMode ? Color.FromRgb(23, 29, 42) : Color.FromRgb(243, 247, 249));
        if (Content is not Grid root)
            return;
        root.Background = Brushes.Transparent;
        if (!File.Exists(prefs.BackgroundPath))
        {
            appearanceImage = null;
            appearanceFile = "";
            return;
        }

        try
        {
            var modified = File.GetLastWriteTimeUtc(prefs.BackgroundPath);
            if (appearanceImage == null || appearanceFile != prefs.BackgroundPath || modified != appearanceModified)
            {
                if (new FileInfo(prefs.BackgroundPath).Length > 40 * 1024 * 1024)
                    throw new IOException("背景图片不能超过 40 MB。");
                using var stream = File.OpenRead(prefs.BackgroundPath);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                var frame = decoder.Frames[0];
                if (frame.PixelWidth > 32768 || frame.PixelHeight > 32768 || (long)frame.PixelWidth * frame.PixelHeight > 100_000_000)
                    throw new IOException("背景图片尺寸过大。");
                stream.Position = 0;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = Math.Min(2560, frame.PixelWidth);
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                appearanceImage = bitmap;
                appearanceFile = prefs.BackgroundPath;
                appearanceModified = modified;
            }

            root.Background = new ImageBrush(appearanceImage)
            {
                Stretch = prefs.BackgroundFit switch
                {
                    "完整显示" => Stretch.Uniform,
                    "拉伸" => Stretch.Fill,
                    _ => Stretch.UniformToFill
                },
                Opacity = double.IsFinite(prefs.BackgroundOpacity) ? Math.Clamp(prefs.BackgroundOpacity, 0, 0.65) : 0.22
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.IO.FileFormatException)
        {
            appearanceImage = null;
            appearanceFile = "";
        }
    }

    void OpenAppearance()
    {
        var body = new StackPanel();
        body.Children.Add(Heading("界面与背景"));
        body.Children.Add(Hint("Tab 切换焦点，Esc 返回。背景图片缓存在内存中；不会在滚动或下载时反复加载。"));
        var dark = new CheckBox
        {
            Content = "深色模式",
            IsChecked = prefs.DarkMode
        };
        body.Children.Add(dark);
        body.Children.Add(Hint("强调色"));
        var accent = new ComboBox
        {
            ItemsSource = new[] {
                "地大蓝",
                "青绿",
                "石墨",
                "紫罗兰"
            },
            SelectedItem = prefs.Accent == "蓝色" ? "地大蓝" : prefs.Accent
        };
        if (accent.SelectedIndex < 0)
            accent.SelectedIndex = 0;
        body.Children.Add(accent);
        var choices = (prefs.BackgroundImages ?? []).Where(File.Exists).Append(prefs.BackgroundPath).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
        body.Children.Add(Hint("背景图片 · 可添加多张，手动切换；不自动轮播"));
        var images = new ComboBox
        {
            ItemsSource = choices,
            SelectedItem = prefs.BackgroundPath,
            ToolTip = "选择已添加的本地背景"
        };
        body.Children.Add(images);
        var fit = new ComboBox
        {
            ItemsSource = new[] {
                "裁剪填充",
                "完整显示",
                "拉伸"
            },
            SelectedItem = prefs.BackgroundFit
        };
        if (fit.SelectedIndex < 0)
            fit.SelectedIndex = 0;
        body.Children.Add(Hint("图片填充方式"));
        body.Children.Add(fit);
        var opacity = new Slider
        {
            Minimum = 0,
            Maximum = 0.65,
            Value = double.IsFinite(prefs.BackgroundOpacity) ? Math.Clamp(prefs.BackgroundOpacity, 0, 0.65) : 0.22,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = false,
            ToolTip = "背景图片强度；越低遮罩越强"
        };
        var amount = Hint($"图片强度 {opacity.Value:P0}（降低可增强文字可读性）");
        opacity.ValueChanged += (_, _) => amount.Text = $"图片强度 {opacity.Value:P0}（降低可增强文字可读性）";
        body.Children.Add(amount);
        body.Children.Add(opacity);
        var status = Hint("点击应用后保存；取消或返回不会修改设置。");
        body.Children.Add(status);
        var dialog = FeatureDialog("界面设置", body);
        body.Children.Add(ActionButton("添加本地背景图片", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "背景图片|*.png;*.jpg;*.jpeg",
                Multiselect = true
            };
            if (picker.ShowDialog(dialog) != true)
                return;
            choices = choices.Concat(picker.FileNames).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
            images.ItemsSource = choices;
            images.SelectedItem = picker.FileNames[0];
        }));
        body.Children.Add(ActionButton("恢复默认背景", (_, _) =>
        {
            images.SelectedIndex = -1;
            opacity.Value = 0.22;
            fit.SelectedIndex = 0;
            status.Text = "已选择纯色背景，点击应用保存。";
        }));
        body.Children.Add(ActionButton("应用", (_, _) =>
        {
            try
            {
                launcherSettings.SaveAppearance(prefs, dark.IsChecked == true, accent.SelectedItem as string ?? "地大蓝",
                    images.SelectedItem as string ?? "", opacity.Value, fit.SelectedItem as string ?? "裁剪填充", choices);
                ApplyPersonalization();
                ThemeWindow(this);
                status.Text = "外观已保存并应用，无需重启。";
            }
            catch (Exception ex)
            {
                status.Text = "未能应用外观：" + ex.Message;
            }
        }));
        dialog.ShowDialog();
    }
}
