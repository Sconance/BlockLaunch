using System.IO;
using System.Windows.Controls;

namespace BlockLaunch;

/// <summary>Validates and persists instance identity; page rendering is an injected callback.</summary>
internal sealed class VersionIdentityEditor(
    VersionSettingsPageContext context,
    Func<bool> isBusy,
    Action requireStopped,
    Func<string, StackPanel, InlineDialog> createDialog)
{
    internal void Save()
    {
        if (isBusy())
            throw new IOException("请等待当前任务完成。");
        requireStopped();
        context.Catalog.Save(context.Descriptor);
        context.View.Title.Text = "版本设置 · " + context.Descriptor.DisplayName;
        context.View.Window.Title = context.View.Title.Text;
    }

    internal void Edit(string label, string value, Action<string> save)
    {
        if (isBusy())
            return;
        var panel = new StackPanel();
        panel.Children.Add(FeatureUiElements.Heading(label));
        var input = new TextBox
        {
            Text = value,
            MaxLength = label.Contains("描述") ? 2000 : 100,
            AcceptsReturn = label.Contains("描述"),
            MinHeight = label.Contains("描述") ? 95 : 36
        };
        panel.Children.Add(input);
        var dialog = createDialog(label, panel);
        dialog.Owner = context.View.Window;
        dialog.Height = 270;
        panel.Children.Add(VersionSettingsView.Button("保存", (_, _) =>
        {
            try
            {
                save(input.Text);
                Save();
                dialog.DialogResult = true;
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(dialog, ex.Message);
            }
        }));
        if (dialog.ShowDialog() == true)
            context.Render("概览");
    }
}
