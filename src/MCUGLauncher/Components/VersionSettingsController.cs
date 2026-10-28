using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BlockLaunch;
internal sealed class VersionSettingsController
{
    readonly LauncherWorkspace workspace;
    internal VersionSettingsController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal InlineDialog? versionSettingsWindow;
    internal string versionInitialPage = "概览";
    internal void OpenVersionSection(string key, string section)
    {
        versionInitialPage = section;
        try
        {
            OpenVersionSettings(key);
        }
        finally
        {
            versionInitialPage = "概览";
        }
    }

    internal static Style VersionMenuStyle() => (Style)System.Windows.Markup.XamlReader.Parse("""
        <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="RadioButton">
          <Setter Property="Background" Value="{DynamicResource ThemeSurfaceBrush}"/><Setter Property="Foreground" Value="{DynamicResource ThemeTextBrush}"/>
          <Setter Property="BorderBrush" Value="Transparent"/><Setter Property="BorderThickness" Value="3,0,0,0"/>
          <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="RadioButton">
            <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" Padding="10,9"><ContentPresenter VerticalAlignment="Center"/></Border>
          </ControlTemplate></Setter.Value></Setter>
          <Style.Triggers><Trigger Property="IsChecked" Value="True"><Setter Property="Background" Value="{DynamicResource ThemeSelectedBrush}"/><Setter Property="Foreground" Value="#006DB8"/><Setter Property="BorderBrush" Value="#006DB8"/></Trigger></Style.Triggers>
        </Style>
        """);
    internal void OpenVersionSettings(string key)
    {
        try
        {
            OpenVersionSettingsCore(key);
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(workspace.Window, ex.Message, "无法打开版本设置");
        }
    }

    internal void OpenVersionSettingsCore(string key)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        if (versionSettingsWindow != null)
        {
            versionSettingsWindow.Activate();
            return;
        }

        var root = workspace.LaunchCoordinator.prefs.Root;
        var catalog = new InstanceCatalog(root);
        var descriptor = catalog.GetOrCreate(key);
        var requirements = workspace.InstancePagesController.RequirementsFor(descriptor.LaunchProfileId);
        var window = new InlineDialog
        {
            Owner = workspace.Window,
            Title = "版本设置 · " + descriptor.DisplayName,
            Width = 920,
            Height = 710,
            MinWidth = 820,
            MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(226, 237, 250)),
            FontFamily = workspace.Window.FontFamily
        };
        versionSettingsWindow = window;
        window.Closed += (_, _) =>
        {
            versionSettingsWindow = null;
            workspace.InstancePagesController.RefreshInstanceViews();
        };
        workspace.ExperienceThemeController.ConfigureUtilityWindow(window);
        window.Closing += (_, e) =>
        {
            if (workspace.LaunchCoordinator.busy)
                e.Cancel = true;
        };
        var layout = new Grid();
        layout.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        layout.RowDefinitions.Add(new());
        window.Content = layout;
        var bar = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
            LastChildFill = true
        };
        bar.Visibility = Visibility.Collapsed;
        // The main window provides the shared title and Back action.
        var back = FeatureUiElements.ActionButton("← 返回", (_, _) => window.Close());
        back.Foreground = Brushes.White;
        back.Background = Brushes.Transparent;
        DockPanel.SetDock(back, Dock.Left);
        bar.Children.Add(back);
        var title = new TextBlock
        {
            Text = "版本设置 · " + descriptor.DisplayName,
            Foreground = Brushes.White,
            FontSize = 17,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 12, 10, 12)
        };
        bar.Children.Add(title);
        layout.Children.Add(bar);
        var body = new Grid();
        body.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(150)
        });
        body.ColumnDefinitions.Add(new());
        Grid.SetRow(body, 1);
        layout.Children.Add(body);
        var menu = new StackPanel
        {
            Background = Brushes.White
        };
        body.Children.Add(menu);
        var content = new StackPanel
        {
            Margin = new Thickness(16)
        };
        var scroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(scroll, 1);
        body.Children.Add(scroll);
        string DataDirectory() => (descriptor.Launch ?? workspace.LaunchCoordinator.prefs.Launch).Isolation == "共享根目录" ? root : catalog.DirectoryFor(key);
        Border VersionCard(UIElement child) => new()
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = child
        };
        Button Button(string label, RoutedEventHandler action)
        {
            var button = FeatureUiElements.ActionButton(label, action);
            button.MinWidth = 112;
            button.FontSize = 12;
            button.Margin = new Thickness(0, 4, 10, 4);
            button.Padding = new Thickness(12, 8, 12, 8);
            return button;
        }

        Grid Field(string label, Control value, Button? action = null)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 3, 0, 3)
            };
            row.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(64)
            });
            row.ColumnDefinitions.Add(new());
            row.ColumnDefinitions.Add(new()
            {
                Width = GridLength.Auto
            });
            row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
            Grid.SetColumn(value, 1);
            row.Children.Add(value);
            if (action != null)
            {
                Grid.SetColumn(action, 2);
                action.MinWidth = 80;
                action.Margin = new Thickness(8, 4, 0, 4);
                row.Children.Add(action);
            }

            return row;
        }

        StackPanel Card(string name)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = name, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
            content.Children.Add(VersionCard(stack));
            return stack;
        }

        void SaveIdentity()
        {
            if (workspace.LaunchCoordinator.busy)
                throw new IOException("请等待当前任务完成。");
            workspace.ContentController.RequireStopped();
            catalog.Save(descriptor);
            title.Text = "版本设置 · " + descriptor.DisplayName;
            window.Title = title.Text;
        }

        void EditText(string label, string value, Action<string> save)
        {
            if (workspace.LaunchCoordinator.busy)
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
            var dialog = workspace.InstancePagesController.FeatureDialog(label, panel);
            dialog.Owner = window;
            dialog.Height = 270;
            panel.Children.Add(Button("保存", (_, _) =>
            {
                try
                {
                    save(input.Text);
                    SaveIdentity();
                    dialog.DialogResult = true;
                }
                catch (Exception ex)
                {
                    InlineMessageBox.Show(dialog, ex.Message);
                }
            }));
            if (dialog.ShowDialog() == true)
                Render("概览");
        }

        void Header()
        {
            var row = new DockPanel();
            if (File.Exists(descriptor.IconPath))
            {
                var icon = new Image
                {
                    Source = new BitmapImage(new Uri(descriptor.IconPath)),
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(0, 0, 14, 0)
                };
                DockPanel.SetDock(icon, Dock.Left);
                row.Children.Add(icon);
            }
            else
            {
                var icon = new TextBlock
                {
                    Text = requirements.Loader == "vanilla" ? "▣" : "◇",
                    FontSize = 33,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 109, 184)),
                    Margin = new Thickness(0, 0, 14, 0)
                };
                DockPanel.SetDock(icon, Dock.Left);
                row.Children.Add(icon);
            }

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = descriptor.DisplayName, FontSize = 16, FontWeight = FontWeights.SemiBold });
            info.Children.Add(new TextBlock { Text = "Minecraft " + requirements.Minecraft + " · " + requirements.Loader + (requirements.LoaderVersion.Length > 0 ? " " + requirements.LoaderVersion : ""), FontSize = 11, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 3, 0, 0) });
            row.Children.Add(info);
            content.Children.Add(VersionCard(row));
        }

        void Render(string page)
        {
            content.Children.Clear();
            scroll.ScrollToTop();
            Header();
            content.Dispatcher.BeginInvoke(() => workspace.ExperienceThemeController.ThemeWindow(workspace.Window));
            if (page == "概览")
            {
                var custom = Card("个性化");
                var icons = new ComboBox
                {
                    ItemsSource = new[]
                    {
                        "自动",
                        "选择自定义图片…"
                    },
                    SelectedIndex = File.Exists(descriptor.IconPath) ? 1 : 0
                };
                custom.Children.Add(Field("图标", icons));
                icons.SelectionChanged += (_, _) =>
                {
                    if (workspace.LaunchCoordinator.busy)
                        return;
                    try
                    {
                        if (icons.SelectedIndex == 0)
                            descriptor.IconPath = "";
                        else
                        {
                            var picker = new OpenFileDialog
                            {
                                Filter = "版本图标|*.png;*.jpg;*.jpeg;*.ico"
                            };
                            if (picker.ShowDialog(window) != true)
                                return;
                            var image = new BitmapImage(new Uri(picker.FileName));
                            if (image.PixelWidth > 4096 || image.PixelHeight > 4096)
                                throw new IOException("图标尺寸不能超过 4096×4096。");
                            var file = Path.Combine(catalog.MetadataDirectory(key), "icon" + Path.GetExtension(picker.FileName));
                            File.Copy(picker.FileName, file, true);
                            descriptor.IconPath = file;
                        }

                        SaveIdentity();
                        Render("概览");
                    }
                    catch (Exception ex)
                    {
                        InlineMessageBox.Show(window, ex.Message);
                    }
                };
                var category = new ComboBox
                {
                    ItemsSource = new[]
                    {
                        "自动",
                        "原版游戏",
                        "整合包",
                        "常用版本",
                        "测试版本",
                        descriptor.Category
                    }.Distinct().ToList(),
                    SelectedItem = descriptor.Category
                };
                custom.Children.Add(Field("分类", category, Button("保存分类", (_, _) =>
                {
                    try
                    {
                        descriptor.Category = category.SelectedItem as string ?? "自动";
                        SaveIdentity();
                    }
                    catch (Exception ex)
                    {
                        InlineMessageBox.Show(window, ex.Message);
                    }
                })));
                var personalization = new WrapPanel();
                personalization.Children.Add(Button("修改版本名", (_, _) => EditText("修改版本名", descriptor.DisplayName, text =>
                {
                    if (string.IsNullOrWhiteSpace(text))
                        throw new IOException("版本名不能为空。");
                    descriptor.DisplayName = text.Trim();
                })));
                personalization.Children.Add(Button("修改版本描述", (_, _) => EditText("修改版本描述", descriptor.Description, text => descriptor.Description = text)));
                personalization.Children.Add(Button(descriptor.Favorite ? "移出收藏夹" : "加入收藏夹", (_, _) =>
                {
                    try
                    {
                        descriptor.Favorite = !descriptor.Favorite;
                        SaveIdentity();
                        Render("概览");
                    }
                    catch (Exception ex)
                    {
                        InlineMessageBox.Show(window, ex.Message);
                    }
                }));
                personalization.Children.Add(Button("自定义分类", (_, _) => EditText("自定义分类", descriptor.Category, text => descriptor.Category = string.IsNullOrWhiteSpace(text) ? "自动" : text.Trim())));
                custom.Children.Add(personalization);
                if (descriptor.Description.Length > 0)
                    custom.Children.Add(FeatureUiElements.Hint(descriptor.Description));
                var shortcuts = Card("快捷方式");
                var folders = new WrapPanel();
                folders.Children.Add(Button("存档与截图库", (_, _) =>
                {
                    window.Close();
                    workspace.InstanceLibraryController.OpenInstanceLibrary(key);
                }));
                foreach (var item in new[]
                {
                    ("版本文件夹", ""),
                    ("存档文件夹", "saves"),
                    ("Mod 文件夹", "mods"),
                    ("截图文件夹", "screenshots")
                }

                )
                    folders.Children.Add(Button(item.Item1, (_, _) =>
                    {
                        try
                        {
                            workspace.LaunchCoordinator.OpenFolder(Path.Combine(DataDirectory(), item.Item2));
                        }
                        catch (Exception ex)
                        {
                            InlineMessageBox.Show(window, ex.Message);
                        }
                    }));
                shortcuts.Children.Add(folders);
                var advanced = Card("高级管理");
                var actions = new WrapPanel();
                actions.Children.Add(Button("导出启动脚本", (_, _) => workspace.InstancePagesController.ExportScript(key)));
                actions.Children.Add(Button("补全文件", (_, _) => workspace.DownloadQueueController.EnqueueDownload("下载 / 修复 · " + descriptor.DisplayName, key, new() { Version = descriptor.LaunchProfileId })));
                if (File.Exists(Path.Combine(catalog.MetadataDirectory(key), ".blocklaunch-pack.json")))
                    actions.Children.Add(Button("升级整合包", async (_, _) => await workspace.PackUpgradeController.UpgradePack(key, DataDirectory(), window)));
                if (File.Exists(Path.Combine(catalog.MetadataDirectory(key), ".blocklaunch-upgrade.json")))
                    actions.Children.Add(Button("返回升级前实例", (_, _) => workspace.PackUpgradeController.ReturnToOriginal(key, window)));
                var remove = Button("删除版本", (_, _) => workspace.UninstallController.UninstallInstance(key));
                remove.Foreground = Brushes.Red;
                remove.BorderBrush = Brushes.Red;
                remove.BorderThickness = new Thickness(1);
                actions.Children.Add(remove);
                advanced.Children.Add(actions);
            }
            else if (page == "设置")
            {
                content.Children.Add(FeatureUiElements.Hint(descriptor.Launch == null ? "当前跟随全局设置，保存后仅对本版本生效。" : "当前使用此版本的独立设置。"));
                var value = JsonSerializer.Deserialize<LaunchSettings>(JsonSerializer.Serialize(descriptor.Launch ?? workspace.LaunchCoordinator.prefs.Launch))!;
                var form = new LaunchOptionsForm(value);
                content.Children.Add(form.Panel);
                var java = new WrapPanel();
                java.Children.Add(Button("Java 管理", (_, _) => workspace.JavaManagerController.OpenJavaManager(root, descriptor.LaunchProfileId, path =>
                {
                    if (workspace.LaunchCoordinator.busy)
                        throw new IOException("请等待当前任务完成。");
                    form.SetJava(path);
                }, window)));
                java.Children.Add(Button("选择 Java 文件", (_, _) =>
                {
                    var picker = new OpenFileDialog
                    {
                        Filter = "Java 程序|java.exe;javaw.exe|应用程序|*.exe"
                    };
                    if (picker.ShowDialog(window) == true)
                        form.SetJava(picker.FileName);
                }));
                java.Children.Add(Button("自动检测兼容 Java", async (_, _) => await workspace.LaunchCoordinator.Run(async () =>
                {
                    var path = await JavaDiscovery.FindCompatible(root, requirements.Minecraft, workspace.LaunchCoordinator.CurrentToken);
                    if (string.IsNullOrWhiteSpace(path))
                        InlineMessageBox.Show(window, "未找到兼容 Java，可在 Java 路径中手动指定。");
                    else
                        form.SetJava(path);
                }, "检测 Java")));
                content.Children.Add(java);
                var actions = new WrapPanel();
                actions.Children.Add(Button("保存版本设置", (_, _) =>
                {
                    try
                    {
                        if (workspace.LaunchCoordinator.busy)
                            return;
                        workspace.ContentController.RequireStopped();
                        var settings = form.Read();
                        settings.Validate();
                        descriptor.Launch = settings;
                        catalog.Save(descriptor);
                        InlineMessageBox.Show(window, "此版本设置已保存。");
                        Render("设置");
                    }
                    catch (Exception ex)
                    {
                        InlineMessageBox.Show(window, ex.Message);
                    }
                }));
                actions.Children.Add(Button("恢复跟随全局设置", (_, _) =>
                {
                    try
                    {
                        if (workspace.LaunchCoordinator.busy)
                            return;
                        workspace.ContentController.RequireStopped();
                        descriptor.Launch = null;
                        catalog.Save(descriptor);
                        Render("设置");
                    }
                    catch (Exception ex)
                    {
                        InlineMessageBox.Show(window, ex.Message);
                    }
                }));
                content.Children.Add(actions);
            }
            else if (page == "修改")
            {
                var loaders = Card("安装 / 修改模组加载器");
                loaders.Children.Add(FeatureUiElements.Hint("为 Minecraft " + requirements.Minecraft + " 安装指定加载器，生成新的启动版本。原实例与存档保留；现有模组不会自动迁移。"));
                var loader = new ComboBox
                {
                    ItemsSource = new[]
                    {
                        "fabric",
                        "quilt",
                        "forge",
                        "neoforge"
                    },
                    SelectedIndex = 0
                };
                var versions = new ComboBox
                {
                    DisplayMemberPath = "Label"
                };
                loaders.Children.Add(loader);
                loaders.Children.Add(versions);
                loaders.Children.Add(Button("获取兼容加载器版本", async (_, _) =>
                {
                    var selected = (string)loader.SelectedItem;
                    await workspace.LaunchCoordinator.Run(async () =>
                    {
                        List<LoaderRelease> releases;
                        if (selected is "forge" or "neoforge")
                            releases = await LoaderCatalog.Get(workspace.LaunchCoordinator.http, requirements.Minecraft, selected, workspace.LaunchCoordinator.CurrentToken);
                        else
                        {
                            var api = selected == "fabric" ? "https://meta.fabricmc.net/v2" : "https://meta.quiltmc.org/v3";
                            using var json = JsonDocument.Parse(await workspace.LaunchCoordinator.http.GetStringAsync(api + "/versions/loader/" + Uri.EscapeDataString(requirements.Minecraft), workspace.LaunchCoordinator.CurrentToken));
                            releases = json.RootElement.EnumerateArray().Select(item => item.GetProperty("loader")).Select(item => new LoaderRelease(item.GetProperty("version").GetString()!, item.GetProperty("version").GetString()!)).ToList();
                        }

                        versions.Tag = selected;
                        versions.ItemsSource = releases;
                        versions.SelectedIndex = releases.Count > 0 ? 0 : -1;
                        if (releases.Count == 0)
                            InlineMessageBox.Show(window, "没有此游戏版本可用的加载器版本。");
                    }, "读取加载器版本");
                }));
                loaders.Children.Add(Button("安装所选加载器", (_, _) =>
                {
                    if (workspace.LaunchCoordinator.busy)
                        return;
                    if (versions.SelectedItem is not LoaderRelease release || versions.Tag as string != loader.SelectedItem as string)
                    {
                        InlineMessageBox.Show(window, "请先获取当前加载器的兼容版本。");
                        return;
                    }

                    workspace.DownloadQueueController.EnqueueDownload("安装 " + loader.SelectedItem + " · " + requirements.Minecraft, requirements.Minecraft, new()
                    {
                        Version = requirements.Minecraft,
                        Loader = (string)loader.SelectedItem,
                        LoaderVersion = release.Version,
                        BackupInstanceDirectory = DataDirectory()
                    });
                    InlineMessageBox.Show(window, "已加入下载队列，可在任务与恢复中查看。");
                }));
                var tools = Card("实例维护");
                tools.Children.Add(Button("补全游戏文件", (_, _) => workspace.DownloadQueueController.EnqueueDownload("下载 / 修复 · " + descriptor.DisplayName, key, new() { Version = descriptor.LaunchProfileId })));
                tools.Children.Add(Button("复制为独立实例", (_, _) =>
                {
                    window.Close();
                    workspace.CollectionsController.CloneInstanceDialog(key);
                }));
            }
            else if (page == "Mod 管理")
            {
                var card = Card("此版本的 Mod");
                card.Children.Add(FeatureUiElements.Hint("目录：" + Path.Combine(DataDirectory(), "mods")));
                var query = new TextBox
                {
                    ToolTip = "搜索 Mod 文件名"
                };
                var list = new ListBox
                {
                    MinHeight = 220,
                    MaxHeight = 390,
                    DisplayMemberPath = "Detail"
                };
                card.Children.Add(query);
                card.Children.Add(list);
                void RefreshMods()
                {
                    var items = ContentService.ListLocal(DataDirectory()).Where(item => Path.GetFileName(Path.GetDirectoryName(item.Path)) == "mods" && item.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase));
                    list.ItemsSource = items.Select(item => new VersionModView(item, item.Name + " · " + item.State)).ToList();
                }

                query.TextChanged += (_, _) => RefreshMods();
                RefreshMods();
                var actions = new WrapPanel();
                card.Children.Add(actions);
                actions.Children.Add(Button("启动前检查", async (_, _) => await workspace.ExperienceController.ShowModScan(key, DataDirectory(), window)));
                actions.Children.Add(Button("刷新", (_, _) => RefreshMods()));
                actions.Children.Add(Button("启用 / 禁用", async (_, _) =>
                {
                    if (list.SelectedItem is not VersionModView item)
                        return;
                    await workspace.LaunchCoordinator.Run(() =>
                    {
                        workspace.ContentController.RequireStopped();
                        ContentService.Toggle(item.Item);
                        RefreshMods();
                        return Task.CompletedTask;
                    }, "切换 Mod 状态");
                }));
                actions.Children.Add(Button("移除选中 Mod", async (_, _) =>
                {
                    if (list.SelectedItem is not VersionModView item)
                        return;
                    await workspace.LaunchCoordinator.Run(() =>
                    {
                        workspace.ContentController.RequireStopped();
                        ContentService.MoveToBackup(DataDirectory(), item.Item);
                        RefreshMods();
                        return Task.CompletedTask;
                    }, "移除 Mod");
                }));
                actions.Children.Add(Button("批量检查更新", async (_, _) => await workspace.UpdatesController.OpenContentUpdates(DataDirectory(), key, requirements.Minecraft, requirements.Loader, owner: window)));
                actions.Children.Add(Button("回退最近内容操作", async (_, _) =>
                {
                    await workspace.UpdatesController.RestoreContent(DataDirectory());
                    RefreshMods();
                }));
                actions.Children.Add(Button("导入 Mod", async (_, _) =>
                {
                    if (workspace.LaunchCoordinator.busy)
                        return;
                    var picker = new OpenFileDialog
                    {
                        Filter = "Mod JAR|*.jar",
                        Multiselect = true
                    };
                    if (picker.ShowDialog(window) != true)
                        return;
                    await workspace.LaunchCoordinator.Run(async () =>
                    {
                        workspace.ContentController.RequireStopped();
                        if (requirements.Loader == "vanilla")
                            throw new IOException("请先安装加载器，再导入 Mod。");
                        await new ContentService(workspace.LaunchCoordinator.http, workspace.LaunchCoordinator.CurrentToken).ImportFiles(DataDirectory(), "mods", picker.FileNames);
                        RefreshMods();
                    }, "导入 Mod");
                }));
                actions.Children.Add(Button("搜索下载 Mod", (_, _) =>
                {
                    if (!workspace.LaunchCoordinator.LocalVersions().Contains(key))
                    {
                        InlineMessageBox.Show(window, "请先完成版本安装。");
                        return;
                    }

                    workspace.Controls.Versions.SelectedValue = key;
                    workspace.Controls.ContentType.SelectedIndex = 0;
                    window.Close();
                    workspace.Controls.DiscoverNav.IsChecked = true;
                }));
            }
            else
            {
                var export = Card("导出整合包");
                export.Children.Add(FeatureUiElements.Hint("生成 MultiMC / Prism 兼容 ZIP，包含 Minecraft 与加载器组件清单。"));
                CheckBox Option(string label, bool selected)
                {
                    var check = new CheckBox
                    {
                        Content = label,
                        IsChecked = selected,
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    export.Children.Add(check);
                    return check;
                }

                var mods = Option("包含模组本体", true);
                var servers = Option("包含服务器列表", false);
                var shaders = Option("包含光影", true);
                var resources = Option("包含资源包", true);
                var saves = Option("包含存档", false);
                export.Children.Add(Button("选择位置并导出", async (_, _) =>
                {
                    if (workspace.LaunchCoordinator.busy)
                        return;
                    var picker = new SaveFileDialog
                    {
                        Filter = "整合包 ZIP|*.zip",
                        FileName = "BlockLaunch-Pack.zip"
                    };
                    if (picker.ShowDialog(window) != true)
                        return;
                    var options = new ExportOptions(mods.IsChecked == true, servers.IsChecked == true, shaders.IsChecked == true, resources.IsChecked == true, saves.IsChecked == true);
                    await workspace.LaunchCoordinator.Run(async () =>
                    {
                        workspace.ContentController.RequireStopped();
                        await InstanceManagement.Export(DataDirectory(), picker.FileName, descriptor.DisplayName, requirements, options, workspace.LaunchCoordinator.CurrentToken);
                        InlineMessageBox.Show(window, "整合包已导出。");
                    }, "导出整合包");
                }));
                var scripts = Card("导出启动脚本");
                scripts.Children.Add(FeatureUiElements.Hint("生成调用此启动器的脚本，不保存账号令牌。"));
                scripts.Children.Add(Button("导出启动脚本", (_, _) => workspace.InstancePagesController.ExportScript(key)));
            }
        }

        foreach (var item in new[]
        {
            ("◇", "概览"),
            ("⚙", "设置"),
            ("✎", "修改"),
            ("▦", "Mod 管理"),
            ("□", "导出")
        }

        )
        {
            var nav = new RadioButton
            {
                Style = VersionMenuStyle(),
                Content = item.Item1 + "   " + item.Item2,
                GroupName = "VersionPages",
                Margin = new Thickness(0, 7, 0, 0),
                FontSize = 14
            };
            nav.Checked += (_, _) =>
            {
                try
                {
                    Render(item.Item2);
                }
                catch (Exception ex)
                {
                    content.Children.Clear();
                    content.Children.Add(FeatureUiElements.Hint(ex.Message));
                }
            };
            menu.Children.Add(nav);
        }

        workspace.MenuExtensionsController.AttachExtensionButtons(LauncherMenuArea.VersionSettings, menu, instanceId: key, lifetime: window);
        var selectedPage = versionInitialPage switch
        {
            "设置" => 1,
            "修改" => 2,
            "Mod 管理" => 3,
            "导出" => 4,
            _ => 0
        };
        ((RadioButton)menu.Children[selectedPage]).IsChecked = true;
        window.ShowDialog();
    }

    internal sealed record VersionModView(LocalContent Item, string Detail);
}
