using BlockLaunch;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
static class Program
{
    [STAThread] static void Main()
    {
        var root=Path.GetFullPath("work/v0162-inline-"+Guid.NewGuid().ToString("N"));var game=Path.Combine(root,"minecraft");
        AtomicFile.WriteJson(Path.Combine(game,"versions","1.21.1","1.21.1.json"),new {id="1.21.1",type="release",javaVersion=new{majorVersion=21},libraries=Array.Empty<object>()});
        new SettingsStore(root).Save(new(){Root=game,PreferMua=true,Player="OfflineTester",ResourceDownloadDirectory=Path.Combine(root,"downloads")});
        var source=System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LauncherApp.xaml"));var ns=source.Root!.Name.Namespace;var dictionary=new System.Xml.Linq.XElement(ns+"ResourceDictionary",new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns+"x","http://schemas.microsoft.com/winfx/2006/xaml"),source.Root.Element(ns+"Application.Resources")!.Nodes());
        var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown,Resources=(ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString())};var window=new MainWindow(root);SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        object Field(string name)=>typeof(MainWindow).GetField(name,flags)!.GetValue(window)!;
        object? Call(string name,params object[] args)=>typeof(MainWindow).GetMethod(name,flags)!.Invoke(window,args);
        List<InlineDialog> Pages()=>((IEnumerable<InlineDialog>)Field("inlinePages")).ToList();
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
        void Wait(Task task){var frame=new DispatcherFrame();task.ContinueWith(_=>window.Dispatcher.BeginInvoke(()=>frame.Continue=false));Dispatcher.PushFrame(frame);task.GetAwaiter().GetResult();}
        void Capture(string file,int width=1060,int height=700)
        {
            var grid=(Grid)window.Content;grid.Background=window.Background;grid.Measure(new Size(width,height));grid.Arrange(new Rect(0,0,width,height));grid.UpdateLayout();var bmp=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bmp.Render(grid);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using var stream=File.Create("work/verify/"+file);png.Save(stream);
        }
        void Drive(Action start,Func<InlineDialog,bool> step)
        {
            Exception? failure=null;var deadline=DateTime.UtcNow.AddSeconds(20);var scheduled=false;
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(40)};
            timer.Tick+=(_,_)=>
            {
                if(scheduled)return;var page=Pages().LastOrDefault();if(page==null && DateTime.UtcNow<=deadline)return;scheduled=true;
                window.Dispatcher.BeginInvoke(()=>{scheduled=false;try{if(DateTime.UtcNow>deadline)throw new Exception("Inline driver timed out");if(app.Windows.Count!=1)throw new Exception("Unexpected native window");if(!page!.IsLoaded)page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));if(step(page))timer.Stop();}catch(Exception ex){failure=ex;timer.Stop();foreach(var item in Pages().AsEnumerable().Reverse())typeof(InlineDialog).GetMethod("ForceClose",flags)!.Invoke(item,null);}finally{scheduled=false;}});
            };
            timer.Start();try{start();}finally{timer.Stop();}if(failure!=null)throw failure;
        }
        try
        {
            Check(((ComboBox)Field("AccountMode")).SelectedIndex==2,"preferred MUA mode survives missing cache without offline fallback");((ComboBox)Field("AccountMode")).SelectedIndex=0;
            Check(window.WindowStyle==WindowStyle.None && WindowChrome.GetWindowChrome(window)?.ResizeBorderThickness.Left==6,"custom integrated caption retains resize border");
            Check(window.Title.StartsWith("MCUG Launch") && ((SolidColorBrush)app.Resources["AccentBrush"]).Color==Color.FromRgb(0,109,184),"new brand uses Wuhan CUG blue");
            Call("RefreshInstanceViews"); var search=(TextBox)Field("instanceQuery");var versions=(ListBox)Field("instanceList");
            search.Text="no-such-installed-version";Check(versions.Items.Count==0,"installed version search hides unmatched entries");
            search.Text="1.21.1";Check(versions.Items.Count>0,"installed version search finds local game version");search.Clear();
            Capture("v0162-home-offline.png");
            Check(((StackPanel)Field("offlineIdentity")).Visibility==Visibility.Visible,"offline identity has explicitly labeled editable name");
            typeof(MainWindow).GetField("savedAccount",flags)!.SetValue(window,new SavedMicrosoftAccount("OnlineFixture","0123456789abcdef0123456789abcdef"));((ComboBox)Field("AccountMode")).SelectedIndex=1;
            Check(((StackPanel)Field("offlineIdentity")).Visibility==Visibility.Collapsed && ((TextBlock)Field("AvatarPlayerName")).Text=="OnlineFixture","Microsoft mode displays saved role and hides offline input");Capture("v0162-home-online.png");
            Call("ThemeWindow",window);Check(((SolidColorBrush)((RadioButton)Field("microsoftChip")).Foreground).Color==Colors.White,"theme preserves selected account chip foreground trigger");
            ((ComboBox)Field("AccountMode")).SelectedIndex=2;
            Check(((RadioButton)Field("muaChip")).IsChecked==true && ((StackPanel)Field("muaIdentity")).Visibility==Visibility.Visible && ((StackPanel)Field("offlineIdentity")).Visibility==Visibility.Collapsed && ((StackPanel)Field("microsoftIdentity")).Visibility==Visibility.Collapsed,"MUA is a separate right-hand login mode with no offline editor");
            Capture("v0170-mua-home.png");
            var mua=(RadioButton)Field("muaChip");var accountChips=(StackPanel)mua.Parent;
            Check(accountChips.ActualWidth<=244,"three login options fit the account card");
            Check(((TextBlock)Field("AvatarPlayerName")).Text=="尚未登录","MUA mode never displays Microsoft or offline identity");
            PasswordBox? muaPassword=null;
            Drive(()=>Wait((Task)Call("MuaLogin")!),page=>
            {
                Check(page.Title=="MUA 登录" && Pages().Count==1,"MUA login uses the main window inline host");
                Check(Walk(page).OfType<TextBox>().Any(t=>t.IsReadOnly&&t.Text==MuaAccountService.Api),"MUA login displays the fixed credential destination");
                muaPassword=Walk(page).OfType<PasswordBox>().Single();
                Buttons(page).Single(b=>Equals(b.Content,"登录")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(Pages().Count==1&&Walk(page).OfType<TextBlock>().Any(t=>t.Text.Contains("请填写账号 / 角色名")),"empty MUA login stays in form with validation");
                muaPassword.Password="fixture-password-never-sent";Capture("v0170-mua-login-form.png");
                Buttons(page).Single(b=>Equals(b.Content,"取消")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return true;
            });
            Check(muaPassword?.Password.Length==0 && Field("muaAccount")==null,"cancel clears password and does not create an account");
            ((ComboBox)Field("AccountMode")).SelectedIndex=1;
            var parent=new InlineDialog {Title="父页面",Owner=window,Content=new TextBox {Text="保留的编辑内容"}};parent.Show();
            var child=new InlineDialog {Title="子页面",Owner=parent,Content=new TextBlock {Text="子页面内容"}};var closed=0;child.Closed+=(_,_)=>closed++;
            Drive(()=>Check(child.ShowDialog()==true,"embedded modal returns selected result"),page=>{Check(Pages().Count==2,"nested page stack");Check(((StackPanel)((DockPanel)((Grid)window.Content).Children[0]).Children.OfType<StackPanel>().Single(p=>Equals(p.Tag,"WindowControls"))).IsEnabled,"caption buttons stay enabled inside page");page.DialogResult=true;return true;});
            Check(closed==1 && Pages().Count==1 && ((TextBox)parent.Content).Text=="保留的编辑内容","return restores parent content and closes once");parent.Close();Check(Pages().Count==0,"last page returns to main content");
            var guarded=new InlineDialog {Title="关闭保护",Owner=window,Content=new TextBlock()};var refuse=true;guarded.Closing+=(_,e)=>e.Cancel=refuse;guarded.Show();guarded.Close();Check(Pages().Count==1,"close guard preserves active operation");refuse=false;guarded.Close();
            Drive(()=>Check(InlineMessageBox.Show(window,"错误说明","内嵌提示",MessageBoxButton.OKCancel)==MessageBoxResult.Cancel,"cancel result preserves confirmation semantics"),page=>{Buttons(page).Single(b=>Equals(b.Content,"取消")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return true;});
            var files=Path.Combine(root,"picker");Directory.CreateDirectory(Path.Combine(files,"child"));File.WriteAllText(Path.Combine(files,"a.jar"),"a");File.WriteAllText(Path.Combine(files,"b.zip"),"b");File.WriteAllText(Path.Combine(files,"hidden.txt"),"text");
            var open=new BlockLaunch.OpenFileDialog {Title="文件选择测试",InitialDirectory=files,Filter="模组与整合包|*.jar;*.zip",Multiselect=true};
            Drive(()=>Check(open.ShowDialog(window)==true,"embedded file selector accepts"),page=>{var list=Walk(page).OfType<ListBox>().Single();if(list.Items.Count==0 || !list.IsEnabled)return false;var choices=list.Items.Cast<object>().Where(o=>o.GetType().GetProperty("Name")!.GetValue(o)?.ToString() is "a.jar" or "b.zip").ToList();Check(choices.Count==2 && list.Items.Count==3,"file type filtering retains folders and matching files only");foreach(var item in choices)list.SelectedItems.Add(item);Capture("v0162-file-picker.png");Buttons(page).Single(b=>Equals(b.Content,"打开所选文件")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return true;});
            Check(open.FileNames.Length==2 && open.FileNames.All(File.Exists),"multi-select returns exact paths");
            var save=new BlockLaunch.SaveFileDialog {Title="保存路径测试",InitialDirectory=files,Filter="ZIP|*.zip",FileName="new-pack"};
            Drive(()=>Check(save.ShowDialog(window)==true,"embedded save selects path"),page=>{var list=Walk(page).OfType<ListBox>().Single();if(!list.IsEnabled || list.Items.Count==0)return false;Buttons(page).Single(b=>Equals(b.Content,"保存到此处")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return true;});
            Check(save.FileName.EndsWith("new-pack.zip") && !File.Exists(save.FileName),"save appends extension without prematurely creating file");
            var overwrite=new BlockLaunch.SaveFileDialog {Title="覆盖保护测试",InitialDirectory=files,Filter="ZIP|*.zip",FileName="b.zip"};var stage=0;
            Drive(()=>Check(overwrite.ShowDialog(window)!=true,"cancelled overwrite does not select target"),page=>
            {
                if(stage==0 && page.Title=="覆盖保护测试"){var list=Walk(page).OfType<ListBox>().Single();if(!list.IsEnabled || list.Items.Count==0)return false;stage=1;Buttons(page).Single(b=>Equals(b.Content,"保存到此处")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
                else if(stage==1 && page.Title=="覆盖文件确认"){stage=2;Buttons(page).Single(b=>Equals(b.Content,"取消")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
                else if(stage==2 && page.Title=="覆盖保护测试"){page.Close();return true;}return false;
            });Check(File.ReadAllText(Path.Combine(files,"b.zip"))=="b","overwrite cancel preserves original bytes");
            var folder=new BlockLaunch.OpenFolderDialog {Title="文件夹选择测试",InitialDirectory=files};
            Drive(()=>Check(folder.ShowDialog(window)==true,"embedded folder selector accepts"),page=>{var list=Walk(page).OfType<ListBox>().Single();if(!list.IsEnabled || list.Items.Count==0)return false;Check(list.Items.Count==1,"folder selector excludes files");Buttons(page).Single(b=>Equals(b.Content,"选择此文件夹")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return true;});Check(folder.FolderName==files,"folder selector uses visible current location");
            Drive(()=>Call("OpenJavaManager",game,"1.21.1",(Action<string>)(_=>{}),window),page=>{Check(page.Title=="Java 管理","Java management is an inline page");Check(Buttons(page).Any(b=>Equals(b.Content,"全盘搜索 Java"))&&Buttons(page).Any(b=>Equals(b.Content,"搜索指定目录"))&&Buttons(page).Any(b=>Equals(b.Content,"取消搜索")),"Java manager exposes full search, folder search and cancellation");Capture("v0183-java-page.png");page.Close();return true;});
            Drive(()=>Call("OpenVersionSettings","1.21.1"),page=>{Check(page.Title.StartsWith("版本设置"),"version settings is an inline page");Capture("v0162-version-page.png");page.Close();return true;});
            Check(!(bool)Field("sketchExpanded"),"version chooser is collapsed by default");Capture("v0181-home-collapsed.png");
            Call("ToggleSketchVersions");Check(Equals(((Button)Field("sketchToggle")).Content,"<"),"expanded chooser arrow points left");
            var sketch=(Border)Field("sketchVersions");
            Check(((TabControl)Field("MainTabs")).SelectedIndex==0 && Walk(sketch).OfType<Expander>().Any(),"sketch version groups share home with account panel");
            var sketchSearch=Walk(sketch).OfType<TextBox>().Single(); sketchSearch.Text="unmatched-fixture";
            Check(!Walk(sketch).OfType<Expander>().Any(),"sketch search excludes unmatched installed versions");sketchSearch.Clear();
            Capture("v0180-sketch-home.png");
            Capture("v0181-home-expanded.png");Call("ToggleSketchVersions");Check(((Border)Field("sketchVersions")).Visibility==Visibility.Collapsed && Equals(((Button)Field("sketchToggle")).Content,">"),"toggle hides chooser and restores right arrow");Call("ToggleSketchVersions");
            for(var i=0;i<10;i++){var key="fixture-"+i;AtomicFile.WriteJson(Path.Combine(game,"versions",key,key+".json"),new{id=key,type="release",libraries=Array.Empty<object>()});var catalog=new InstanceCatalog(game);var descriptor=catalog.GetOrCreate(key);descriptor.DisplayName="长版本名兼容测试 · Fabric 生存建筑整合包 / Long Minecraft Instance Name "+i;catalog.Save(descriptor);}
            Call("RefreshInstanceViews");
            var selector=(ComboBox)Field("Versions");selector.ItemsSource=new[]{new VersionRow("1.21.1","长版本名兼容测试 · Minecraft 1.21.1 Fabric 生存建筑整合包","release")};selector.SelectedIndex=0;Call("ShowSketchVersions");
            Capture("modern-home-expanded.png");Capture("modern-home-small.png",780,540);
            Call("ApplyResponsiveLayout",1920.0,1080.0);Capture("modern-home-maximized.png",1920,1080);
            Check(Math.Abs(((ScaleTransform)((Grid)window.Content).Children.OfType<FrameworkElement>().First().LayoutTransform).ScaleX-1.4)<0.001,"large window scales content within bounded readable range");
            Check(Math.Abs(WindowChrome.GetWindowChrome(window)!.CaptionHeight-67.2)<0.01,"native caption follows visual scale for drag-to-restore");
            Check(WindowChrome.GetIsHitTestVisibleInChrome((RadioButton)Field("HomeNav")),"caption preserves clickable navigation");
            Call("ApplyResponsiveLayout",1060.0,740.0);Check(((Grid)window.Content).RowDefinitions[0].Height.Value==48,"restored window returns to compact scale");
            Check(((Grid)Field("sketchHome")).ColumnDefinitions[0].Width.Value==224,"compact account column uses 224 pixels");
            var preferences=(Preferences)Field("prefs");preferences.DarkMode=true;Call("ThemeWindow",window);Capture("modern-home-dark.png");Check(((SolidColorBrush)app.Resources["ThemeTextBrush"]).Color.R>200,"dark theme keeps light foreground");preferences.DarkMode=false;Call("ThemeWindow",window);
            Call("ToggleSketchVersions");Capture("modern-home-collapsed.png");Call("ToggleSketchVersions");
            Drive(()=>Call("OpenSketchManager","1.21.1"),page=>{
                Check(page.Title=="版本内容管理","sketch content manager stays inside main window");
                var categories=Walk(page).OfType<RadioButton>().Where(r=>r.GroupName=="SketchCategories").ToList();
                Check(categories.Count==7,"seven sketch content categories available");
                var versionList=Walk(page).OfType<ListBox>().First();Check(!ScrollViewer.GetCanContentScroll(versionList),"manager version list scrolls in pixels");
                var header=(DockPanel)((Grid)window.Content).Children[0];Check(header.Children.OfType<StackPanel>().All(p=>p.IsEnabled),"top navigation remains enabled in content manager");
                foreach(var category in categories){category.IsChecked=true;Check(Pages().Count==1,"category navigation retains one inline page");}
                categories[0].IsChecked=true;Capture("v0181-sketch-manager.png");page.Close();return true;
            });
            Drive(()=>Call("OpenSketchManager","1.21.1"),page=>{var home=(RadioButton)Field("HomeNav");home.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left){RoutedEvent=UIElement.PreviewMouseDownEvent});Check(Pages().Count==0 && ((TabControl)Field("MainTabs")).SelectedIndex==0,"header navigation exits content manager without Back");return true;});
            Drive(()=>Call("OpenVersionSection","1.21.1","导出"),page=>{Check(Walk(page).OfType<TextBlock>().Any(t=>t.Text=="导出启动脚本"),"export action opens export section directly");page.Close();return true;});
            var currentChooser=(Border)Field("sketchVersions");var javaFilter=Walk(currentChooser).OfType<ComboBox>().Single(c=>c.ToolTip?.ToString()?.Contains("Java 大版本")==true);javaFilter.SelectedItem=25;Check(!Walk(currentChooser).OfType<Expander>().Any(),"instance Java filter excludes incompatible major versions");javaFilter.SelectedItem=21;Check(Walk(currentChooser).OfType<Expander>().Any(),"instance Java filter retains compatible major versions");javaFilter.SelectedItem=0;
            var prefs183=(Preferences)Field("prefs");var imagePath=Path.Combine(root,"background.png");var backgroundBitmap=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[]{0,100,200,255,0,100,200,255,0,100,200,255,0,100,200,255},8);var backgroundEncoder=new PngBitmapEncoder();backgroundEncoder.Frames.Add(BitmapFrame.Create(backgroundBitmap));using(var output=File.Create(imagePath))backgroundEncoder.Save(output);
            prefs183.BackgroundPath=imagePath;prefs183.BackgroundFit="完整显示";prefs183.BackgroundOpacity=0.35;Call("ApplyBackground");var imageBrush=(ImageBrush)((Grid)window.Content).Background;Check(imageBrush.Stretch==Stretch.Uniform&&imageBrush.Opacity==0.35,"local background applies fill choice and opacity");
            prefs183.DarkMode=true;Call("ThemeWindow",window);Check(((Grid)window.Content).Background is ImageBrush&&((SolidColorBrush)window.Background).Color.R<50,"custom background survives dark-mode change over a dark solid base");prefs183.DarkMode=false;prefs183.BackgroundPath="";Call("ThemeWindow",window);
            Drive(()=>Call("OpenAppearance"),page=>{Check(Buttons(page).Any(b=>Equals(b.Content,"恢复默认背景"))&&Walk(page).OfType<Slider>().Any(),"appearance page exposes background restore and strength slider");Capture("v0183-appearance.png");page.Close();return true;});
            var queue183=(OperationCoordinator)Field("operations");var failure183=queue183.Enqueue("下载失败验收",game,"fixture",new(){Kind="resource"});try{Wait(queue183.Run("下载失败验收",game,"fixture",_=>throw new IOException("网络中断验收"),failure183));}catch(IOException){}Call("RefreshMiniDownloads",true);
            Check(((TextBlock)Field("miniDownloadLabel")).Text.Contains("下载失败")&&((Border)Field("miniDownload")).ToolTip?.ToString()?.Contains("网络中断验收")==true,"failed download remains visible with failure reason");Call("ToggleMiniDownload");Check(queue183.History().First(r=>r.Id==failure183.Id).State==OperationState.Queued,"failed download retries through existing persistent queue");queue183.ChangePending(failure183.Id,OperationState.Cancelled);Call("RefreshMiniDownloads",true);
            Check(EmbeddedMicrosoftOAuthUI.IsMicrosoftAuthorization(new Uri("https://login.live.com/oauth20_authorize.srf")) && !EmbeddedMicrosoftOAuthUI.IsMicrosoftAuthorization(new Uri("https://login.live.com.evil.test/")) && !EmbeddedMicrosoftOAuthUI.IsMicrosoftAuthorization(new Uri("http://login.live.com/")),"embedded OAuth requires exact Microsoft HTTPS authorization host");
            Check(app.Windows.Count==1 && Pages().Count==0,"all tested flows leave one main window and empty page stack");
            Console.WriteLine("ALL_INLINE_UI_CHECKS_PASSED (component interactions; not live Microsoft authorization)");
        }
        catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}
        finally{((DispatcherTimer)Field("miniDownloadTimer")).Stop();((FileStream)Field("queueLease")).Dispose();app.Shutdown();}
    }
    static IEnumerable<DependencyObject> Walk(DependencyObject parent){foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>()){yield return child;foreach(var nested in Walk(child))yield return nested;}}
    static IEnumerable<Button> Buttons(DependencyObject parent)=>Walk(parent).OfType<Button>();
}

