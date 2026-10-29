using System.Windows;
using System.Windows.Controls;
using System.Net.Http;

namespace BlockLaunch;

internal sealed record ContentUpdateActions(
    Window Owner,
    Func<bool> IsBusy,
    Func<HttpClient> GetHttp,
    Func<CancellationToken> GetToken,
    Func<Func<Task>, string, Task> Run,
    Action RequireStopped,
    Func<string, StackPanel, InlineDialog> FeatureDialog,
    Action<string, string, DownloadRequest> EnqueueDownload,
    Func<InstanceTarget> Target,
    Func<string> ManagedDirectory,
    Action RefreshLocalContent,
    Action<string> Log);

internal sealed class ContentUpdatesController(ContentUpdateActions host)
{
    public async Task OpenContentUpdates(string directory, string key, string minecraft, string loader, string? project = null, object? owner = null)
    {
        List<ContentUpdate>? updates = null;
        await host.Run(async () =>
        {
            host.RequireStopped();
            updates = await ContentUpdates.Check(new ContentService(host.GetHttp(), host.GetToken()), directory, minecraft, loader, host.GetToken());
        }, "检查兼容更新");
        if (updates == null)
            return;
        ContentUpdateSelectionView selection = null!;
        selection = new ContentUpdateSelectionView(
            host.FeatureDialog,
            FeatureUiElements.ActionButton,
            owner ?? host.Owner,
            updates,
            project,
            async roots =>
        {
            ContentUpdatePreview? preview = null;
            await host.Run(async () =>
            {
                host.RequireStopped();
                preview = await ContentUpdates.Preview(new ContentService(host.GetHttp(), host.GetToken()), directory, roots, minecraft, loader, host.GetToken());
            }, "解析更新与依赖");
            if (preview == null)
                return;

            var confirmation = new ContentUpdatePreviewView(
                host.FeatureDialog,
                FeatureUiElements.ActionButton,
                selection.Dialog,
                preview);
            if (!confirmation.Show())
                return;
            host.EnqueueDownload("批量内容更新 · " + preview.Changes.Count + " 项", key, new()
            {
                Kind = "content",
                PreparedPlan = preview.Plan,
                ContentFingerprint = preview.Fingerprint,
                Destination = directory,
                GameVersion = minecraft,
                Loader = loader
            });
            selection.Close();
        },
            () => host.IsBusy());
        selection.Show();
    }

    public async Task CheckAllUpdates()
    {
        if (host.IsBusy())
            return;
        try
        {
            var target = host.Target();
            await OpenContentUpdates(host.ManagedDirectory(), target.Version, target.GameVersion, target.Loader);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    public async Task RestoreContent(string directory)
    {
        if (host.IsBusy() || InlineMessageBox.Show("回退最近一次内容安装/更新？\n更新后手动改动的文件会阻止回退。存档不受影响。", "内容回退", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            return;
        await host.Run(async () =>
        {
            host.RequireStopped();
            var token = host.GetToken();
            await Task.Run(() => ContentTransaction.RestoreLatest(directory, token), token);
            host.RefreshLocalContent();
            host.Log("最近一次内容操作已回退。");
        }, "回退内容更新");
    }
    internal async void CheckAllUpdatesClick(object sender, RoutedEventArgs e) => await CheckAllUpdates();
    internal async void RestoreContentClick(object sender, RoutedEventArgs e) => await RestoreContent(host.ManagedDirectory());
}
