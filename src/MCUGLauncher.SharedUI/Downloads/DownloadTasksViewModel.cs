using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlockLaunch.Contracts;

namespace BlockLaunch.SharedUI;

public sealed class DownloadTasksViewModel(IFrontDownloadTasks downloads) : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<FrontDownloadResult>? CommandCompleted;
    bool disposed;
    bool isBusy;
    bool isPaused;
    string status = "正在读取下载任务…";
    IReadOnlyList<FrontDownloadTask> tasks = [];

    public IReadOnlyList<FrontDownloadTask> Tasks { get => tasks; private set => Set(ref tasks, value); }
    public bool IsPaused { get => isPaused; private set => Set(ref isPaused, value); }
    public bool IsBusy { get => isBusy; private set => Set(ref isBusy, value); }
    public string Status { get => status; private set => Set(ref status, value); }
    public string QueueStatus => IsPaused ? "队列已暂停；点击某个任务的继续按钮恢复。" : "队列可运行；实际执行由宿主负责。";

    public void Refresh()
    {
        if (disposed || IsBusy)
            return;
        try
        {
            IsBusy = true;
            ReadTasks();
        }
        catch (Exception exception)
        {
            Status = "读取下载任务失败：" + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Change(string id, FrontDownloadAction action)
    {
        if (disposed || IsBusy)
            return;
        try
        {
            IsBusy = true;
            var result = downloads.Change(id, action);
            Status = result.Warning.Length > 0 ? result.Warning
                : result.Changed ? "任务操作已接受。" : "任务状态已变化或正在提交，请等待后刷新。";
            try
            {
                ReadTasks();
            }
            catch (Exception exception)
            {
                Status += " 任务刷新失败：" + exception.Message;
            }
            CommandCompleted?.Invoke(result);
        }
        catch (Exception exception)
        {
            Status = "下载操作失败：" + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    void ReadTasks()
    {
        var snapshot = downloads.Read();
        IsPaused = snapshot.IsPaused;
        Tasks = snapshot.Tasks;
        PropertyChanged?.Invoke(this, new(nameof(QueueStatus)));
        if (Status == "正在读取下载任务…" || Status.StartsWith("读取下载任务失败："))
            Status = "任务状态已读取。";
    }

    public void Dispose()
    {
        disposed = true;
        CommandCompleted = null;
    }

    void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
    }
}
