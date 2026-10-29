using System.IO;
using System.Windows;
using System.Windows.Controls;
using BlockLaunch.Contracts;
using Microsoft.Win32;

namespace BlockLaunch.Front;

public partial class LauncherView : UserControl, IDisposable
{
    readonly IFrontLauncher launcher;
    readonly CancellationTokenSource lifetime = new();
    bool busy;
    bool disposed;
    bool controlsReady;
    IReadOnlyList<FrontProfile> profileSnapshot = [];
    public string? SelectedProfile => (Profiles.SelectedItem as FrontProfile)?.Id;
    public event Action<string>? RequestPage;
    public FrontMenuRegistry? MenuPages { get; set; }
    string loadedMinecraft = "";
    string loadedLoader = "";
    int skinRevision;

    public LauncherView(IFrontLauncher launcher)
    {
        this.launcher = launcher;
        InitializeComponent();
        controlsReady = true;
        Avatar.Source = AvatarRenderer.Default();
        Loaded += (_, _) => ReadAccounts();
    }

    public void Navigate(string page)
    {
        Pages.SelectedIndex = page == "content" ? 2 : page == "accounts" ? 1 : page is "install" or "import" ? 3 : 0;
        GameInstallPanel.Visibility = page == "import" ? Visibility.Collapsed : Visibility.Visible;
        PackImportPanel.Visibility = page == "install" ? Visibility.Collapsed : Visibility.Visible;
    }

    async Task Run(Func<Task> action)
    {
        if (busy || disposed)
            return;
        busy = true;
        try
        {
            ActionStatus.Text = "正在执行…";
            await action();
            if (!disposed)
                ActionStatus.Text = launcher.Status;
        }
        catch (OperationCanceledException)
        {
            if (!disposed)
                ActionStatus.Text = "操作已取消。";
        }
        catch (Exception exception)
        {
            if (!disposed)
                ActionStatus.Text = "操作失败：" + exception.Message;
        }
        finally
        {
            busy = false;
        }
    }

    FrontProfile Selected() => Profiles.SelectedItem as FrontProfile ?? throw new InvalidOperationException("请先刷新并选择版本。");
    string LoaderKind => (Loader.SelectedItem as ComboBoxItem)?.Tag as string ?? "vanilla";
    Task Done(Action action) { action(); return Task.CompletedTask; }

    async void RefreshProfilesClick(object sender, RoutedEventArgs args) => await RefreshProfiles();

    public Task RefreshProfiles() => Run(async () =>
    {
        var old = (Profiles.SelectedItem as FrontProfile)?.Id;
        var result = await launcher.ReadProfiles(lifetime.Token);
        if (disposed)
            return;
        RootLabel.Text = result.Root;
        profileSnapshot = result.Items;
        Profiles.ItemsSource = result.Items.Where(item => item.Installed).ToArray();
        var installable = result.Items.Where(item => item.Type.Equals("release", StringComparison.OrdinalIgnoreCase) || item.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase)).ToArray();
        InstallProfiles.ItemsSource = installable;
        Profiles.SelectedItem = result.Items.FirstOrDefault(item => item.Id == old && item.Installed) ?? result.Items.FirstOrDefault(item => item.Installed);
        InstallProfiles.SelectedIndex = installable.Length > 0 ? 0 : -1;
        FilterProfiles();
        ProfileCards.SelectedItem = Profiles.SelectedItem;
        if (result.Warning.Length > 0)
            MessageBox.Show(Window.GetWindow(this), result.Warning, "在线版本查询失败，显示本地实例");
    });

    void ProfileChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!controlsReady)
            return;
        if (!ReferenceEquals(ProfileCards.SelectedItem, Profiles.SelectedItem))
            ProfileCards.SelectedItem = Profiles.SelectedItem;
        SelectedLabel.Text = (Profiles.SelectedItem as FrontProfile)?.Label ?? "尚未安装游戏，请前往下载";
    }
    void InstallProfileChanged(object sender, SelectionChangedEventArgs args) => ClearLoaders();

    void ProfileCardChanged(object sender, SelectionChangedEventArgs args)
    {
        if (ProfileCards.SelectedItem is FrontProfile profile && !ReferenceEquals(Profiles.SelectedItem, profile))
            Profiles.SelectedItem = profile;
    }
    void LoaderChanged(object sender, SelectionChangedEventArgs args) => ClearLoaders();
    void ClearLoaders()
    {
        loadedMinecraft = loadedLoader = "";
        if (LoaderVersions != null)
            LoaderVersions.ItemsSource = null;
    }

    async void LoadersClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        var version = InstallSelected().Id;
        var loader = LoaderKind;
        var result = await launcher.ReadLoaders(version, loader, lifetime.Token);
        if (disposed || version != InstallSelected().Id || loader != LoaderKind)
            return;
        LoaderVersions.ItemsSource = result;
        LoaderVersions.SelectedIndex = result.Count > 0 ? 0 : -1;
        loadedMinecraft = version;
        loadedLoader = loader;
    });

    async void InstallClick(object sender, RoutedEventArgs args) => await Run(() => Done(() =>
    {
        var version = InstallSelected().Id;
        var loader = LoaderKind;
        if (loader != "vanilla" && (loadedMinecraft != version || loadedLoader != loader))
            throw new InvalidOperationException("请重新读取当前游戏版本的兼容加载器。");
        if (!Confirm("安装 / 修复 " + version + " · " + loader + "？将写入正式游戏目录。"))
            return;
        launcher.EnqueueGame(version, loader, (LoaderVersions.SelectedItem as FrontLoader)?.Id ?? "");
    }));

    async void LaunchClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        if (OfflineMode.IsChecked == true)
            launcher.SelectAccount("offline", OfflineName.Text.Trim());
        else if (MicrosoftMode.IsChecked == true)
            launcher.SelectAccount("microsoft", "");
        else
            launcher.SelectAccount("mua", "");
        await launcher.Launch(Selected().Id, summary => Confirm(summary + "\n\n了解风险并继续启动？"), lifetime.Token);
    });
    async void StopClick(object sender, RoutedEventArgs args) => await Run(() => Done(launcher.StopGame));

    async void ImportClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        var picker = new OpenFileDialog { Filter = "整合包 (*.mrpack;*.zip)|*.mrpack;*.zip", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) == true && Confirm("导入 " + Path.GetFileName(picker.FileName) + "？将创建真实实例。"))
            await launcher.ImportPack(picker.FileName, lifetime.Token);
    });

    void ReadAccounts()
    {
        try
        {
            var account = launcher.ReadAccounts();
            AccountSummary.Text = $"当前：{account.Selected} · {account.OfflineName}\n微软：{account.MicrosoftName}\nMUA：{account.MuaName}";
            OfflineName.Text = account.Selected == "offline" ? account.OfflineName : "Player";
            OfflineMode.IsChecked = account.Selected == "offline";
            MicrosoftMode.IsChecked = account.Selected == "microsoft";
            MuaMode.IsChecked = account.Selected == "mua";
            PlayerLabel.Text = account.Selected == "microsoft" ? account.MicrosoftName : account.Selected == "mua" ? account.MuaName : account.OfflineName;
            _ = RefreshSkin();
        }
        catch (Exception exception)
        {
            ActionStatus.Text = "读取账号失败：" + exception.Message;
        }
    }

    void AccountsClick(object sender, RoutedEventArgs args) => ReadAccounts();
    async void RefreshSkinClick(object sender, RoutedEventArgs args) => await RefreshSkin();

    async void SelectSkinClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        var picker = new OpenFileDialog { Filter = "皮肤 PNG|*.png", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) != true)
            return;
        if (new FileInfo(picker.FileName).Length > 2 * 1024 * 1024)
            throw new IOException("皮肤文件不能超过 2 MiB。");
        AvatarRenderer.FromSkin(await File.ReadAllBytesAsync(picker.FileName, lifetime.Token));
        launcher.SaveOfflineSkin(picker.FileName);
        await RefreshSkin();
    });

    async Task RefreshSkin()
    {
        var revision = ++skinRevision;
        Avatar.Source = AvatarRenderer.Default();
        try
        {
            var data = await launcher.ReadAccountSkin(lifetime.Token);
            if (!disposed && revision == skinRevision && data != null)
                Avatar.Source = AvatarRenderer.FromSkin(data);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            if (!disposed && revision == skinRevision)
                Avatar.ToolTip = "头像暂不可用：" + exception.Message;
        }
    }
    async void OfflineClick(object sender, RoutedEventArgs args) => await Run(() => Done(() => { launcher.SelectAccount("offline", OfflineName.Text.Trim()); ReadAccounts(); }));
    async void SelectMicrosoftClick(object sender, RoutedEventArgs args) => await Run(() => Done(() => { launcher.SelectAccount("microsoft", ""); ReadAccounts(); }));
    async void SelectMuaClick(object sender, RoutedEventArgs args) => await Run(() => Done(() => { launcher.SelectAccount("mua", ""); ReadAccounts(); }));
    async void MicrosoftClick(object sender, RoutedEventArgs args) => await Run(async () => { await launcher.LoginMicrosoft(lifetime.Token); ReadAccounts(); });
    async void ForgetMicrosoftClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        if (Confirm("清除已保存的微软登录？"))
            await launcher.ForgetAccount("microsoft", lifetime.Token);
        ReadAccounts();
    });
    async void ForgetMuaClick(object sender, RoutedEventArgs args) => await Run(async () =>
    {
        if (Confirm("退出 MUA 并清除本地登录？"))
            await launcher.ForgetAccount("mua", lifetime.Token);
        ReadAccounts();
    });

    async void MuaClick(object sender, RoutedEventArgs args)
    {
        if (busy || disposed)
            return;
        var password = MuaPassword.Password;
        MuaPassword.Clear();
        try
        {
            await Run(async () =>
            {
                await launcher.LoginMua(MuaAccount.Text.Trim(), password, SelectIdentity, lifetime.Token);
                ReadAccounts();
            });
        }
        finally
        {
            password = "";
        }
    }

    Task<FrontIdentity?> SelectIdentity(IReadOnlyList<FrontIdentity> identities)
    {
        lifetime.Token.ThrowIfCancellationRequested();
        if (identities.Count == 1)
            return Task.FromResult<FrontIdentity?>(identities[0]);
        var choices = new ListBox { ItemsSource = identities, DisplayMemberPath = "Name", SelectedIndex = 0 };
        var confirm = new Button { Content = "使用所选角色", Margin = new Thickness(0, 12, 0, 0) };
        var body = new DockPanel { Margin = new Thickness(18) };
        DockPanel.SetDock(confirm, Dock.Bottom);
        body.Children.Add(confirm);
        body.Children.Add(choices);
        var dialog = new Window { Owner = Window.GetWindow(this), Title = "选择 MUA 角色", Width = 420, Height = 340, Content = body, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        confirm.Click += (_, _) => dialog.DialogResult = choices.SelectedItem != null;
        var closed = false;
        dialog.Closed += (_, _) => closed = true;
        using var cancellation = lifetime.Token.Register(() => Dispatcher.BeginInvoke(() => { if (!closed) dialog.Close(); }));
        return Task.FromResult(dialog.ShowDialog() == true ? choices.SelectedItem as FrontIdentity : null);
    }

    async void ContentClick(object sender, RoutedEventArgs args) => await Run(() => Done(() => LocalContent.ItemsSource = launcher.ReadContent(Selected().Id)));
    async void ToggleContentClick(object sender, RoutedEventArgs args) => await Run(() => Done(() =>
    {
        if (LocalContent.SelectedItem is not FrontLocalContent item)
            throw new InvalidOperationException("请选择本地资源。");
        launcher.ToggleContent(Selected().Id, item.Path);
        LocalContent.ItemsSource = launcher.ReadContent(Selected().Id);
    }));

    bool Confirm(string message) => MessageBox.Show(Window.GetWindow(this), message, "确认真实操作", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;

    FrontProfile InstallSelected() => InstallProfiles.SelectedItem as FrontProfile ?? throw new InvalidOperationException("请先刷新并选择游戏版本。");

    void AccountModeChanged(object sender, RoutedEventArgs args)
    {
        if (!controlsReady || sender is not RadioButton { Tag: string mode })
            return;
        OfflineIdentity.Visibility = mode == "offline" ? Visibility.Visible : Visibility.Collapsed;
        MicrosoftIdentity.Visibility = mode == "microsoft" ? Visibility.Visible : Visibility.Collapsed;
        MuaIdentity.Visibility = mode == "mua" ? Visibility.Visible : Visibility.Collapsed;
    }

    void ManageAccountsClick(object sender, RoutedEventArgs args) => RequestPage?.Invoke("accounts");
    void BackHomeClick(object sender, RoutedEventArgs args) => RequestPage?.Invoke("start");
    void ToggleVersionsClick(object sender, RoutedEventArgs args)
    {
        VersionsPanel.Visibility = VersionsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        VersionsToggle.Content = VersionsPanel.Visibility == Visibility.Visible ? "<" : ">";
    }
    void SearchProfilesChanged(object sender, TextChangedEventArgs args)
    {
        if (controlsReady)
            FilterProfiles();
    }
    void FilterProfiles()
    {
        var query = ProfileSearch.Text.Trim();
        ProfileCards.ItemsSource = profileSnapshot.Where(item => item.Installed && (query.Length == 0 || item.Label.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    async void ManageVersionClick(object sender, RoutedEventArgs args)
    {
        if (Profiles.SelectedItem is not FrontProfile profile)
        {
            ActionStatus.Text = "请先选择已安装版本。";
            return;
        }
        if (launcher is not IFrontVersions versions)
        {
            ActionStatus.Text = "当前宿主不提供版本管理接口。";
            return;
        }
        try
        {
            var dialog = new VersionManagerWindow(launcher, versions, profile.Id, MenuPages) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            await RefreshProfiles();
            if (!disposed && dialog.CreatedInstance != null)
                Profiles.SelectedItem = profileSnapshot.FirstOrDefault(item => item.Id == dialog.CreatedInstance && item.Installed) ?? Profiles.SelectedItem;
        }
        catch (Exception exception)
        {
            ActionStatus.Text = "版本管理失败：" + exception.Message;
        }
    }

    public void UpdateGameState()
    {
        StopButton.Visibility = launcher.IsGameRunning ? Visibility.Visible : Visibility.Collapsed;
        LaunchButton.IsEnabled = !busy && !launcher.IsBusy && !launcher.IsGameRunning && Profiles.SelectedItem != null;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        lifetime.Cancel();
        MuaPassword.Clear();
        lifetime.Dispose();
    }
}
