using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
namespace BlockLaunch;
public partial class MainWindow
{
    bool DownloadInProgress => operations?.Current?.Record.Download!=null;
    InlineDialog? downloadTasksPage;
    StackPanel? downloadTaskRows;
    ScrollViewer? downloadTaskScroll;
    DateTime downloadTaskRefresh;
    void OpenDownloadTasks()
    {
        if(downloadTasksPage!=null){downloadTasksPage.Activate();return;}
        var body=new DockPanel();var heading=new StackPanel();DockPanel.SetDock(heading,Dock.Top);
        heading.Children.Add(Hint("任务保存各自的安装目录。暂停保留进度；取消停止任务并保留已校验缓存。提交安装结果时请等待完成。"));
        heading.Children.Add(ActionButton("刷新任务",(_,_)=>RefreshDownloadTasks(true)));body.Children.Add(heading);
        downloadTaskRows=new StackPanel();downloadTaskScroll=new ScrollViewer{Content=downloadTaskRows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};body.Children.Add(downloadTaskScroll);
        downloadTasksPage=new InlineDialog{Owner=this,Title="下载任务",Tag="DownloadTasks",Content=body};
        downloadTasksPage.Closed+=(_,_)=>{downloadTasksPage=null;downloadTaskRows=null;downloadTaskScroll=null;};
        RefreshDownloadTasks(true);downloadTasksPage.Show();
    }
    void RefreshDownloadTasks(bool force=false)
    {
        if(downloadTaskRows==null || !force && (DateTime.UtcNow-downloadTaskRefresh).TotalSeconds<1)return;
        downloadTaskRefresh=DateTime.UtcNow;var offset=downloadTaskScroll?.VerticalOffset??0;downloadTaskRows.Children.Clear();
        var records=operations.History().Where(r=>r.Download!=null).Select(r=>r.Id==operations.Current?.Record.Id?operations.Current.Record:r).OrderBy(r=>r.State is OperationState.Running or OperationState.Pausing or OperationState.Cancelling?0:r.State is OperationState.Queued or OperationState.Paused?1:2).ThenByDescending(r=>r.Updated).Take(100).ToList();
        foreach(var record in records)
        {
            var panel=new StackPanel();panel.Children.Add(new TextBlock{Text=record.Name,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});
            panel.Children.Add(Hint(record.StateLabel+" · "+(record.Percent is {} p?$"{p:0}%":record.State==OperationState.Running?"准备 / 校验中":"—")+(operations.Current?.Record.Id==record.Id?" · "+miniDownloadSpeed.Text:"")));
            panel.Children.Add(new ProgressBar{Minimum=0,Maximum=100,Value=record.Percent??0,Height=4,Margin=new Thickness(0,4,0,8),IsIndeterminate=record.State==OperationState.Running&&record.Percent==null});
            panel.Children.Add(new TextBlock{Text=record.Message,TextWrapping=TextWrapping.Wrap,FontSize=11,ToolTip=record.Root});
            var actions=new WrapPanel();
            void Add(string name,OperationState state,bool enabled){var b=ActionButton(name,(_,_)=>ChangeDownloadById(record.Id,state));b.IsEnabled=enabled&&!record.Committing;actions.Children.Add(b);}
            Add("暂停",OperationState.Paused,record.State is OperationState.Running or OperationState.Queued);
            Add(record.State==OperationState.Failed?"重试":"继续",OperationState.Queued,record.State is OperationState.Paused or OperationState.Failed or OperationState.Interrupted || record.State==OperationState.Queued&&prefs.DownloadsPaused);
            Add("取消",OperationState.Cancelled,record.State is OperationState.Running or OperationState.Queued or OperationState.Paused or OperationState.Failed or OperationState.Interrupted);
            panel.Children.Add(actions);var card=FeatureCard(panel);card.Padding=new Thickness(12);card.Margin=new Thickness(0,0,0,8);downloadTaskRows.Children.Add(card);
        }
        if(records.Count==0)downloadTaskRows.Children.Add(Hint("暂无下载任务。"));
        downloadTaskScroll?.ScrollToVerticalOffset(offset);
    }
    void ChangeDownloadById(string id,OperationState state)
    {
        var record=operations.History().FirstOrDefault(r=>r.Id==id);if(record?.Download==null)return;
        var active=operations.Current?.Record.Id==id;
        var changed=active?state!=OperationState.Queued&&operations.Cancel(state==OperationState.Paused):state==OperationState.Queued&&record.State==OperationState.Queued || operations.ChangePending(id,state);
        if(changed && state==OperationState.Queued){prefs.DownloadsPaused=false;prefs.PausedDownloadId="";SaveDownloadPause();}
        if(changed && state==OperationState.Paused){prefs.DownloadsPaused=true;prefs.PausedDownloadId=id;SaveDownloadPause();}
        if(changed && state==OperationState.Cancelled && prefs.PausedDownloadId==id){prefs.PausedDownloadId="";SaveDownloadPause();}
        if(!changed)Status.Text="任务正在提交或状态已变化，请刷新后重试。";
        RefreshMiniDownloads(true);RefreshDownloadTasks(true);RefreshOperations();
    }
    void CancelMiniDownload(){if(MiniDownloadRecord() is {} record)ChangeDownloadById(record.Id,OperationState.Cancelled);}
    void UpdateTaskbarDownload(OperationRecord? record)
    {
        TaskbarItemInfo??=new TaskbarItemInfo();
        TaskbarItemInfo.ProgressState=record==null?TaskbarItemProgressState.None:record.State==OperationState.Failed?TaskbarItemProgressState.Error:record.State is OperationState.Paused or OperationState.Pausing?TaskbarItemProgressState.Paused:record.State==OperationState.Running?record.Percent==null?TaskbarItemProgressState.Indeterminate:TaskbarItemProgressState.Normal:TaskbarItemProgressState.Paused;
        TaskbarItemInfo.ProgressValue=Math.Clamp((record?.Percent??0)/100,0,1);
    }
    async Task RunAccountAction(Func<Task> action,string name)
    {
        if(DownloadInProgress)
        {
            if(InlineMessageBox.Show(this,"账号操作需要先暂停当前下载，是否暂停？之后可在下载任务中继续。",name,MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;
            var record=operations.Current!.Record;if(!operations.Cancel(pause:true)){InlineMessageBox.Show("正在提交安装结果，请稍后重试。");return;}
            prefs.DownloadsPaused=true;prefs.PausedDownloadId=record.Id;SaveDownloadPause();
            while(busy)await Task.Delay(50);
        }
        await Run(action,name);
    }
}
