using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BlockLaunch;

public partial class MainWindow
{
    readonly ComboBox loaderMinecraft = new() { MinWidth = 180, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly Dictionary<string, (ComboBox Versions, Button Install, TextBlock State)> loaderRows = new();
    CancellationTokenSource? loaderBrowseCancellation;

    FrameworkElement BuildLoaderOverview(Border original)
    {
        var old = (StackPanel)original.Child;
        loaderMinecraft.ItemTemplate = LoaderValueTemplate("Id");
        var page = new StackPanel();
        page.Children.Add(Heading("版本安装配置"));
        page.Children.Add(Hint("选择 Minecraft 版本和一种兼容的加载器，版本列表来自对应官方服务。"));
        var game = new StackPanel();
        game.Children.Add(new TextBlock { Text = "Minecraft 版本", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
        game.Children.Add(loaderMinecraft);
        var gameCard = FeatureCard(game);
        gameCard.Padding = new Thickness(14, 10, 14, 10);
        page.Children.Add(gameCard);
        loaderMinecraft.SelectionChanged += async (_, _) => await LoadConfigurationVersions();
        foreach (var loader in new[] { "forge", "neoforge", "fabric", "quilt" })
        {
            var title = loader switch
            {
                "forge" => "Forge",
                "neoforge" => "NeoForge",
                "fabric" => "Fabric",
                _ => "Quilt"
            };
            var row = new Grid();
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(52)
            });
            row.ColumnDefinitions.Add(new());
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(170)
            });
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(82)
            });
            var icon = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 10, 0), Child = new TextBlock { Text = title[..1], FontSize = 24, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            icon.SetResourceReference(Border.BackgroundProperty, "ThemeInputBrush");
            row.Children.Add(icon);
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 15 });
            var state = new TextBlock { Text = "请选择 Minecraft 版本", FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 8, 0) };
            state.SetResourceReference(TextBlock.ForegroundProperty, "UiSecondaryTextBrush");
            text.Children.Add(state);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            var versions = new ComboBox { IsEnabled = false, ItemTemplate = LoaderValueTemplate("Label"), Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(versions, 2);
            row.Children.Add(versions);
            var install = ActionButton("安装", (_, _) => QueueConfiguredLoader(loader));
            install.Style = (Style)FindResource("Primary");
            install.IsEnabled = false;
            install.Margin = new Thickness(0);
            install.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(install, 3);
            row.Children.Add(install);
            loaderRows[loader] = (versions, install, state);
            var card = FeatureCard(row);
            card.Padding = new Thickness(14, 12, 14, 12);
            card.Margin = new Thickness(0, 0, 0, 8);
            page.Children.Add(card);
        }
        page.Children.Add(Hint("每次安装一种加载器，生成独立版本。Forge、NeoForge、Fabric 和 Quilt 不能混合安装。"));
        var localInstaller = old.Children.OfType<Button>().Single();
        old.Children.Remove(localInstaller);
        var advanced = new StackPanel();
        advanced.Children.Add(localInstaller);
        advanced.Children.Add(Hint("本地官方安装器的目标目录应设为游戏根目录，完成后刷新版本。"));
        page.Children.Add(new Expander { Header = "高级 · 本地官方安装器", Content = advanced, Margin = new Thickness(0, 10, 0, 0) });
        return page;
    }

    void PrepareLoaderConfiguration()
    {
        var selected = loaderMinecraft.SelectedItem as VersionRow;
        var available = rows.Where(r => r.Type is "release" or "snapshot").ToList();
        loaderMinecraft.ItemsSource = available;
        loaderMinecraft.SelectedItem = available.FirstOrDefault(r => r.Id == downloadVersion) ?? available.FirstOrDefault(r => r.Id == selected?.Id) ?? available.FirstOrDefault();
    }

    static DataTemplate LoaderValueTemplate(string property)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(property));
        text.SetBinding(FrameworkElement.ToolTipProperty, new Binding(property));
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        return new DataTemplate { VisualTree = text };
    }

    async Task LoadConfigurationVersions()
    {
        loaderBrowseCancellation?.Cancel();
        loaderBrowseCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        loaderBrowseCancellation = cancellation;
        var token = cancellation.Token;
        foreach (var item in loaderRows.Values)
        {
            item.Versions.ItemsSource = null;
            item.Versions.IsEnabled = false;
            item.Install.IsEnabled = false;
            item.State.Text = "正在读取兼容版本…";
        }
        if (loaderMinecraft.SelectedItem is not VersionRow minecraft)
            return;
        await Task.WhenAll(loaderRows.Select(async pair =>
        {
            var loader = pair.Key;
            var controls = pair.Value;
            try
            {
                List<LoaderRelease> releases;
                if (loader is "forge" or "neoforge")
                    releases = await LoaderCatalog.Get(http, minecraft.Id, loader, token);
                else
                {
                    var endpoint = loader == "fabric" ? "https://meta.fabricmc.net/v2/versions/loader/" : "https://meta.quiltmc.org/v3/versions/loader/";
                    using var document = JsonDocument.Parse(await http.GetStringAsync(endpoint + Uri.EscapeDataString(minecraft.Id), token));
                    releases = document.RootElement.EnumerateArray().Select(e => e.GetProperty("loader")).Select(e =>
                    {
                        var version = e.GetProperty("version").GetString()!;
                        var preview = e.TryGetProperty("stable", out var stable) && stable.ValueKind == JsonValueKind.False || version.Contains("beta") || version.Contains("alpha");
                        return new LoaderRelease(version, version + (preview ? " · 测试版" : ""));
                    }).DistinctBy(r => r.Version).OrderBy(r => r.Label.Contains("测试版")).ToList();
                }
                token.ThrowIfCancellationRequested();
                controls.Versions.ItemsSource = releases;
                controls.Versions.SelectedIndex = releases.Count > 0 ? 0 : -1;
                controls.Versions.IsEnabled = releases.Count > 0;
                controls.Install.IsEnabled = releases.Count > 0;
                controls.State.Text = releases.Count > 0 ? $"{releases.Count} 个可用版本 · Minecraft {minecraft.Id}" : "没有兼容版本";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    controls.State.Text = "读取失败，重新选择游戏版本可重试";
                    controls.State.ToolTip = ex.Message;
                }
            }
        }));
    }

    void QueueConfiguredLoader(string loader)
    {
        if (loaderMinecraft.SelectedItem is not VersionRow minecraft || loaderRows[loader].Versions.SelectedItem is not LoaderRelease release)
            return;
        try
        {
            RequireStopped();
            EnqueueDownload(loader + " · " + minecraft.Id, minecraft.Id, new()
            {
                Version = minecraft.Id,
                Loader = loader,
                LoaderVersion = release.Version
            });
        }
        catch (Exception ex) { InlineMessageBox.Show(ex.Message); }
    }
}
