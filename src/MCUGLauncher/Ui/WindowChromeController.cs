using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MCUGLauncher.Ui;

/// <summary>
/// 无边框窗口的窗口行为：DWM 圆角、拖动、最小化/最大化/关闭与尺寸复位。
/// 从 MainWindow 抽离，避免窗口 API 与 WebView2 消息分发混在同一个类里。
/// </summary>
sealed class WindowChromeController(Window window)
{
    DispatcherTimer? _dragTimer;
    IntPtr _dragWindow;
    Point32 _dragCursor;
    Rect32 _dragRect;

    /// <summary>在窗口句柄创建后调用，让 Windows 11 绘制圆角外框。</summary>
    public void Initialize()
    {
        var handle = new WindowInteropHelper(window).Handle;
        var preference = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(handle, 33, ref preference, sizeof(int));
    }

    /// <summary>WebView2 占用了鼠标，因此用定时器跟踪光标位置来拖动窗口。</summary>
    public void BeginDrag()
    {
        _dragWindow = new WindowInteropHelper(window).Handle;
        GetCursorPos(out _dragCursor);
        GetWindowRect(_dragWindow, out _dragRect);
        if (_dragTimer is null)
        {
            _dragTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            _dragTimer.Tick += (_, _) => DragTick();
        }
        _dragTimer.Start();
    }

    void DragTick()
    {
        if ((GetAsyncKeyState(0x01) & 0x8000) == 0)
        {
            _dragTimer?.Stop();
            return;
        }
        GetCursorPos(out var cursor);
        SetWindowPos(_dragWindow, IntPtr.Zero,
            _dragRect.Left + cursor.X - _dragCursor.X,
            _dragRect.Top + cursor.Y - _dragCursor.Y,
            0, 0, 0x0001 | 0x0004 | 0x0010);
    }

    /// <summary>处理界面标题栏发来的窗口命令。</summary>
    public void Handle(string action)
    {
        switch (action)
        {
            case "minimize":
                window.WindowState = WindowState.Minimized;
                break;
            case "close":
                window.Close();
                break;
            case "maximize":
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                break;
        }
    }

    /// <summary>把窗口恢复到默认的 1200 × 780。</summary>
    public void Reset()
    {
        window.Width = 1200;
        window.Height = 780;
        window.WindowState = WindowState.Normal;
    }

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out Point32 point);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hwnd, out Rect32 rect);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [StructLayout(LayoutKind.Sequential)]
    struct Point32
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rect32
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
