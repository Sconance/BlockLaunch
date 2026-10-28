using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlockLaunch.Contracts;

namespace BlockLaunch.Front;

public sealed class SettingsViewModel(IFrontSettings settings) : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;
    CancellationTokenSource? pending;
    bool disposed;
    bool loaded;
    bool isBusy;
    string root = "";
    string java = "";
    string ram = "";
    string width = "";
    string height = "";
    string status = "尚未读取设置。";

    public string Root { get => root; set => Set(ref root, value); }
    public string Java { get => java; set => Set(ref java, value); }
    public string Ram { get => ram; set => Set(ref ram, value); }
    public string Width { get => width; set => Set(ref width, value); }
    public string Height { get => height; set => Set(ref height, value); }
    public string Status { get => status; private set => Set(ref status, value); }
    public bool IsBusy { get => isBusy; private set { Set(ref isBusy, value); NotifyAvailability(); } }
    public bool CanReload => !disposed && !IsBusy;
    public bool CanSave => loaded && CanReload;
    public bool CanEdit => CanSave;

    public Task Load() => Execute(false);
    public Task Save() => CanSave ? Execute(true) : Task.CompletedTask;

    async Task Execute(bool save)
    {
        if (!CanReload)
            return;
        var input = new FrontSettingsSnapshot(Root, Java, Ram, Width, Height);
        var cancellation = new CancellationTokenSource();
        pending = cancellation;
        IsBusy = true;
        Status = save ? "正在保存设置…" : "正在读取设置…";
        if (!save)
            loaded = false;
        try
        {
            var snapshot = save
                ? await settings.Save(input, cancellation.Token)
                : await settings.Read(cancellation.Token);
            if (disposed)
                return;
            Root = snapshot.Root;
            Java = snapshot.Java;
            Ram = snapshot.Ram;
            Width = snapshot.Width;
            Height = snapshot.Height;
            loaded = true;
            Status = save ? "设置已保存。重启预览后仍可读取。" : "设置已读取；编辑后点击保存。";
        }
        catch (OperationCanceledException)
        {
            if (!disposed)
                Status = "操作已取消；若保存已开始，请重新读取确认磁盘状态。";
        }
        catch (Exception exception)
        {
            if (!disposed)
                Status = (save ? "保存失败：" : "读取失败：") + exception.Message;
        }
        finally
        {
            pending = null;
            cancellation.Dispose();
            if (!disposed)
                IsBusy = false;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        pending?.Cancel();
        NotifyAvailability();
    }

    void NotifyAvailability()
    {
        PropertyChanged?.Invoke(this, new(nameof(CanReload)));
        PropertyChanged?.Invoke(this, new(nameof(CanSave)));
        PropertyChanged?.Invoke(this, new(nameof(CanEdit)));
    }

    void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
    }
}
