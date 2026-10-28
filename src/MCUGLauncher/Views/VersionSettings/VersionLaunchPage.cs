using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed record VersionLaunchActions(
    Func<LaunchSettings> GetGlobalLaunch,
    Action<Action<string>> OpenJavaManager);

/// <summary>Builds the 设置 page using per-instance data and explicit operations.</summary>
internal sealed class VersionLaunchPage(VersionSettingsPageContext context, VersionTaskActions tasks, VersionLaunchActions operations)
{
    internal void Build()
    {
        context.View.Content.Children.Add(FeatureUiElements.Hint(context.Descriptor.Launch == null ? "当前跟随全局设置，保存后仅对本版本生效。" : "当前使用此版本的独立设置。"));
        var value = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(context.Descriptor.Launch ?? operations.GetGlobalLaunch()))!;
        var form = new LaunchOptionsForm(value);
        context.View.Content.Children.Add(form.Panel);
        var java = new WrapPanel();
        java.Children.Add(VersionSettingsView.Button("Java 管理", (_, _) => operations.OpenJavaManager(path =>
        {
            if (tasks.IsBusy())
                throw new IOException("请等待当前任务完成。");
            form.SetJava(path);
        })));
        java.Children.Add(VersionSettingsView.Button("选择 Java 文件", (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Filter = "Java 程序|java.exe;javaw.exe|应用程序|*.exe"
            };
            if (picker.ShowDialog(context.View.Window) == true)
                form.SetJava(picker.FileName);
        }));
        java.Children.Add(VersionSettingsView.Button("自动检测兼容 Java", async (_, _) => await tasks.Run(async () =>
        {
            var path = await JavaDiscovery.FindCompatible(context.Root, context.Requirements.Minecraft, tasks.GetToken());
            if (string.IsNullOrWhiteSpace(path))
                InlineMessageBox.Show(context.View.Window, "未找到兼容 Java，可在 Java 路径中手动指定。");
            else
                form.SetJava(path);
        }, "检测 Java")));
        context.View.Content.Children.Add(java);
        var actions = new WrapPanel();
        actions.Children.Add(VersionSettingsView.Button("保存版本设置", (_, _) =>
        {
            try
            {
                if (tasks.IsBusy())
                    return;
                tasks.RequireStopped();
                var settings = form.Read();
                settings.Validate();
                context.Descriptor.Launch = settings;
                context.Catalog.Save(context.Descriptor);
                InlineMessageBox.Show(context.View.Window, "此版本设置已保存。");
                context.Render("设置");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.View.Window, ex.Message);
            }
        }));
        actions.Children.Add(VersionSettingsView.Button("恢复跟随全局设置", (_, _) =>
        {
            try
            {
                if (tasks.IsBusy())
                    return;
                tasks.RequireStopped();
                context.Descriptor.Launch = null;
                context.Catalog.Save(context.Descriptor);
                context.Render("设置");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.View.Window, ex.Message);
            }
        }));
        context.View.Content.Children.Add(actions);

    }
}
