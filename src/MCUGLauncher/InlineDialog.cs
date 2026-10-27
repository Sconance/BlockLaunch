using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BlockLaunch;

/// <summary>A main-window surface. It owns no native window or independent message box.</summary>
public sealed class InlineDialog : ContentControl
{
    string title = "";

    public string Title
    {
        get => title;
        set
        {
            title = value;
            Host?.RefreshInlineTitle(this);
        }
    }

    internal Action? PrepareContent
    {
        get;
        set;
    }
    public object? Owner
    {
        get;
        set;
    }
    public SizeToContent SizeToContent
    {
        get;
        set;
    }
    public ResizeMode ResizeMode
    {
        get;
        set;
    }
    public WindowStartupLocation WindowStartupLocation
    {
        get;
        set;
    }

    public event EventHandler? Closed;
    public event CancelEventHandler? Closing;

    internal MainWindow? Host
    {
        get;
        private set;
    }

    DispatcherFrame? frame;
    bool closed;
    bool prepared;
    bool? result;

    public bool? DialogResult
    {
        get => result;
        set => Finish(value, false);
    }

    public void Show()
    {
        if (closed)
            throw new InvalidOperationException("此页面已关闭。");
        Host ??= Owner as MainWindow ?? (Owner as InlineDialog)?.Host ?? Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (Host == null)
            throw new InvalidOperationException("没有可用的启动器主窗口。");
        if (!prepared)
        {
            PrepareContent?.Invoke();
            prepared = true;
        }

        Width = double.NaN;
        Height = double.NaN;
        MinWidth = MinHeight = 0;
        MaxWidth = MaxHeight = double.PositiveInfinity;
        Host.PushInline(this);
    }

    public bool? ShowDialog()
    {
        frame = new DispatcherFrame();
        Show();
        if (!closed)
            Dispatcher.PushFrame(frame);
        frame = null;
        return result ?? false;
    }

    public bool Activate()
    {
        Host?.ActivateInline(this);
        return Focus();
    }

    public void Close() => Finish(null, false);
    internal void ForceClose() => Finish(false, true);

    void Finish(bool? value, bool force)
    {
        if (closed)
            return;
        var args = new CancelEventArgs();
        if (!force)
            Closing?.Invoke(this, args);
        if (args.Cancel)
            return;
        closed = true;
        result = value;
        Host?.PopInline(this);
        if (frame != null)
            frame.Continue = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

public static class InlineMessageBox
{
    public static MessageBoxResult Show(string message, string caption = "提示", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None) => Show(null, message, caption, buttons, icon);

    public static MessageBoxResult Show(object? owner, string message, string caption = "提示", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        var body = new StackPanel
        {
            MaxWidth = 680,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24)
        };
        body.Children.Add(new TextBlock { Text = caption, FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16), TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBox { Text = message, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 320, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, BorderThickness = new Thickness(0) });
        var dialog = new InlineDialog
        {
            Title = caption,
            Owner = owner,
            Content = new ScrollViewer
            {
                Content = body,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        };
        var selected = buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : buttons == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.Cancel;
        var actions = new WrapPanel
        {
            Margin = new Thickness(0, 18, 0, 0)
        };
        body.Children.Add(actions);
        void Add(string label, MessageBoxResult value)
        {
            var button = new Button
            {
                Content = label,
                MinWidth = 110
            };
            button.Click += (_, _) =>
            {
                selected = value;
                dialog.DialogResult = value is MessageBoxResult.OK or MessageBoxResult.Yes;
            };
            actions.Children.Add(button);
        }

        if (buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel)
        {
            Add("确定", MessageBoxResult.Yes);
            Add("取消", MessageBoxResult.No);
        }
        else
        {
            Add("知道了", MessageBoxResult.OK);
            if (buttons == MessageBoxButton.OKCancel)
                Add("取消", MessageBoxResult.Cancel);
        }

        dialog.ShowDialog();
        return selected;
    }
}
