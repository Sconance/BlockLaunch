using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MCUGLauncher.Ui;

/// <summary>
/// 无边框窗口的窗口行为。
/// </summary>
/// <remarks>
/// 依赖 WPF <see cref="System.Windows.Window"/>、<see cref="System.Windows.Interop.WindowInteropHelper"/> 与 <see cref="System.Windows.Threading.DispatcherTimer"/>；所有方法都必须在 UI 线程调用，拖动期间每 15ms 轮询一次鼠标左键状态。
/// </remarks>
sealed class WindowChromeController(Window window)
{
    DispatcherTimer? _dragTimer;
    IntPtr _dragWindow;
    Point32 _dragCursor;
    Rect32 _dragRect;

    /// <summary>
    /// 初始化窗口外框。
    /// </summary>
    /// <remarks>
    /// 通过 DwmSetWindowAttribute(33) 请求 Windows 11 圆角；旧系统忽略失败。
    /// </remarks>
    public void Initialize()
    {
        var handle = new WindowInteropHelper(window).Handle;
        var preference = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(handle, 33, ref preference, sizeof(int));
    }

    /// <summary>
    /// 开始拖动窗口。
    /// </summary>
    /// <remarks>
    /// 记录光标与窗口起点并启动 15ms 定时器；WebView2 占用鼠标，不能用 DragMove。
    /// </remarks>
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

    /// <summary>
    /// 处理标题栏窗口命令。
    /// </summary>
    /// <param name="action">minimize / close / maximize，其他值忽略</param>
    /// <remarks>
    /// 支持 minimize/close/maximize，未知动作忽略；最大化时再次调用恢复普通状态。
    /// </remarks>
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

    /// <summary>
    /// 恢复默认窗口尺寸。
    /// </summary>
    /// <remarks>
    /// 宽高 1200×780，并退出最大化/最小化。
    /// </remarks>
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
