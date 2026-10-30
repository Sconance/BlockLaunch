namespace BlockLaunch;

/// <summary>Compatibility navigation only; all Mod operations live in the content manager.</summary>
internal sealed class VersionModPage(VersionSettingsPageContext context, Action openContentManager)
{
    internal void Build()
    {
        context.View.Window.Close();
        openContentManager();
    }
}
