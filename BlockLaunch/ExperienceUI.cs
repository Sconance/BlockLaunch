using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace BlockLaunch;
public partial class MainWindow
{
    string? nextServerAddress;
    string SaveBackupRoot => Path.Combine(settingsDir,"world-backups");
    void BuildExperienceUI()
    {
        var tools=new MenuItem { Header="Java 管理" }; tools.Click+=(_,_)=>OpenJavaManager(prefs.Root,Versions.SelectedValue as string ?? "",path=>{ if(busy) throw new IOException("请等待任务完成再更改 Java。"); prefs.Java=path; prefs.Launch.Java=path; JavaPath.Text=path; settings.Save(prefs); }); moreMenu.Items.Add(tools);
        var backups=new MenuItem { Header="存档保护与恢复" }; backups.Click+=(_,_)=>OpenSaveProtection(); moreMenu.Items.Add(backups);
        var servers=new MenuItem { Header="服务器收藏" }; servers.Click+=(_,_)=>OpenServers(); moreMenu.Items.Add(servers);
        var appearance=new MenuItem { Header="界面与辅助功能" }; appearance.Click+=(_,_)=>OpenAppearance(); moreMenu.Items.Add(appearance);
        var queue=(WrapPanel)((Grid)((TabItem)MainTabs.Items[6]).Content).Children.OfType<WrapPanel>().Single();
        foreach(var pair in new[]{("上移",-1),("下移",1)}) queue.Children.Add(ActionButton(pair.Item1,(_,_)=>{ if(TaskHistoryList.SelectedItem is OperationRecord record) { operations.MovePending(record.Id,pair.Item2); RefreshOperations(); } }));
        queue.Children.Add(ActionButton("下载限速",(_,_)=>OpenDownloadLimit()));
        Width=Math.Min(Width,Math.Max(780,SystemParameters.WorkArea.Width-24)); Height=Math.Min(Height,Math.Max(540,SystemParameters.WorkArea.Height-24)); MinWidth=780; MinHeight=540;
        miniDownloadProgress.Foreground=(Brush)FindResource("AccentBrush");
        Loaded+=(_,_)=>ThemeWindow(this);
    }
    async Task ProtectWorlds(string directory,string reason,CancellationToken token)
    {
        if(!prefs.ProtectSaves) return; RequireStopped();
        var registry=Path.Combine(prefs.Root,"instances"); var catalog=new InstanceCatalog(prefs.Root);
        var registered=Directory.Exists(registry) && Directory.EnumerateDirectories(registry).Any(d=>File.Exists(Path.Combine(d,".blocklaunch-instance.json")) && Path.GetFullPath(catalog.DirectoryFor(Path.GetFileName(d))).Equals(Path.GetFullPath(directory),StringComparison.OrdinalIgnoreCase));
        if(!registered && !InstanceUninstaller.Inside(directory,prefs.Root) && !Path.GetFullPath(directory).Equals(Path.GetFullPath(prefs.Root),StringComparison.OrdinalIgnoreCase)) throw new IOException("存档保护目录未登记在当前游戏目录中。");
        var backup=await Task.Run(()=>SaveProtection.Create(directory,SaveBackupRoot,reason,prefs.SaveBackupCount,token),token);
        if(backup!=null) Log($"存档保护完成：{backup.Worlds.Count} 个世界 · {reason}");
    }
    bool ConfirmModScan(ModScan scan,string title)
    {
        var panel=new StackPanel(); panel.Children.Add(Heading(scan.Summary)); panel.Children.Add(Hint("只检查已启用 JAR 的可识别元数据；未知条件不当作兼容结论。不会自动移动或删除模组。"));
        foreach(var finding in scan.Findings.Take(200)) panel.Children.Add(Hint($"{finding.Severity} · {finding.File}\n{finding.Message}"));
        var dialog=FeatureDialog(title,panel); var actions=new WrapPanel(); actions.Children.Add(ActionButton("返回检查",(_,_)=>dialog.DialogResult=false)); actions.Children.Add(ActionButton("了解风险，继续启动",(_,_)=>dialog.DialogResult=true)); panel.Children.Add(actions); return dialog.ShowDialog()==true;
    }
    async Task ShowModScan(string key,string directory,object? owner=null)
    {
        try
        {
            var descriptor=new InstanceCatalog(prefs.Root).GetOrCreate(key); var req=RequirementsFor(descriptor.LaunchProfileId);
            var scan=await Task.Run(()=>ModPreflight.Scan(directory,req.Minecraft,req.Loader,JavaDiscovery.RequiredMajor(prefs.Root,descriptor.LaunchProfileId),req.LoaderVersion));
            var panel=new StackPanel(); panel.Children.Add(Heading(scan.Summary)); panel.Children.Add(Hint("扫描启用的 JAR，含 Fabric 内嵌模组；复杂条件或未知元数据会提示人工核对。"));
            foreach(var finding in scan.Findings.Take(200)) panel.Children.Add(FeatureCard(new StackPanel { Children={Heading(finding.Severity+" · "+finding.File),Hint(finding.Message)} }));
            if(scan.Findings.Count==0) panel.Children.Add(Hint("未发现已知元数据问题；仍需由加载器完成实际兼容性验证。"));
            var dialog=FeatureDialog("本地 Mod 检查",panel); dialog.Owner=owner??this;
            panel.Children.Add(ActionButton("打开 Mod 文件夹",(_,_)=>OpenFolder(Path.Combine(directory,"mods"))));
            panel.Children.Add(ActionButton("修复后重新检查",async(_,_)=>{ dialog.Close(); await ShowModScan(key,directory,owner); })); dialog.ShowDialog();
        }
        catch(Exception ex) { InlineMessageBox.Show(owner??this,ex.Message,"Mod 检查失败"); }
    }
    void OpenDownloadLimit()
    {
        var body=new StackPanel(); body.Children.Add(Heading("下载限速")); body.Children.Add(Hint("单位 KiB/s，0 为不限速。对启动器 HTTP 下载立即生效；外部 Forge/NeoForge 安装器自身的网络请求不受此限制。")); var input=new TextBox {Text=prefs.Network.LimitKibPerSecond.ToString()}; body.Children.Add(input); var dialog=FeatureDialog("下载限速",body); dialog.Height=290;
        body.Children.Add(ActionButton("应用",(_,_)=>{ try { if(!int.TryParse(input.Text,out var limit) || limit is <0 or >1048576) throw new IOException("请输入 0–1048576。"); prefs.Network.LimitKibPerSecond=limit; runtimeNetwork.LimitKibPerSecond=limit; settings.Save(prefs); dialog.Close(); } catch(Exception ex) { InlineMessageBox.Show(dialog,ex.Message); } })); dialog.ShowDialog();
    }
    void OpenSaveProtection()
    {
        try
        {
            var key=Selected(); var directory=Instance(); var body=new StackPanel(); body.Children.Add(Heading("存档保护与恢复")); body.Children.Add(Hint("修改加载器和确认整合包升级前备份。恢复创建新世界，不覆盖现有存档。备份按当前实例独立保留。"));
            var enabled=new CheckBox { Content="操作前自动备份存档",IsChecked=prefs.ProtectSaves }; var count=new TextBox {Text=prefs.SaveBackupCount.ToString()}; body.Children.Add(enabled); body.Children.Add(Hint("保留最近几次备份（1–100）")); body.Children.Add(count);
            var choices=new ListBox {MinHeight=70,MaxHeight=220,DisplayMemberPath="Label"}; body.Children.Add(choices); var preview=Hint("选择备份查看内容。"); body.Children.Add(preview);
            void RefreshList() { choices.ItemsSource=SaveProtection.List(SaveBackupRoot,directory).SelectMany(b=>b.Worlds.Select(w=>new SaveChoice(b,w,$"{b.Created.ToLocalTime():yyyy-MM-dd HH:mm} · {w.World} · {w.Version} · {b.Reason}"))).ToList(); }
            RefreshList(); choices.SelectionChanged+=(_,_)=>{ if(choices.SelectedItem is SaveChoice item) { var path=SaveProtection.ArchivePath(SaveBackupRoot,directory,item.Backup,item.World); using var zip=System.IO.Compression.ZipFile.OpenRead(path); preview.Text=$"将恢复为新世界：{item.World.World}\n保存版本：{item.World.Version} · {zip.Entries.Count} 个归档条目 · {new FileInfo(path).Length/1048576d:0.0} MiB\n原世界保持不变。"; } };
            var dialog=FeatureDialog("存档保护",body);
            body.Children.Add(ActionButton("保存策略",(_,_)=>{ if(!int.TryParse(count.Text,out var keep) || keep is <1 or >100) { InlineMessageBox.Show(dialog,"保留数量应为 1–100。"); return; } prefs.ProtectSaves=enabled.IsChecked==true; prefs.SaveBackupCount=keep; settings.Save(prefs); preview.Text="策略已保存。"; }));
            body.Children.Add(ActionButton("立即备份所有世界",async(_,_)=>{ if(busy) { preview.Text="请等待当前任务完成。"; return; } await Run(async()=>{ RequireStopped(); await Task.Run(()=>SaveProtection.Create(directory,SaveBackupRoot,"手动备份",prefs.SaveBackupCount,CurrentToken),CurrentToken); },"备份存档"); RefreshList(); }));
            body.Children.Add(ActionButton("确认预览，恢复为新世界",async(_,_)=>{ if(choices.SelectedItem is not SaveChoice item || busy) return; var path=SaveProtection.ArchivePath(SaveBackupRoot,directory,item.Backup,item.World); await Run(async()=>{ RequireStopped(); var restored=await Task.Run(()=>WorldArchives.Import(directory,path,item.World.World+"-恢复",token:CurrentToken),CurrentToken); preview.Text="已恢复为独立世界："+restored; },"恢复存档副本"); }));
            body.Children.Add(ActionButton("打开备份目录",(_,_)=>OpenFolder(SaveProtection.DirectoryFor(SaveBackupRoot,directory)))); dialog.ShowDialog();
        }
        catch(Exception ex) { InlineMessageBox.Show(this,ex.Message,"存档保护"); }
    }
    record SaveChoice(SaveBackup Backup,SaveBackupItem World,string Label);
    void OpenServers()
    {
        var body=new StackPanel(); body.Children.Add(Heading("服务器收藏")); body.Children.Add(Hint("收藏保存在启动器配置中。点击查询才连接服务器，显示 MOTD、版本、人数和 Ping；暂不解析 DNS SRV，必要时填写实际端口。"));
        var list=new ListBox {MinHeight=70,MaxHeight=220,DisplayMemberPath="Name"}; body.Children.Add(list);
        var name=new TextBox {ToolTip="服务器名称"}; var address=new TextBox {ToolTip="域名:端口 或 [IPv6]:端口"}; var profiles=new ComboBox {ItemsSource=LocalVersions().Order().ToList(),SelectedItem=Versions.SelectedValue as string}; body.Children.Add(Hint("名称 / 地址 / 启动实例")); body.Children.Add(name); body.Children.Add(address); body.Children.Add(profiles); var status=Hint("选择或添加服务器。"); body.Children.Add(status);
        void Reload() { var id=(list.SelectedItem as ServerFavorite)?.Id; list.ItemsSource=prefs.Servers.ToList(); list.SelectedItem=prefs.Servers.FirstOrDefault(s=>s.Id==id); }
        list.SelectionChanged+=(_,_)=>{ if(list.SelectedItem is ServerFavorite item) { name.Text=item.Name; address.Text=item.Address; profiles.SelectedItem=item.Profile; status.Text="尚未查询服务器状态。"; } }; Reload();
        var dialog=FeatureDialog("服务器收藏",body); using var cts=new CancellationTokenSource(); dialog.Closed+=(_,_)=>cts.Cancel();
        body.Children.Add(ActionButton("新增 / 保存",(_,_)=>{ try { var endpoint=ServerEndpoint.Parse(address.Text); if(string.IsNullOrWhiteSpace(name.Text) || name.Text.Length>100) throw new IOException("名称应为 1–100 个字符。"); if(profiles.SelectedItem is not string profile) throw new IOException("请选择已安装实例。"); var old=list.SelectedItem as ServerFavorite; var item=new ServerFavorite(old?.Id??Guid.NewGuid().ToString("N"),name.Text.Trim(),endpoint.ToString(),profile); if(old!=null) prefs.Servers.Remove(old); prefs.Servers.Add(item); settings.Save(prefs); Reload(); list.SelectedItem=item; } catch(Exception ex) { status.Text=ex.Message; } }));
        body.Children.Add(ActionButton("清空选择，添加新收藏",(_,_)=>{ list.SelectedItem=null; name.Clear(); address.Clear(); }));
        body.Children.Add(ActionButton("查询在线状态",async(_,_)=>{ var captured=address.Text; status.Text="正在查询…"; try { var result=await ServerFavorites.Ping(captured,cts.Token); if(address.Text==captured && !cts.IsCancellationRequested) status.Text=$"{result.Description}\n{result.Version} · {result.Online}/{result.Maximum} 人 · {result.LatencyMs} ms"; } catch(OperationCanceledException) { if(!cts.IsCancellationRequested) status.Text="查询超时；不代表服务器一定离线。"; } catch(Exception ex) { status.Text="无法查询："+ex.Message; } }));
        body.Children.Add(ActionButton("启动并连接所选服务器",async(_,_)=>{ if(busy || list.SelectedItem is not ServerFavorite item) return; if(!LocalVersions().Contains(item.Profile)) { status.Text="对应实例已不存在，请重新选择并保存。"; return; } Versions.SelectedValue=item.Profile; dialog.Close(); nextServerAddress=item.Address; try { await Run(Launch,"连接收藏服务器"); } finally {nextServerAddress=null;} }));
        body.Children.Add(ActionButton("移除收藏",(_,_)=>{ if(list.SelectedItem is ServerFavorite item) { prefs.Servers.Remove(item); settings.Save(prefs); Reload(); } })); dialog.ShowDialog();
    }
}

