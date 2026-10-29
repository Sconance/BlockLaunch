using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch.Front;

public partial class DownloadBrowserView : UserControl
{
    readonly LauncherView launcher;
    readonly ResourceBrowserView browser;
    readonly ResourceBrowserViewModel resources;
    bool selecting;
    public string CurrentCategory { get; private set; } = "原版游戏";

    public DownloadBrowserView(LauncherView launcher, ResourceBrowserView browser, ResourceBrowserViewModel resources)
    {
        this.launcher = launcher;
        this.browser = browser;
        this.resources = resources;
        InitializeComponent();
    }

    public void DetachContent() => CategoryHost.Content = null;

    public void ShowCategory(string category)
    {
        CurrentCategory = category;
        selecting = true;
        try
        {
            foreach (var button in Categories.Children.OfType<RadioButton>())
                button.IsChecked = Equals(button.Tag, category);
        }
        finally
        {
            selecting = false;
        }
        CategoryHost.Content = null;
        if (category is "原版游戏" or "模组加载器" or "本地整合包")
        {
            launcher.Navigate(category == "本地整合包" ? "import" : "install");
            CategoryHost.Content = launcher;
            return;
        }
        resources.Kind = category switch
        {
            "整合包" => "modpack", "数据包" => "datapack", "资源包" => "resourcepack",
            "光影包" => "shader", _ => "mod"
        };
        CategoryHost.Content = browser;
        browser.ShowResults();
    }

    void CategoryChanged(object sender, RoutedEventArgs args)
    {
        if (!selecting && sender is RadioButton { Tag: string category })
            ShowCategory(category);
    }
}
