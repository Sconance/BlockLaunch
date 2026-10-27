namespace BlockLaunch;

public partial class MainWindow
{
    // Register additional, implemented pages here. Existing pages and configuration stay unchanged.
    // Example: menus.Register(LauncherMenuArea.Download,
    //     new("my-page", "栏目名称", context => new MyPage(context.Window)) { Glyph = "◇" });
    partial void ConfigureMenuExtensions(LauncherMenuRegistry menus)
    {
    }
}
