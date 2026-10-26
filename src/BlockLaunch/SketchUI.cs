using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace BlockLaunch;

public partial class MainWindow
{
    Grid? sketchHome;
    Border? sketchWelcome;
    Border? sketchVersions;
    Button? sketchToggle;
    bool sketchExpanded;
    int sketchJavaMajor;
    void ToggleSketchVersions()
    {
        sketchExpanded = !sketchExpanded;
        if (sketchExpanded) ShowHomeVersions();
        else { if(sketchVersions!=null) sketchVersions.Visibility=Visibility.Collapsed; if(sketchWelcome!=null) sketchWelcome.Visibility=Visibility.Collapsed; }
        UpdateSketchToggle();
    }
    void UpdateSketchToggle() { if(sketchToggle!=null) {sketchToggle.Content=sketchExpanded ? "<" : ">";sketchToggle.ToolTip=sketchExpanded ? "收起版本列表" : "展开版本列表";} }
    void BuildSketchUI()
    {
        sketchHome = (Grid)((TabItem)MainTabs.Items[0]).Content;
        sketchHome.ColumnDefinitions[0].Width = new GridLength(255);
        sketchWelcome = sketchHome.Children.OfType<Border>().FirstOrDefault(b => Grid.GetColumn(b) == 2);
        var left = (DockPanel)sketchHome.Children[0];
        var launch = (StackPanel)left.Children[0];
        var oldButtons = launch.Children.OfType<Grid>().First();
        launch.Children.Remove(oldButtons);
        var row = new Grid(); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(38) }); row.ColumnDefinitions.Add(new() { Width = new GridLength(38) });
        launch.Children.Remove(LaunchGameButton); row.Children.Add(LaunchGameButton);
        var choose = ActionButton(">", (_, _) => ToggleSketchVersions()); sketchToggle=choose; choose.ToolTip = "展开版本列表"; choose.FontSize = 24; choose.Margin = new Thickness(4,8,0,4); Grid.SetColumn(choose,1); row.Children.Add(choose);
        var manage = ActionButton("⚙", (_, _) => { try { OpenSketchManager(Selected()); } catch(Exception ex) { InlineMessageBox.Show(ex.Message); } }); manage.ToolTip = "管理当前版本"; manage.Margin = new Thickness(4,8,0,4); Grid.SetColumn(manage,2); row.Children.Add(manage);
        launch.Children.Insert(0,row);
        LaunchGameButton.FontSize = 15;
        if(sketchWelcome!=null)sketchWelcome.Visibility=Visibility.Collapsed;
        var main=(Grid)MainTabs.Parent; main.RowDefinitions[0].Height=new GridLength(8);
        var heading=(Grid)((StackPanel)PageTitle.Parent).Parent;
        heading.Visibility=Visibility.Collapsed;
        var header=(DockPanel)((Grid)Content).Children[0];
        var logo=new BitmapImage(new Uri("pack://application:,,,/MCUGLaunch;component/Assets/MCUGLogo.png"));Icon=logo;
        header.Children.Insert(0,new Image {Source=logo,Width=30,Height=30,Margin=new Thickness(12,6,0,6)});
        if(header.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock brand){brand.Width=160;brand.Margin=new Thickness(8,0,0,0);}
        var greeting=new TextBlock {Text="选择版本，开始游戏。",Foreground=Brushes.White,FontSize=11,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(14,0,8,0)};
        var navigation=(StackPanel)header.Children[header.Children.Count-1];navigation.Children.Add(greeting);
        UpdateSketchToggle();
    }
    void ShowSketchVersions()
    {
        if(sketchHome == null) return;
        if(!sketchExpanded) return;
        if(sketchWelcome != null) sketchWelcome.Visibility = Visibility.Collapsed;
        if(sketchVersions != null) sketchHome.Children.Remove(sketchVersions);
        var body = new DockPanel();
        var toolbar = new StackPanel(); DockPanel.SetDock(toolbar,Dock.Top);
        var directory = new DockPanel();
        var add = ActionButton("＋", AddRootClick); add.ToolTip="添加已有游戏目录"; DockPanel.SetDock(add,Dock.Right); directory.Children.Add(add);
        if(rootList.Parent is Panel parent) parent.Children.Remove(rootList);
        rootList.MinWidth=80; rootList.HorizontalAlignment=HorizontalAlignment.Stretch;
        rootList.ItemTemplate=(DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextTrimming='CharacterEllipsis' ToolTip='{Binding}'/></DataTemplate>");
        directory.Children.Add(rootList);
        var titleRow=new DockPanel();var tools = new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right}; DockPanel.SetDock(tools,Dock.Right); tools.Children.Add(ActionButton("导入整合包",ImportPackClick)); tools.Children.Add(ActionButton("刷新版本",async(_,_)=>await Run(async()=>{await Refresh(); RefreshInstanceViews(); ShowSketchVersions();}))); titleRow.Children.Add(tools);titleRow.Children.Add(Heading("游戏目录")); toolbar.Children.Add(titleRow);toolbar.Children.Add(directory);
        var searchRow=new Grid();searchRow.ColumnDefinitions.Add(new(){Width=new GridLength(28)});searchRow.ColumnDefinitions.Add(new());searchRow.Children.Add(new TextBlock {Text="⌕",FontSize=21,VerticalAlignment=VerticalAlignment.Center});
        var search=new TextBox {Text=instanceQuery.Text,ToolTip="搜索已安装版本",Margin=new Thickness(0,4,0,10)};Grid.SetColumn(search,1);searchRow.Children.Add(search);toolbar.Children.Add(searchRow); body.Children.Add(toolbar);
        searchRow.ColumnDefinitions.Add(new(){Width=new GridLength(112)});
        var javaFilter=new ComboBox {ItemsSource=new[]{0,8,16,17,21,25},SelectedItem=sketchJavaMajor,ToolTip="按实例要求的 Java 大版本筛选",Margin=new Thickness(8,4,0,10)};
        javaFilter.ItemTemplate=(DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock><TextBlock.Style><Style TargetType='TextBlock'><Setter Property='Text' Value='{Binding StringFormat=Java {0}}'/><Style.Triggers><DataTrigger Binding='{Binding}' Value='0'><Setter Property='Text' Value='全部 Java'/></DataTrigger></Style.Triggers></Style></TextBlock.Style></TextBlock></DataTemplate>");
        Grid.SetColumn(javaFilter,2);searchRow.Children.Add(javaFilter);
        var groups=new StackPanel();
        void RenderGroups()
        {
            groups.Children.Clear();
            var query=search.Text.Trim();
            var items=(instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection.Cast<InstanceView>() ?? Enumerable.Empty<InstanceView>();
            var matches=items.Where(i=>(i.Name+" "+i.Detail+" Java "+i.JavaMajor).Contains(query,StringComparison.OrdinalIgnoreCase)&&(sketchJavaMajor==0||i.JavaMajor==sketchJavaMajor)).ToList();
            foreach(var group in matches.GroupBy(i=>i.Group))
            {
                var entries=new StackPanel();
                foreach(var item in group)
                {
                    var line=new DockPanel {Margin=new Thickness(0,2,0,2),MinHeight=54};
                    var actions=new StackPanel {Orientation=Orientation.Horizontal}; DockPanel.SetDock(actions,Dock.Right);
                    var play=ActionButton("▷",async(_,_)=>{if(busy)return; Versions.SelectedValue=item.Key; await Run(Launch);}); play.ToolTip="启动此版本"; actions.Children.Add(play);
                    var settingsButton=ActionButton("⚙",(_,_)=>OpenSketchManager(item.Key)); settingsButton.ToolTip="管理此版本"; actions.Children.Add(settingsButton); line.Children.Add(actions);
                    var label=new StackPanel {HorizontalAlignment=HorizontalAlignment.Stretch}; label.Children.Add(new TextBlock {Text=item.Name,FontSize=13,FontWeight=FontWeights.SemiBold,TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=item.Name}); label.Children.Add(new TextBlock {Text=item.Detail,FontSize=11,Foreground=Brushes.SlateGray,Margin=new Thickness(0,3,0,0),TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=item.Detail});
                    var select=ActionButton("",(_,_)=>{if(busy)return; Versions.SelectedValue=item.Key; UpdateHomeLaunchLabel(); RenderGroups();}); select.ContentTemplate=null; select.Content=label; select.HorizontalContentAlignment=HorizontalAlignment.Stretch; select.ToolTip=item.Name+"\n"+item.Detail; select.Padding=new Thickness(12,7,12,7);
                    select.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' CornerRadius='8' Padding='{TemplateBinding Padding}' BorderThickness='2,0,0,0' BorderBrush='{TemplateBinding BorderBrush}'><ContentPresenter HorizontalAlignment='Stretch' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='0.8'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
                    select.BorderBrush=Brushes.Transparent;
                    if(Versions.SelectedValue as string==item.Key) select.SetResourceReference(Control.BackgroundProperty,"ThemeSelectedBrush");
                    if(Versions.SelectedValue as string==item.Key)select.SetResourceReference(Control.BorderBrushProperty,"AccentBrush");
                    line.Children.Add(select); entries.Children.Add(line);
                }
                var groupCard=FeatureCard(new Expander {Header=group.Key+"（"+group.Count()+"）",IsExpanded=true,Content=entries,Padding=new Thickness(2),FontSize=12});groupCard.SetResourceReference(Border.BorderBrushProperty,"ThemeBorderBrush");groupCard.Padding=new Thickness(12);groupCard.CornerRadius=new CornerRadius(10);groupCard.Margin=new Thickness(0,0,0,8);groups.Children.Add(groupCard);
            }
            if(matches.Count==0) groups.Children.Add(Hint(query.Length>0 ? "没有匹配的已安装版本。" : "还没有安装游戏，请到「下载」获取版本，或添加已有游戏目录。"));
        }
        search.TextChanged+=(_,_)=>{instanceQuery.Text=search.Text;RenderGroups();}; RenderGroups();
        javaFilter.SelectionChanged+=(_,_)=>{sketchJavaMajor=javaFilter.SelectedItem is int major?major:0;RenderGroups();};
        body.Children.Add(new ScrollViewer {Content=groups,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        sketchVersions=new Border {Padding=new Thickness(4,0,0,0),Child=body}; Grid.SetColumn(sketchVersions,2); sketchHome.Children.Add(sketchVersions);UpdateSketchToggle();
    }
    void OpenSketchManager(string initialKey)
    {
        if(busy)return;
        RefreshInstanceViews();
        var page=new InlineDialog {Owner=this,Title="版本内容管理",Tag="SketchManager"};
        var layout=new Grid(); layout.ColumnDefinitions.Add(new(){Width=new GridLength(205)}); layout.ColumnDefinitions.Add(new(){Width=new GridLength(110)}); layout.ColumnDefinitions.Add(new()); page.Content=layout;
        var versions=new DockPanel {Margin=new Thickness(0,0,12,0)}; layout.Children.Add(versions);
        var top=new StackPanel(); DockPanel.SetDock(top,Dock.Top); top.Children.Add(Heading("游戏目录")); top.Children.Add(new TextBlock {Text=prefs.Root,ToolTip=prefs.Root,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=Brushes.SlateGray,Margin=new Thickness(0,4,0,12)}); versions.Children.Add(top);
        var bottom=ActionButton("＋ 导入整合包",(_,e)=>{page.Close();ImportPackClick(this,e);}); DockPanel.SetDock(bottom,Dock.Bottom); versions.Children.Add(bottom);
        var list=new ListBox {DisplayMemberPath="Name",ItemsSource=(instanceList.ItemsSource as System.ComponentModel.ICollectionView)?.SourceCollection}; ScrollViewer.SetCanContentScroll(list,false);VirtualizingPanel.SetScrollUnit(list,ScrollUnit.Pixel);versions.Children.Add(list);
        var categories=new StackPanel {Margin=new Thickness(0,0,12,0)}; Grid.SetColumn(categories,1); layout.Children.Add(categories);
        var right=new DockPanel(); Grid.SetColumn(right,2); layout.Children.Add(right);
        var bar=new StackPanel(); DockPanel.SetDock(bar,Dock.Top); right.Children.Add(bar);
        var files=new ListBox {DisplayMemberPath="Name"};ScrollViewer.SetCanContentScroll(files,false); var info=Hint("选择内容查看详情。"); var preview=new Image {Height=180,Stretch=Stretch.Uniform};
        var detail=new StackPanel(); detail.Children.Add(info); detail.Children.Add(preview); DockPanel.SetDock(detail,Dock.Bottom); right.Children.Add(detail); right.Children.Add(files);
        var current=initialKey; var category="存档"; string directory="";
        string DataRoot() {var catalog=new InstanceCatalog(prefs.Root);var d=catalog.GetOrCreate(current);return (d.Launch??prefs.Launch).Isolation=="共享根目录"?prefs.Root:catalog.DirectoryFor(current);}
        void Render()
        {
            bar.Children.Clear(); preview.Source=null; info.Text="选择内容查看详情。";
            if(list.SelectedItem is not InstanceView selected)return; current=selected.Key;
            var actions=new WrapPanel(); actions.Children.Add(ActionButton("版本设置",(_,_)=>OpenVersionSettings(current))); actions.Children.Add(ActionButton("导出",(_,_)=>OpenVersionSection(current,"导出"))); actions.Children.Add(ActionButton("删除版本",(_,_)=>{page.Close();UninstallInstance(current);})); bar.Children.Add(Heading(selected.Name)); bar.Children.Add(actions);
            directory=Path.Combine(DataRoot(),category switch {"存档"=>"saves","模组"=>"mods","资源包"=>"resourcepacks","光影"=>"shaderpacks","截图"=>"screenshots","蓝图"=>"schematics",_=>""});
            var folder=ActionButton("打开文件夹",(_,_)=>{try{OpenFolder(directory);}catch(Exception ex){InlineMessageBox.Show(ex.Message);}});bar.Children.Add(folder);
            if(category=="服务器") {files.ItemsSource=null;info.Text="服务器地址保存于实例 servers.dat；点击下方管理启动器服务器收藏。";bar.Children.Add(ActionButton("服务器收藏",(_,_)=>OpenServers()));return;}
            files.ItemsSource=Directory.Exists(directory)?new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(f=>f.Name).ToList():null;
            if(files.Items.Count==0)info.Text="此分类暂无内容。";
            if(category=="存档" || category=="截图")bar.Children.Add(ActionButton("备份、导入与预览",(_,_)=>OpenInstanceLibrary(current)));
            if(category=="模组")bar.Children.Add(ActionButton("启用、禁用与更新",(_,_)=>OpenVersionSection(current,"Mod 管理")));
        }
        void SafeRender() {try{Render();}catch(Exception ex){files.ItemsSource=null;preview.Source=null;info.Text="无法读取此分类："+ex.Message;}}
        foreach(var name in new[]{"存档","模组","资源包","光影","服务器","截图","蓝图"}) {var nav=new RadioButton {Content=name,GroupName="SketchCategories",Style=VersionMenuStyle(),Margin=new Thickness(0,4,0,4)};nav.Checked+=(_,_)=>{category=name;SafeRender();};categories.Children.Add(nav);}
        files.SelectionChanged+=(_,_)=>{preview.Source=null;if(files.SelectedItem is not FileSystemInfo entry)return;info.Text=entry.Name+"\n修改时间："+entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm");if(category=="截图" && entry is FileInfo file && file.Length<20*1024*1024 && new[]{".png",".jpg",".jpeg"}.Contains(file.Extension.ToLowerInvariant())) {try{var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.UriSource=new Uri(file.FullName);bitmap.DecodePixelWidth=900;bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.EndInit();preview.Source=bitmap;}catch{info.Text+="\n此图片暂时无法预览。";}}};
        list.SelectionChanged+=(_,_)=>SafeRender();list.SelectedItem=list.Items.Cast<InstanceView>().FirstOrDefault(i=>i.Key==initialKey); ((RadioButton)categories.Children[0]).IsChecked=true;
        page.Closed+=(_,_)=>{RefreshInstanceViews();ShowSketchVersions();}; page.ShowDialog();
    }
}
