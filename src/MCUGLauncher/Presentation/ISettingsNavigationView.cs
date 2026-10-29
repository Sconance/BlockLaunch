namespace BlockLaunch;

internal sealed record SettingsSection(string Id, string Title, string Group, string Glyph = "◇");

internal interface ISettingsNavigationView
{
    event Action<string>? SectionRequested;
    void RenderSections(IReadOnlyList<SettingsSection> sections);
    void SelectSection(string id);
}
