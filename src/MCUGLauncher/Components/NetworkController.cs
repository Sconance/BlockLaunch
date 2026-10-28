using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Coordinates editing, persistence and inline hosting through explicit callbacks.</summary>
internal sealed class NetworkController(
    Func<NetworkSettings> getSettings,
    Action<NetworkSettings> saveSettings,
    Func<string, StackPanel, InlineDialog> createDialog)
{
    internal void NetworkClick(object sender, RoutedEventArgs e)
    {
        var form = new NetworkSettingsForm(getSettings());
        var dialog = createDialog("下载与网络设置", form);
        form.Children.Add(FeatureUiElements.ActionButton("保存，下次启动生效", (_, _) =>
        {
            try
            {
                var value = form.Read(getSettings().LimitKibPerSecond);
                value.Validate();
                saveSettings(value);
                dialog.DialogResult = true;
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(dialog, ex.Message);
            }
        }));
        dialog.ShowDialog();
    }
}
