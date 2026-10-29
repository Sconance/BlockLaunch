namespace BlockLaunch;

/// <summary>Framework-neutral settings selection. Page construction belongs to the host/view.</summary>
internal sealed class SettingsNavigationController : IDisposable
{
    readonly ISettingsNavigationView view;
    readonly Action<string> activate;
    IReadOnlyList<SettingsSection> sections = [];
    internal string SelectedSettingsSection { get; private set; } = "launch";

    internal SettingsNavigationController(ISettingsNavigationView view, Action<string> activate)
    {
        this.view = view;
        this.activate = activate;
        view.SectionRequested += SelectSettingsSection;
    }

    internal void SetSettingsSections(IReadOnlyList<SettingsSection> values)
    {
        if (values.Select(section => section.Id).Distinct().Count() != values.Count)
            throw new ArgumentException("Settings section IDs must be unique.");
        sections = values;
        view.RenderSections(values);
        if (values.Count > 0)
        {
            SelectedSettingsSection = values.Any(section => section.Id == SelectedSettingsSection) ? SelectedSettingsSection : values[0].Id;
            view.SelectSection(SelectedSettingsSection);
        }
    }

    internal void SelectSettingsSection(string id)
    {
        if (!sections.Any(section => section.Id == id))
            return;
        SelectedSettingsSection = id;
        view.SelectSection(id);
        activate(id);
    }

    public void Dispose() => view.SectionRequested -= SelectSettingsSection;
}
