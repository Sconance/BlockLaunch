using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class CollectionsController
{
    readonly LauncherWorkspace workspace;
    internal CollectionsController(LauncherWorkspace workspace)
    {
        this.workspace = workspace;
    }

    internal ResourceBookmarkStore? bookmarks;
    internal readonly TextBox localQuery = new()
    {
        Width = 170,
        ToolTip = "搜索名称、文件名、目录及项目 ID；空格分隔多个关键词"
    };
    internal readonly ComboBox localKind = new()
    {
        Width = 130,
        ItemsSource = new[]
        {
            "全部类型",
            "模组",
            "资源包",
            "光影",
            "数据包"
        },
        SelectedIndex = 0
    };
    internal readonly ComboBox localState = new()
    {
        Width = 110,
        ItemsSource = new[]
        {
            "全部状态",
            "已启用",
            "已禁用"
        },
        SelectedIndex = 0
    };
    internal readonly TextBlock localSummary = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(8, 0, 0, 0)
    };
    internal List<LocalContent> localSnapshot = [];
    internal Grid bookmarkPage = new();
    internal UIElement resourceSearchPage = null!;
    internal void BuildCollectionsUI()
    {
        try
        {
            var store = new ResourceBookmarkStore(workspace.LaunchCoordinator.settingsDir);
            store.Read();
            bookmarks = store;
        }
        catch (Exception ex)
        {
            workspace.LaunchCoordinator.Log("资源收藏暂不可用，原文件已保留：" + ex.Message);
        }

        var projectPanel = (StackPanel)workspace.Controls.ProjectTitle.Parent;
        projectPanel.Children.Insert(projectPanel.Children.IndexOf(workspace.Controls.ProjectDescription) + 1, FeatureUiElements.ActionButton("★ 一键收藏", (_, _) => FavoriteSelected()));
        projectPanel.Children.Insert(projectPanel.Children.IndexOf(workspace.Controls.ProjectDescription) + 2, FeatureUiElements.ActionButton("打开资源收藏夹", (_, _) => ShowBookmarks()));
        var resourceFilters = (StackPanel)((Grid)workspace.Controls.SearchQuery.Parent).Parent;
        // The resource filters are a runtime StackPanel containing the original search Grid.
        resourceFilters.Children.Add(FeatureUiElements.ActionButton("★ 我的收藏夹", (_, _) => ShowBookmarks()));
        var grid = (Grid)workspace.Controls.InstalledContent.Parent;
        var row = grid.Children.OfType<WrapPanel>().First(p => Grid.GetRow(p) == 1);
        grid.Children.Remove(row);
        var tools = new StackPanel();
        Grid.SetRow(tools, 1);
        grid.Children.Add(tools);
        tools.Children.Add(row);
        var search = new WrapPanel
        {
            Margin = new Thickness(0, 0, 0, 10)
        };
        tools.Children.Add(search);
        search.Children.Add(new TextBlock { Text = "本地搜索", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        search.Children.Add(localQuery);
        search.Children.Add(localKind);
        search.Children.Add(localState);
        search.Children.Add(localSummary);
        var resourceTab = (TabItem)workspace.Window.MainTabs.Items[2];
        resourceSearchPage = (UIElement)resourceTab.Content;
        resourceTab.Content = null;
        var pages = new Grid();
        pages.Children.Add(resourceSearchPage);
        pages.Children.Add(bookmarkPage);
        bookmarkPage.Visibility = Visibility.Collapsed;
        resourceTab.Content = pages;
        localQuery.TextChanged += (_, _) => ApplyLocalSearch();
        localKind.SelectionChanged += (_, _) => ApplyLocalSearch();
        localState.SelectionChanged += (_, _) => ApplyLocalSearch();
    }

    internal void ApplyLocalSearch()
    {
        var kind = localKind.SelectedIndex switch
        {
            1 => "mods",
            2 => "resourcepacks",
            3 => "shaderpacks",
            4 => "datapacks",
            _ => "全部"
        };
        var state = localState.SelectedIndex switch
        {
            1 => "已启用",
            2 => "已禁用",
            _ => "全部"
        };
        var selected = (workspace.Controls.InstalledContent.SelectedItem as LocalContent)?.Path;
        var result = LocalContentSearch.Filter(localSnapshot, localQuery.Text, kind, state);
        workspace.Controls.InstalledContent.ItemsSource = result;
        workspace.Controls.InstalledContent.SelectedItem = result.FirstOrDefault(i => i.Path == selected);
        localSummary.Text = $"显示 {result.Count} / {localSnapshot.Count} 项";
    }

    internal List<ContentProject> DecorateProjects(IEnumerable<ContentProject> projects)
    {
        try
        {
            return bookmarks?.Prioritize(projects) ?? projects.ToList();
        }
        catch (Exception ex)
        {
            workspace.LaunchCoordinator.Log("收藏排序暂不可用：" + ex.Message);
            return projects.ToList();
        }
    }

    internal void FavoriteSelected()
    {
        if (workspace.LaunchCoordinator.busy || workspace.Controls.SearchResults.SelectedItem is not ContentProject project || bookmarks == null)
            return;
        try
        {
            var old = bookmarks.Read().FirstOrDefault(i => i.Project.Id == project.Id);
            if (old == null)
                bookmarks.Save(project, workspace.PackController.ContentKind, "");
            RefreshBookmarkDecoration();
            workspace.Controls.SearchSummary.Text = old == null ? "已加入默认收藏夹" : "已收藏，可在我的收藏夹中管理";
        }
        catch (Exception ex)
        {
            workspace.Controls.SearchSummary.Text = "收藏失败：" + ex.Message;
        }
    }

    internal void RefreshBookmarkDecoration()
    {
        var items = workspace.Controls.SearchResults.Items.Cast<ContentProject>().ToList();
        var selectedId = (workspace.Controls.SearchResults.SelectedItem as ContentProject)?.Id;
        workspace.Controls.SearchResults.SelectionChanged -= workspace.ContentController.ProjectChanged;
        try
        {
            workspace.Controls.SearchResults.ItemsSource = DecorateProjects(items);
            workspace.Controls.SearchResults.SelectedItem = workspace.Controls.SearchResults.Items.Cast<ContentProject>().FirstOrDefault(p => p.Id == selectedId);
        }
        finally
        {
            workspace.Controls.SearchResults.SelectionChanged += workspace.ContentController.ProjectChanged;
        }
    }

    internal void ShowBookmarks() => RenderBookmarkFolders();
    internal StackPanel BookmarkLayout(string title)
    {
        workspace.Controls.DiscoverNav.IsChecked = true;
        resourceSearchPage.Visibility = Visibility.Collapsed;
        bookmarkPage.Visibility = Visibility.Visible;
        bookmarkPage.Children.Clear();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var body = new StackPanel();
        scroll.Content = body;
        bookmarkPage.Children.Add(scroll);
        var bar = new WrapPanel();
        bar.Children.Add(FeatureUiElements.ActionButton("← 返回资源搜索", (_, _) =>
        {
            bookmarkPage.Visibility = Visibility.Collapsed;
            resourceSearchPage.Visibility = Visibility.Visible;
        }));
        body.Children.Add(bar);
        body.Children.Add(FeatureUiElements.Heading(title));
        return body;
    }

    internal void RenderBookmarkFolders()
    {
        if (bookmarks == null)
        {
            workspace.Controls.SearchSummary.Text = "收藏数据无法读取，请查看运行日志。";
            return;
        }

        try
        {
            var body = BookmarkLayout("我的收藏夹");
            body.Children.Add(FeatureUiElements.Hint("收藏项目直接进入默认收藏夹；打开收藏夹后可多选并批量下载。"));
            var create = new WrapPanel();
            var name = new TextBox
            {
                Width = 230,
                ToolTip = "新收藏夹名称（可选）",
                MaxLength = 40
            };
            create.Children.Add(name);
            var notice = FeatureUiElements.Hint("");
            create.Children.Add(FeatureUiElements.ActionButton("新建收藏夹", (_, _) =>
            {
                try
                {
                    bookmarks.CreateFolder(name.Text);
                    RenderBookmarkFolders();
                }
                catch (Exception ex)
                {
                    notice.Text = ex.Message;
                }
            }));
            body.Children.Add(create);
            body.Children.Add(notice);
            var items = bookmarks.Read();
            foreach (var folder in bookmarks.Folders())
            {
                var captured = folder;
                var card = new StackPanel();
                card.Children.Add(FeatureUiElements.Heading("☆  " + folder));
                card.Children.Add(FeatureUiElements.Hint(items.Count(i => i.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase)) + " 个资源"));
                card.Children.Add(FeatureUiElements.ActionButton("打开收藏夹 →", (_, _) => RenderBookmarkItems(captured)));
                body.Children.Add(workspace.InstancePagesController.FeatureCard(card));
            }
        }
        catch (Exception ex)
        {
            workspace.Controls.SearchSummary.Text = "收藏读取失败：" + ex.Message;
        }
    }

    internal void RenderBookmarkItems(string folder)
    {
        if (bookmarks == null)
            return;
        var body = BookmarkLayout("我的收藏夹 / " + folder);
        body.Children.Insert(1, FeatureUiElements.ActionButton("← 全部收藏夹", (_, _) => RenderBookmarkFolders()));
        var query = new TextBox
        {
            ToolTip = "搜索名称、作者、描述与标签"
        };
        body.Children.Add(query);
        var kind = new ComboBox
        {
            ItemsSource = new[]
            {
                "全部类型",
                "模组",
                "资源包",
                "整合包",
                "光影",
                "数据包"
            },
            SelectedIndex = 0
        };
        body.Children.Add(kind);
        var list = new ListBox
        {
            SelectionMode = SelectionMode.Multiple,
            MinHeight = 180,
            MaxHeight = 360
        };
        // A checkbox per row makes batch selection discoverable without Ctrl/Shift.
        var template = new DataTemplate(typeof(BookmarkView));
        var check = new FrameworkElementFactory(typeof(CheckBox));
        check.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsSelected") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1), Mode = System.Windows.Data.BindingMode.TwoWay });
        check.SetBinding(ContentControl.ContentProperty, new System.Windows.Data.Binding("."));
        template.VisualTree = check;
        list.ItemTemplate = template;
        body.Children.Add(list);
        var summary = FeatureUiElements.Hint("");
        body.Children.Add(summary);
        void Render()
        {
            var filter = kind.SelectedIndex switch
            {
                1 => "mod",
                2 => "resourcepack",
                3 => "modpack",
                4 => "shader",
                5 => "datapack",
                _ => null
            };
            list.ItemsSource = bookmarks.Query(query.Text, kind: filter).Where(i => i.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase)).Select(i => new BookmarkView(i)).ToList();
            summary.Text = $"{list.Items.Count} 个资源 · 已选 0 项";
        }

        query.TextChanged += (_, _) => Render();
        kind.SelectionChanged += (_, _) => Render();
        list.SelectionChanged += (_, _) => summary.Text = $"{list.Items.Count} 个资源 · 已选 {list.SelectedItems.Count} 项";
        body.Children.Add(FeatureUiElements.Hint("文件下载到独立资源目录，不修改游戏实例。"));
        var actions = new WrapPanel();
        body.Children.Add(actions);
        actions.Children.Add(FeatureUiElements.ActionButton("全选", (_, _) => list.SelectAll()));
        actions.Children.Add(FeatureUiElements.ActionButton("清空选择", (_, _) => list.UnselectAll()));
        actions.Children.Add(FeatureUiElements.ActionButton("批量下载所选", async (_, _) =>
        {
            if (workspace.LaunchCoordinator.busy)
                return;
            var selected = list.SelectedItems.Cast<BookmarkView>().Select(i => i.Item).ToList();
            if (selected.Count == 0)
            {
                summary.Text = "请先勾选资源。";
                return;
            }

            var target = workspace.ResourceFiltersController.SearchTarget();
            int queued = 0, failed = 0;
            bool prepared = false;
            var errors = new List<string>();
            await workspace.LaunchCoordinator.Run(async () =>
            {
                var resolved = await BookmarkBatch.Resolve(workspace.ContentController.Modrinth, selected, target.GameVersion, target.Loader, workspace.LaunchCoordinator.CurrentToken);
                prepared = true;
                foreach (var result in resolved)
                {
                    if (result.Version == null)
                    {
                        failed++;
                        errors.Add(result.Bookmark.Project.Title + "：" + result.Error);
                        workspace.LaunchCoordinator.Log("收藏批量下载 · " + result.Bookmark.Project.Title + "：" + result.Error);
                        continue;
                    }

                    var item = result.Bookmark;
                    var version = result.Version;
                    workspace.DownloadQueueController.EnqueueDownload("收藏下载 · " + item.Project.Title, version.Id, new()
                    {
                        Kind = "resource",
                        ResourceKind = item.Kind,
                        Content = version,
                        Destination = workspace.LaunchCoordinator.prefs.ResourceDownloadDirectory
                    });
                    queued++;
                }
            }, "收藏夹批量下载");
            summary.Text = prepared ? $"已加入下载队列 {queued} 项，未加入 {failed} 项。" + (errors.Count > 0 ? "\n" + string.Join("\n", errors.Take(5)) : "") : "兼容版本查询已取消或失败，未加入下载任务。";
        }));
        actions.Children.Add(FeatureUiElements.ActionButton("查看兼容版本", (_, _) =>
        {
            if (list.SelectedItem is not BookmarkView row)
                return;
            var item = row.Item;
            if (workspace.CompactShellController.compactShellReady)
                workspace.CompactShellController.NavigateDownloads(item.Kind switch
                {
                    "resourcepack" => "资源包",
                    "modpack" => "整合包",
                    "shader" => "光影包",
                    "datapack" => "数据包",
                    _ => "Mod"
                });
            bookmarkPage.Visibility = Visibility.Collapsed;
            resourceSearchPage.Visibility = Visibility.Visible;
            workspace.Controls.ContentType.SelectedIndex = item.Kind switch
            {
                "resourcepack" => 1,
                "modpack" => 2,
                "shader" => 3,
                "datapack" => 4,
                _ => 0
            };
            workspace.Controls.SearchResults.ItemsSource = DecorateProjects([item.Project]);
            workspace.Controls.SearchHint.Visibility = Visibility.Collapsed;
            workspace.Controls.SearchSummary.Text = "来自收藏夹";
            workspace.Controls.SearchResults.SelectedIndex = 0;
        }));
        var edit = new StackPanel();
        body.Children.Add(workspace.InstancePagesController.FeatureCard(edit));
        edit.Children.Add(FeatureUiElements.Heading("整理所选资源"));
        var folders = new ComboBox
        {
            ItemsSource = bookmarks.Folders(),
            SelectedItem = folder
        };
        edit.Children.Add(folders);
        edit.Children.Add(FeatureUiElements.ActionButton("移动所选到收藏夹", (_, _) =>
        {
            try
            {
                foreach (BookmarkView row in list.SelectedItems.Cast<BookmarkView>().ToList())
                    bookmarks.Save(row.Item.Project, row.Item.Kind, string.Join("，", row.Item.Labels), (string)folders.SelectedItem);
                Render();
                RefreshBookmarkDecoration();
            }
            catch (Exception ex)
            {
                summary.Text = ex.Message;
            }
        }));
        var labels = new TextBox
        {
            ToolTip = "标签用逗号分隔，可留空",
            MaxLength = 1000
        };
        edit.Children.Add(labels);
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItems.Count == 1 && list.SelectedItems[0] is BookmarkView chosen)
                labels.Text = string.Join("，", chosen.Item.Labels);
        };
        edit.Children.Add(FeatureUiElements.ActionButton("保存所选资源的标签", (_, _) =>
        {
            try
            {
                ResourceBookmarkStore.Labels(labels.Text);
                foreach (BookmarkView row in list.SelectedItems.Cast<BookmarkView>().ToList())
                    bookmarks.Save(row.Item.Project, row.Item.Kind, labels.Text);
                Render();
                RefreshBookmarkDecoration();
            }
            catch (Exception ex)
            {
                summary.Text = ex.Message;
            }
        }));
        edit.Children.Add(FeatureUiElements.ActionButton("取消所选收藏", (_, _) =>
        {
            try
            {
                foreach (BookmarkView row in list.SelectedItems.Cast<BookmarkView>().ToList())
                    bookmarks.Remove(row.Item.Project.Id);
                Render();
                RefreshBookmarkDecoration();
            }
            catch (Exception ex)
            {
                summary.Text = ex.Message;
            }
        }));
        Render();
    }

    internal void CloneInstanceDialog(string key)
    {
        if (workspace.LaunchCoordinator.busy)
            return;
        try
        {
            var catalog = new InstanceCatalog(workspace.LaunchCoordinator.prefs.Root);
            var original = catalog.GetOrCreate(key);
            var effective = original.Launch ?? workspace.LaunchCoordinator.prefs.Launch;
            var source = effective.Isolation == "共享根目录" ? workspace.LaunchCoordinator.prefs.Root : catalog.DirectoryFor(key);
            var requirements = workspace.InstancePagesController.RequirementsFor(original.LaunchProfileId);
            var body = new StackPanel();
            body.Children.Add(FeatureUiElements.Heading("复制实例 · " + original.DisplayName));
            body.Children.Add(FeatureUiElements.Hint("复制模组、配置、资源和启动设置，创建独立实例。游戏主体、加载器和公共库沿用已有安装；存档、截图和服务器列表按需勾选。复制前请退出使用源实例的游戏。"));
            var name = new TextBox
            {
                Text = original.DisplayName + " - 副本",
                MaxLength = 100
            };
            body.Children.Add(name);
            var saves = new CheckBox
            {
                Content = "包含存档（可能很大）"
            };
            var shots = new CheckBox
            {
                Content = "包含截图"
            };
            var servers = new CheckBox
            {
                Content = "包含服务器列表"
            };
            body.Children.Add(saves);
            body.Children.Add(shots);
            body.Children.Add(servers);
            var dialog = workspace.InstancePagesController.FeatureDialog("复制独立实例", body);
            dialog.Height = 490;
            body.Children.Add(FeatureUiElements.ActionButton("开始复制", async (_, _) =>
            {
                var chosenName = name.Text;
                var options = new CloneOptions(saves.IsChecked == true, shots.IsChecked == true, servers.IsChecked == true);
                dialog.Close();
                await workspace.LaunchCoordinator.Run(async () =>
                {
                    workspace.ContentController.RequireStopped();
                    var newKey = await Task.Run(() => InstanceCloner.Copy(workspace.LaunchCoordinator.prefs.Root, key, source, requirements.Minecraft, chosenName, effective, options, text => workspace.LaunchCoordinator.operations.Current?.Report(text), workspace.LaunchCoordinator.CurrentToken));
                    await workspace.LaunchCoordinator.Refresh();
                    workspace.Controls.Versions.SelectedValue = newKey;
                    workspace.InstancePagesController.RefreshInstanceViews();
                    workspace.LaunchCoordinator.Log("独立实例已复制：" + chosenName);
                }, "复制实例");
            }));
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            InlineMessageBox.Show(ex.Message, "无法复制实例");
        }
    }
}
