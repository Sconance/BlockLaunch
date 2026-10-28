using BlockLaunch.Contracts;

namespace BlockLaunch;

public sealed class FrontSettingsAdapter(SettingsStore store, LauncherSettingsService settings) : IFrontSettings
{
    readonly SemaphoreSlim gate = new(1, 1);

    public async Task<FrontSettingsSnapshot> Read(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                return Snapshot(store.Load());
            }, token);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<FrontSettingsSnapshot> Save(FrontSettingsSnapshot input, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                settings.EnsureCanSave();
                var prefs = store.Load();
                token.ThrowIfCancellationRequested();
                settings.Save(prefs, new(input.Root, input.Java, prefs.Player, input.Ram, input.Width, input.Height,
                    prefs.LastVersion, prefs.PreferMicrosoft, prefs.PreferMua));
                return Snapshot(prefs);
            }, token);
        }
        finally
        {
            gate.Release();
        }
    }

    static FrontSettingsSnapshot Snapshot(Preferences prefs) => new(
        prefs.Root, prefs.Java, prefs.Ram.ToString(), prefs.Width.ToString(), prefs.Height.ToString());
}
