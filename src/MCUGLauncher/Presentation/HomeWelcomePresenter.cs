using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlockLaunch;

/// <summary>Configures the default welcome surface without reading or changing preferences.</summary>
internal static class HomeWelcomePresenter
{
    internal static void Configure(Grid home, Border accountCard, string backgroundPath)
    {
        var banner = home.Children.OfType<Border>().FirstOrDefault(b => b != accountCard && Grid.GetColumn(b) == 2);
        if (banner == null || File.Exists(backgroundPath))
            return;
        banner.SetResourceReference(Border.BackgroundProperty, "ThemeSurfaceBrush");
        banner.CornerRadius = new CornerRadius(10);
        banner.BorderBrush = new SolidColorBrush(Color.FromRgb(214, 225, 232));
        banner.BorderThickness = new Thickness(1);
        banner.Child = new HomeWelcomeView(Brand.Name);
    }
}
