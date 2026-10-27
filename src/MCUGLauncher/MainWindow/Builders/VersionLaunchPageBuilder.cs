using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BlockLaunch;

internal sealed class VersionLaunchPageBuilder(VersionSettingsPageContext context)
{
    MainWindow host = context.Host;
    InstanceDescriptor descriptor = context.Descriptor;

    public void Build()
    {
        context.Content.Children.Add(MainWindow.Hint(descriptor.Launch == null ? "当前跟随全局设置，保存后仅对本版本生效。" : "当前使用此版本的独立设置。"));
        var value = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(descriptor.Launch ?? host.prefs.Launch))!;
        var form = new LaunchOptionsForm(value);
        context.Content.Children.Add(form.Panel);

        var java = new WrapPanel();
        java.Children.Add(context.Button("Java 管理", (_, _) => host.OpenJavaManager(context.Root, descriptor.LaunchProfileId, path =>
        {
            if (host.busy)
                throw new IOException("请等待当前任务完成。");
            form.SetJava(path);
        }, context.Window)));
        java.Children.Add(context.Button("选择 Java 文件", (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = "Java 程序|java.exe;javaw.exe|应用程序|*.exe" };
            if (picker.ShowDialog(context.Window) == true)
                form.SetJava(picker.FileName);
        }));
        java.Children.Add(context.Button("自动检测兼容 Java", async (_, _) => await host.Run(async () =>
        {
            var path = await JavaDiscovery.FindCompatible(context.Root, context.Requirements.Minecraft, host.CurrentToken);
            if (string.IsNullOrWhiteSpace(path))
                InlineMessageBox.Show(context.Window, "未找到兼容 Java，可在 Java 路径中手动指定。");
            else
                form.SetJava(path);
        }, "检测 Java")));
        context.Content.Children.Add(java);

        var actions = new WrapPanel();
        actions.Children.Add(context.Button("保存版本设置", (_, _) =>
        {
            try
            {
                if (host.busy)
                    return;
                host.RequireStopped();
                var settings = form.Read();
                settings.Validate();
                descriptor.Launch = settings;
                context.Catalog.Save(descriptor);
                InlineMessageBox.Show(context.Window, "此版本设置已保存。");
                context.Render("设置");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.Window, ex.Message);
            }
        }));
        actions.Children.Add(context.Button("恢复跟随全局设置", (_, _) =>
        {
            try
            {
                if (host.busy)
                    return;
                host.RequireStopped();
                descriptor.Launch = null;
                context.Catalog.Save(descriptor);
                context.Render("设置");
            }
            catch (Exception ex)
            {
                InlineMessageBox.Show(context.Window, ex.Message);
            }
        }));
        context.Content.Children.Add(actions);
    }
}
