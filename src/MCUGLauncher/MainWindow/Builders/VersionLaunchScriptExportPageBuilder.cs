namespace BlockLaunch;

internal sealed class VersionLaunchScriptExportPageBuilder(VersionSettingsPageContext context)
{
    public void Build()
    {
        var scripts = context.Card("导出启动脚本");
        scripts.Children.Add(MainWindow.Hint("生成调用此启动器的脚本，不保存账号令牌。"));
        scripts.Children.Add(context.Button("导出启动脚本", (_, _) => context.Host.ExportScript(context.Key)));
    }
}
