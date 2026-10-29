using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Coordinates editing, persistence and inline hosting through explicit callbacks.</summary>
internal sealed class NetworkController(
    Func<NetworkSettings> getSettings,
    Action<NetworkSettings> saveSettings,
    Action<Func<Action, SettingsPageContent>> showPage,
    Action<string> showError)
{
    internal void NetworkClick(object sender, RoutedEventArgs e) => showPage(CreateNetworkPage);

    internal SettingsPageContent CreateNetworkPage(Action close)
    {
        var form = new NetworkSettingsForm(getSettings());
        form.Children.Add(FeatureUiElements.ActionButton("保存，下次启动生效", (_, _) =>
        {
            try
            {
                var value = form.Read(getSettings().LimitKibPerSecond);
                value.Validate();
                saveSettings(value);
                close();
            }
            catch (Exception ex)
            {
                showError(ex.Message);
            }
        }));
        return new(new SettingsToolView(form));
    }
}
