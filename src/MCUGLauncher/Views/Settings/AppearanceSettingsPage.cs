using System.IO;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Appearance form shared by settings and utility entry points.</summary>
internal sealed class AppearanceSettingsPage(Func<Preferences> preferences, Action save, Action apply,
    Func<OpenFileDialog, bool?> chooseFiles)
{
    internal SettingsPageContent Build()
    {
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("界面与背景"));
        body.Children.Add(FeatureUiElements.Hint("Tab 切换焦点，Esc 返回。背景图片缓存在内存中；不会在滚动或下载时反复加载。"));
        var dark = new CheckBox
        {
            Content = "深色模式",
            IsChecked = preferences().DarkMode
        };
        body.Children.Add(dark);
        body.Children.Add(FeatureUiElements.Hint("强调色"));
        var accent = new ComboBox
        {
            ItemsSource = new[]
            {
                "地大蓝",
                "青绿",
                "石墨",
                "紫罗兰"
            },
            SelectedItem = preferences().Accent == "蓝色" ? "地大蓝" : preferences().Accent
        };
        if (accent.SelectedIndex < 0)
            accent.SelectedIndex = 0;
        body.Children.Add(accent);
        var choices = (preferences().BackgroundImages ?? []).Where(File.Exists).Append(preferences().BackgroundPath).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
        body.Children.Add(FeatureUiElements.Hint("背景图片 · 可添加多张，手动切换；不自动轮播"));
        var images = new ComboBox
        {
            ItemsSource = choices,
            SelectedItem = preferences().BackgroundPath,
            ToolTip = "选择已添加的本地背景"
        };
        body.Children.Add(images);
        var fit = new ComboBox
        {
            ItemsSource = new[]
            {
                "裁剪填充",
                "完整显示",
                "拉伸"
            },
            SelectedItem = preferences().BackgroundFit
        };
        if (fit.SelectedIndex < 0)
            fit.SelectedIndex = 0;
        body.Children.Add(FeatureUiElements.Hint("图片填充方式"));
        body.Children.Add(fit);
        var opacity = new Slider
        {
            Minimum = 0,
            Maximum = 0.65,
            Value = double.IsFinite(preferences().BackgroundOpacity) ? Math.Clamp(preferences().BackgroundOpacity, 0, 0.65) : 0.22,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = false,
            ToolTip = "背景图片强度；越低遮罩越强"
        };
        var amount = FeatureUiElements.Hint($"图片强度 {opacity.Value:P0}（降低可增强文字可读性）");
        opacity.ValueChanged += (_, _) => amount.Text = $"图片强度 {opacity.Value:P0}（降低可增强文字可读性）";
        body.Children.Add(amount);
        body.Children.Add(opacity);
        var status = FeatureUiElements.Hint("点击应用后保存；取消或返回不会修改设置。");
        body.Children.Add(status);
        body.Children.Add(FeatureUiElements.ActionButton("添加本地背景图片", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "背景图片|*.png;*.jpg;*.jpeg",
                Multiselect = true
            };
            if (chooseFiles(picker) != true)
                return;
            choices = choices.Concat(picker.FileNames).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
            images.ItemsSource = choices;
            images.SelectedItem = picker.FileNames[0];
        }));
        body.Children.Add(FeatureUiElements.ActionButton("恢复默认背景", (_, _) =>
        {
            images.SelectedIndex = -1;
            opacity.Value = 0.22;
            fit.SelectedIndex = 0;
            status.Text = "已选择纯色背景，点击应用保存。";
        }));
        body.Children.Add(FeatureUiElements.ActionButton("应用", (_, _) =>
        {
            try
            {
                preferences().DarkMode = dark.IsChecked == true;
                preferences().Accent = accent.SelectedItem as string ?? "地大蓝";
                preferences().BackgroundPath = images.SelectedItem as string ?? "";
                preferences().BackgroundImages = choices;
                preferences().BackgroundFit = fit.SelectedItem as string ?? "裁剪填充";
                preferences().BackgroundOpacity = opacity.Value;
                save();
                apply();
                status.Text = "外观已保存并应用，无需重启。";
            }
            catch (Exception ex)
            {
                status.Text = "未能应用外观：" + ex.Message;
            }
        }));
        return new(new SettingsToolView(body));
    }
}
