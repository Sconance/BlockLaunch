using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;
internal sealed class AppearanceController
{
    readonly LauncherWorkspace workspace;
    internal AppearanceController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal BitmapImage? appearanceImage;
    internal string appearanceFile = "";
    internal DateTime appearanceModified;
    internal static string AccentColor(string name) => name switch
    {
        "青绿" => "#147D78",
        "石墨" => "#526375",
        "紫罗兰" => "#7658A8",
        _ => "#006DB8"
    };
    internal void ApplyBackground()
    {
        workspace.Window.Background = new SolidColorBrush(workspace.LaunchCoordinator.prefs.DarkMode ? Color.FromRgb(23, 29, 42) : Color.FromRgb(243, 247, 249));
        if (workspace.Window.Content is not Grid root)
            return;
        root.Background = Brushes.Transparent;
        if (!File.Exists(workspace.LaunchCoordinator.prefs.BackgroundPath))
        {
            appearanceImage = null;
            appearanceFile = "";
            return;
        }

        try
        {
            var modified = File.GetLastWriteTimeUtc(workspace.LaunchCoordinator.prefs.BackgroundPath);
            if (appearanceImage == null || appearanceFile != workspace.LaunchCoordinator.prefs.BackgroundPath || modified != appearanceModified)
            {
                if (new FileInfo(workspace.LaunchCoordinator.prefs.BackgroundPath).Length > 40 * 1024 * 1024)
                    throw new IOException("背景图片不能超过 40 MB。");
                using var stream = File.OpenRead(workspace.LaunchCoordinator.prefs.BackgroundPath);
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
                appearanceFile = workspace.LaunchCoordinator.prefs.BackgroundPath;
                appearanceModified = modified;
            }

            root.Background = new ImageBrush(appearanceImage)
            {
                Stretch = workspace.LaunchCoordinator.prefs.BackgroundFit switch
                {
                    "完整显示" => Stretch.Uniform,
                    "拉伸" => Stretch.Fill,
                    _ => Stretch.UniformToFill
                },
                Opacity = double.IsFinite(workspace.LaunchCoordinator.prefs.BackgroundOpacity) ? Math.Clamp(workspace.LaunchCoordinator.prefs.BackgroundOpacity, 0, 0.65) : 0.22
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.IO.FileFormatException)
        {
            appearanceImage = null;
            appearanceFile = "";
        }
    }

    internal void OpenAppearance() => SettingsDialogPresenter.Show("界面设置", workspace.Window,
        _ => CreateAppearancePage(), workspace.ExperienceThemeController.ConfigureUtilityWindow);

    internal SettingsPageContent CreateAppearancePage() => new AppearanceSettingsPage(
        () => workspace.LaunchCoordinator.prefs,
        () => workspace.LaunchCoordinator.settings.Save(workspace.LaunchCoordinator.prefs),
        () =>
        {
            workspace.InstanceWorkspaceController.ApplyPersonalization();
            workspace.ExperienceThemeController.ThemeWindow(workspace.Window);
        }, picker => picker.ShowDialog(workspace.Window)).Build();
}
