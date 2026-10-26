using System.IO;
using System.IO.Enumeration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace BlockLaunch;

public class OpenFileDialog : InlineFilePicker { public OpenFileDialog() : base(false,false) { } }
public sealed class SaveFileDialog : InlineFilePicker { public SaveFileDialog() : base(true,false) { } }
public sealed class OpenFolderDialog : InlineFilePicker { public OpenFolderDialog() : base(false,true) { } }

public abstract class InlineFilePicker(bool save, bool folder)
{
    public string Title { get; set; } = "";
    public string Filter { get; set; } = "所有文件|*.*";
    public string InitialDirectory { get; set; } = "";
    public string FileName { get; set; } = "";
    public string[] FileNames { get; private set; } = [];
    public string FolderName { get; set; } = "";
    public bool Multiselect { get; set; }
    public bool OverwritePrompt { get; set; } = true;
    public string DefaultExt { get; set; } = "";
    public bool AddExtension { get; set; } = true;
    public int FilterIndex { get; set; } = 1;
    public static bool MatchesFilter(string name,string patterns) => patterns.Split(';',StringSplitOptions.RemoveEmptyEntries).Any(p=>p is "*" or "*.*" || FileSystemName.MatchesSimpleExpression(p.Trim(),name,true));
    public bool? ShowDialog(object? owner=null)
    {
        var host=owner as MainWindow ?? (owner as InlineDialog)?.Host ?? Application.Current.Windows.OfType<MainWindow>().FirstOrDefault() ?? throw new InvalidOperationException("没有可用的启动器窗口。");
        var body=new Grid {Margin=new Thickness(0,0,0,8)};
        foreach(var height in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto,GridLength.Auto}) body.RowDefinitions.Add(new(){Height=height});
        var pathRow=new DockPanel(); var up=new Button {Content="↑ 上一级",MinWidth=90}; var go=new Button {Content="前往",MinWidth=70}; var address=new TextBox {ToolTip="输入文件夹路径，按 Enter 前往"};
        DockPanel.SetDock(up,Dock.Left); DockPanel.SetDock(go,Dock.Right); pathRow.Children.Add(up); pathRow.Children.Add(go); pathRow.Children.Add(address); body.Children.Add(pathRow);
        var places=new WrapPanel(); Grid.SetRow(places,1); body.Children.Add(places);
        var list=new ListBox {SelectionMode=Multiselect && !folder && !save ? SelectionMode.Extended : SelectionMode.Single,MinHeight=130,HorizontalContentAlignment=HorizontalAlignment.Stretch};
        list.ItemTemplate=(DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><DockPanel><TextBlock Text='{Binding Size}' DockPanel.Dock='Right' Foreground='#647782' Margin='12,0,4,0'/><TextBlock Text='{Binding Kind}' DockPanel.Dock='Left' Width='44' Foreground='#006DB8'/><TextBlock Text='{Binding Name}' TextTrimming='CharacterEllipsis'/></DockPanel></DataTemplate>"); Grid.SetRow(list,2); body.Children.Add(list);
        var fileRow=new DockPanel {Margin=new Thickness(0,8,0,0),Visibility=folder ? Visibility.Collapsed : Visibility.Visible}; var types=new ComboBox {Width=180,ToolTip="文件类型"}; DockPanel.SetDock(types,Dock.Right); fileRow.Children.Add(types); var filename=new TextBox {Text=Path.GetFileName(FileName),ToolTip=Multiselect ? "按 Ctrl/Shift 在列表中选择多个文件，或在此输入文件路径" : "文件名或完整文件路径"}; fileRow.Children.Add(filename); Grid.SetRow(fileRow,3); body.Children.Add(fileRow);
        var bottom=new DockPanel {Margin=new Thickness(0,8,0,0)}; var accept=new Button {Content=folder ? "选择此文件夹" : save ? "保存到此处" : "打开所选文件",MinWidth=140}; var cancel=new Button {Content="取消",MinWidth=80}; DockPanel.SetDock(accept,Dock.Right); DockPanel.SetDock(cancel,Dock.Right); bottom.Children.Add(accept); bottom.Children.Add(cancel); var state=new TextBlock {VerticalAlignment=VerticalAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=System.Windows.Media.Brushes.SlateGray}; bottom.Children.Add(state); Grid.SetRow(bottom,4); body.Children.Add(bottom);
        var dialog=new InlineDialog {Title=Title.Length>0 ? Title : folder ? "选择文件夹" : save ? "选择保存位置" : "选择文件",Owner=owner??host,Content=body};
        var filterParts=Filter.Split('|'); var filters=new List<(string Name,string Pattern)>(); for(var i=0;i+1<filterParts.Length;i+=2) filters.Add((filterParts[i],filterParts[i+1])); if(filters.Count==0) filters.Add(("所有文件","*.*")); types.ItemsSource=filters.Select(f=>f.Name).ToList(); types.SelectedIndex=Math.Clamp(FilterIndex-1,0,filters.Count-1);
        var current=""; var revision=0; var first=true; var updating=false; var typed=false; CancellationTokenSource? navigation=null;
        async Task Navigate(string path)
        {
            navigation?.Cancel(); var cts=new CancellationTokenSource(); navigation=cts; var mine=++revision; list.IsEnabled=false; state.Text="正在读取文件夹…";
            try
            {
                path=Path.GetFullPath(Environment.ExpandEnvironmentVariables(path)); var pattern=filters[Math.Max(0,types.SelectedIndex)].Pattern;
                var entries=await Task.Run(()=>ReadDirectory(path,folder,pattern,cts.Token),cts.Token);
                if(cts.IsCancellationRequested || mine!=revision) return;
                current=path; address.Text=path; list.ItemsSource=entries; typed=false;
                state.Text=entries.Count>=5000 ? "显示前 5000 项；可在名称框输入完整路径。" : $"{entries.Count} 项"+(Multiselect ? " · Ctrl/Shift 多选" : "");
            }
            catch(OperationCanceledException) { }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { if(mine==revision) state.Text="无法读取此位置："+ex.Message; }
            finally { if(mine==revision) {list.IsEnabled=true; navigation=null;} cts.Dispose(); }
        }
        async void Visit(string path) => await Navigate(path);
        void Place(string label,string path) { if(string.IsNullOrWhiteSpace(path)) return; var button=new Button {Content=label,Padding=new Thickness(9,5,9,5)}; button.Click+=(_,_)=>Visit(path); places.Children.Add(button); }
        Place("桌面",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); Place("文档",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); Place("下载",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"));
        foreach(var drive in DriveInfo.GetDrives()) Place(drive.Name,drive.RootDirectory.FullName);
        var newFolder=new Button {Content="新建文件夹",Padding=new Thickness(9,5,9,5)}; places.Children.Add(newFolder);
        newFolder.Click+=(_,_)=>
        {
            if(!Directory.Exists(current)) return;
            var panel=new StackPanel {Margin=new Thickness(20)}; panel.Children.Add(new TextBlock {Text="新文件夹名称",FontSize=18}); var input=new TextBox(); panel.Children.Add(input); var notice=new TextBlock {TextWrapping=TextWrapping.Wrap}; panel.Children.Add(notice); var create=new Button {Content="创建"}; panel.Children.Add(create); var prompt=new InlineDialog {Owner=dialog,Title="新建文件夹",Content=panel};
            create.Click+=(_,_)=>{ try { var name=input.Text.Trim(); if(name is "" or "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars())>=0 || name.EndsWith('.') || name.EndsWith(' ')) throw new IOException("请输入有效的文件夹名称。"); var path=Path.Combine(current,name); if(Directory.Exists(path) || File.Exists(path)) throw new IOException("同名项目已存在。"); Directory.CreateDirectory(path); prompt.DialogResult=true; } catch(Exception ex) {notice.Text=ex.Message;} }; if(prompt.ShowDialog()==true) Visit(current);
        };
        up.Click+=(_,_)=>{ try { var parent=Directory.GetParent(current); if(parent!=null) Visit(parent.FullName); } catch(Exception ex) {state.Text=ex.Message;} };
        go.Click+=(_,_)=>Visit(address.Text); address.KeyDown+=(_,e)=>{ if(e.Key==Key.Enter) {e.Handled=true;Visit(address.Text);} };
        types.SelectionChanged+=(_,_)=>{ if(current.Length>0) Visit(current); };
        filename.TextChanged+=(_,_)=>{if(!updating) typed=true;};
        list.SelectionChanged+=(_,_)=>{ if(list.SelectedItem is PickerEntry entry && !entry.IsDirectory) {updating=true;filename.Text=string.Join("; ",list.SelectedItems.OfType<PickerEntry>().Where(e=>!e.IsDirectory).Select(e=>e.Name));updating=false;typed=false;} };
        void Accept()
        {
            try
            {
                if(!list.IsEnabled || current.Length==0) return;
                if(folder)
                {
                    var path=list.SelectedItem is PickerEntry {IsDirectory:true} selected ? selected.Path : Path.GetFullPath(address.Text);
                    if(!Directory.Exists(path)) throw new IOException("所选文件夹不存在。"); FolderName=path; dialog.DialogResult=true; return;
                }
                var chosen=!typed && !save ? list.SelectedItems.OfType<PickerEntry>().Where(e=>!e.IsDirectory).Select(e=>e.Path).ToArray() : [];
                if(chosen.Length==0)
                {
                    if(string.IsNullOrWhiteSpace(filename.Text)) throw new IOException("请先选择文件或输入文件名。");
                    var path=Path.GetFullPath(Path.IsPathRooted(filename.Text.Trim()) ? filename.Text.Trim() : Path.Combine(current,filename.Text.Trim()));
                    if(Directory.Exists(path)) {Visit(path);return;}
                    if(save && AddExtension && !Path.HasExtension(path)) {var extension=DefaultExt.Length>0 ? DefaultExt : filters[types.SelectedIndex].Pattern.Split(';')[0].Replace("*",""); if(extension.Length>0 && extension!=".") path+=extension.StartsWith('.') ? extension : "."+extension;}
                    chosen=[path];
                }
                if(!Multiselect && chosen.Length>1) throw new IOException("此操作只能选择一个文件。");
                if(chosen.Any(path=>!MatchesFilter(Path.GetFileName(path),filters[types.SelectedIndex].Pattern))) throw new IOException("文件类型不匹配，请调整类型筛选。");
                if(save)
                {
                    var target=chosen[0]; if(!Directory.Exists(Path.GetDirectoryName(target))) throw new IOException("保存文件夹不存在，请先创建或选择文件夹。");
                    if(File.Exists(target) && OverwritePrompt && InlineMessageBox.Show(dialog,"文件已存在，是否覆盖？\n"+target,"覆盖文件确认",MessageBoxButton.OKCancel)!=MessageBoxResult.OK) return;
                }
                else if(chosen.Any(path=>!File.Exists(path))) throw new IOException("所选文件不存在。");
                FileNames=chosen; FileName=chosen[0]; FilterIndex=types.SelectedIndex+1; dialog.DialogResult=true;
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) {state.Text=ex.Message;}
        }
        list.MouseDoubleClick+=(_,_)=>{if(list.SelectedItem is PickerEntry entry) {if(entry.IsDirectory) Visit(entry.Path);else if(!save) Accept();}};
        list.KeyDown+=(_,e)=>{if(e.Key==Key.Enter) {e.Handled=true;if(list.SelectedItem is PickerEntry {IsDirectory:true} entry) Visit(entry.Path);else Accept();}};
        filename.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){e.Handled=true;Accept();}}; accept.Click+=(_,_)=>Accept(); cancel.Click+=(_,_)=>dialog.DialogResult=false;
        dialog.Closed+=(_,_)=>navigation?.Cancel();
        dialog.Loaded+=(_,_)=>{if(!first)return;first=false;var initial=InitialDirectory; if(initial.Length==0 && Path.IsPathRooted(FileName)) initial=Path.GetDirectoryName(FileName)??""; if(!Directory.Exists(initial)) initial=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); if(!Directory.Exists(initial)) initial=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);Visit(initial);};
        return dialog.ShowDialog();
    }
    internal static List<PickerEntry> ReadDirectory(string directory,bool foldersOnly,string filter,CancellationToken token)
    {
        if(!Directory.Exists(directory)) throw new DirectoryNotFoundException("文件夹不存在。"); var result=new List<PickerEntry>();
        foreach(var path in Directory.EnumerateFileSystemEntries(directory))
        {
            token.ThrowIfCancellationRequested(); if(result.Count>=5000) break;
            try {var attributes=File.GetAttributes(path);var isDirectory=(attributes&FileAttributes.Directory)!=0;var name=Path.GetFileName(path);if(!isDirectory && (foldersOnly || !MatchesFilter(name,filter)))continue;var length=isDirectory ? 0 : new FileInfo(path).Length;result.Add(new(name,Path.GetFullPath(path),isDirectory,isDirectory ? "文件夹" : "文件",isDirectory ? "" : length>=1048576 ? $"{length/1048576d:0.0} MiB" : $"{length/1024d:0.0} KiB"));}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){ }
        }
        return result.OrderByDescending(e=>e.IsDirectory).ThenBy(e=>e.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
    }
    internal record PickerEntry(string Name,string Path,bool IsDirectory,string Kind,string Size);
}
