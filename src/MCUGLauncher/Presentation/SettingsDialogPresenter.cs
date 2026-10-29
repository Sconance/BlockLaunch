namespace BlockLaunch;

/// <summary>Only legacy entry points need a dialog; the page factory never receives one.</summary>
internal static class SettingsDialogPresenter
{
    internal static void Show(string title, object owner, Func<Action, SettingsPageContent> create,
        Action<InlineDialog> configure)
    {
        var dialog = new InlineDialog { Title = title, Owner = owner };
        using var page = create(dialog.Close);
        dialog.Content = page.Content;
        dialog.Closed += (_, _) => page.Dispose();
        configure(dialog);
        try
        {
            dialog.ShowDialog();
        }
        finally { dialog.ForceClose(); dialog.Content = null; }
    }
}
