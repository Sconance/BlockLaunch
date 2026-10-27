using System.Windows;

namespace BlockLaunch;

internal sealed class ContentUpdateController(MainWindow host)
{
    public async Task Open(string directory, string key, string minecraft, string loader, string? project = null, object? owner = null)
    {
        List<ContentUpdate>? updates = null;
        await host.Run(async () =>
        {
            host.RequireStopped();
            updates = await ContentUpdates.Check(new ContentService(host.http, host.CurrentToken), directory, minecraft, loader, host.CurrentToken);
        }, "检查兼容更新");
        if (updates == null)
            return;
        ContentUpdateSelectionView selection = null!;
        selection = new ContentUpdateSelectionView(
            host.FeatureDialog,
            MainWindow.ActionButton,
            owner ?? host,
            updates,
            project,
            async roots =>
        {
            ContentUpdatePreview? preview = null;
            await host.Run(async () =>
            {
                host.RequireStopped();
                preview = await ContentUpdates.Preview(new ContentService(host.http, host.CurrentToken), directory, roots, minecraft, loader, host.CurrentToken);
            }, "解析更新与依赖");
            if (preview == null)
                return;

            var confirmation = new ContentUpdatePreviewView(
                host.FeatureDialog,
                MainWindow.ActionButton,
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
            () => host.busy);
        selection.Show();
    }

    public async Task CheckAll()
    {
        if (host.busy)
            return;
        try
        {
            var target = host.Target();
            await Open(host.ManagedDirectory(), target.Version, target.GameVersion, target.Loader);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message);
        }
    }

    public async Task Restore(string directory)
    {
        if (host.busy || InlineMessageBox.Show("回退最近一次内容安装/更新？\n更新后手动改动的文件会阻止回退。存档不受影响。", "内容回退", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            return;
        await host.Run(async () =>
        {
            host.RequireStopped();
            var token = host.CurrentToken;
            await Task.Run(() => ContentTransaction.RestoreLatest(directory, token), token);
            host.RefreshLocalContent();
            host.Log("最近一次内容操作已回退。");
        }, "回退内容更新");
    }
}
