using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch.Front;

public partial class SettingsView : UserControl
{
    readonly SettingsViewModel model;
    bool initialized;

    public SettingsView(SettingsViewModel model)
    {
        this.model = model;
        InitializeComponent();
        DataContext = model;
        Loaded += async (_, _) =>
        {
            if (initialized)
                return;
            initialized = true;
            await model.Load();
        };
    }

    async void SaveClick(object sender, RoutedEventArgs args) => await model.Save();
    async void ReloadClick(object sender, RoutedEventArgs args) => await model.Load();
}
