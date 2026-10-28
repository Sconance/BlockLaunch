namespace BlockLaunch;

internal sealed class ContentUpdateController(ResourceManagementService resources, IContentUpdateInteraction interaction)
{
    public async Task Open(ResourceInstallTarget target, string? project = null, object? owner = null)
    {
        List<ContentUpdate>? updates = null;
        await interaction.Run("检查兼容更新", async token =>
        {
            token.ThrowIfCancellationRequested();
            var result = await resources.CheckUpdates(target.Directory, target.GameVersion, target.Loader, interaction.IsGameRunning, token);
            token.ThrowIfCancellationRequested();
            updates = result;
        });
        if (updates == null)
            return;
        var selection = interaction.CreateSelection(updates, project, owner, async (view, roots) =>
        {
            ContentUpdatePreview? preview = null;
            await interaction.Run("解析更新与依赖", async token =>
            {
                token.ThrowIfCancellationRequested();
                var result = await resources.PreviewUpdates(target.Directory, roots, target.GameVersion, target.Loader, interaction.IsGameRunning, token);
                token.ThrowIfCancellationRequested();
                preview = result;
            });
            if (preview == null)
                return;
            if (!view.ConfirmPreview(preview))
                return;
            var request = resources.PrepareUpdate(preview, target.Directory, target.GameVersion, target.Loader);
            interaction.EnqueueUpdate(target, "批量内容更新 · " + preview.Changes.Count + " 项", request);
            view.Close();
        });
        selection.Show();
    }

    public async Task CheckAll()
    {
        if (interaction.IsBusy)
            return;
        try
        {
            await Open(interaction.CaptureTarget());
        }
        catch (Exception ex)
        {
            interaction.ShowError(ex.Message);
        }
    }

    public async Task Restore(string directory)
    {
        if (interaction.IsBusy || !interaction.ConfirmRestore())
            return;
        await interaction.Run("回退内容更新", async token =>
        {
            await resources.Restore(directory, interaction.IsGameRunning, token);
            interaction.RestoreCompleted();
        });
    }
}
