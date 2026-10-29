using System.Net.Http;
using System.Text.Json;
using System.Windows.Controls;

namespace BlockLaunch;

public sealed record LoaderSelection(string Loader, string Minecraft, string Version);

/// <summary>Owns loader catalogue browsing and cancellation; installation is an application callback.</summary>
public sealed class LoaderConfigurationPresenter : IDisposable
{
    readonly LoaderConfigurationView view;
    readonly Func<HttpClient> httpProvider;
    readonly Action<LoaderSelection> queue;
    CancellationTokenSource? loaderBrowseCancellation;

    public LoaderConfigurationPresenter(LoaderConfigurationView view, Func<HttpClient> httpProvider, Action<LoaderSelection> queue)
    {
        this.view = view;
        this.httpProvider = httpProvider;
        this.queue = queue;
        view.Minecraft.SelectionChanged += OnMinecraftChanged;
        view.InstallRequested += QueueSelection;
    }

    async void OnMinecraftChanged(object sender, SelectionChangedEventArgs e) => await LoadVersions();
    public void Prepare(IReadOnlyList<VersionRow> rows, string? downloadVersion)
    {
        var selected = view.Minecraft.SelectedItem as VersionRow;
        var available = rows.Where(r => r.Type is "release" or "snapshot").ToList();
        view.Minecraft.ItemsSource = available;
        view.Minecraft.SelectedItem = available.FirstOrDefault(r => r.Id == downloadVersion) ?? available.FirstOrDefault(r => r.Id == selected?.Id) ?? available.FirstOrDefault();
    }

    public async Task LoadVersions()
    {
        loaderBrowseCancellation?.Cancel();
        loaderBrowseCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        loaderBrowseCancellation = cancellation;
        var token = cancellation.Token;
        foreach (var item in view.Rows.Values)
        {
            item.Versions.ItemsSource = null;
            item.Versions.IsEnabled = false;
            item.Install.IsEnabled = false;
            item.State.Text = "正在读取兼容版本…";
        }
        if (view.Minecraft.SelectedItem is not VersionRow minecraft)
            return;
        await Task.WhenAll(view.Rows.Select(async pair =>
        {
            var loader = pair.Key;
            var controls = pair.Value;
            try
            {
                List<LoaderRelease> releases;
                if (loader is "forge" or "neoforge")
                    releases = await LoaderCatalog.Get(httpProvider(), minecraft.Id, loader, token);
                else
                {
                    var endpoint = loader == "fabric" ? "https://meta.fabricmc.net/v2/versions/loader/" : "https://meta.quiltmc.org/v3/versions/loader/";
                    using var document = JsonDocument.Parse(await httpProvider().GetStringAsync(endpoint + Uri.EscapeDataString(minecraft.Id), token));
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


    public void QueueSelection(string loader)
    {
        if (view.Minecraft.SelectedItem is VersionRow minecraft && view.Rows[loader].Versions.SelectedItem is LoaderRelease release)
            queue(new(loader, minecraft.Id, release.Version));
    }

    public void Dispose()
    {
        view.Minecraft.SelectionChanged -= OnMinecraftChanged;
        view.InstallRequested -= QueueSelection;
        loaderBrowseCancellation?.Cancel();
        loaderBrowseCancellation?.Dispose();
    }
}
