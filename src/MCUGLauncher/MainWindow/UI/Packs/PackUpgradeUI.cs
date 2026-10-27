namespace BlockLaunch;

public partial class MainWindow
{
    internal Task UpgradePack(string key, string directory, InlineDialog owner) => packUpgradeController.Upgrade(key, directory, owner);

    InlineDialog BuildPackUpgradeDialog(PackUpgradePreview preview, object owner, Func<string[], Task<bool>> commit) =>
        packUpgradeController.BuildDialog(preview, owner, commit);

    internal void ReturnToOriginal(string key, InlineDialog owner) => packUpgradeController.ReturnToOriginal(key, owner);
}
