namespace BlockLaunch;

public sealed class DownloadPauseStore(SettingsStore settings, Func<Preferences> currentPreferences) : IDownloadPauseStore
{
    public DownloadPauseState Read()
    {
        var prefs = currentPreferences();
        return new(prefs.DownloadsPaused, prefs.PausedDownloadId);
    }

    public void Save(DownloadPauseState state)
    {
        var prefs = currentPreferences();
        prefs.DownloadsPaused = state.IsPaused;
        prefs.PausedDownloadId = state.TaskId;
        var persisted = settings.Load();
        persisted.DownloadsPaused = state.IsPaused;
        persisted.PausedDownloadId = state.TaskId;
        settings.Save(persisted);
    }
}
