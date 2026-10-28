using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace BlockLaunch;

public sealed class GameWindowBehaviorController(Window host, Action<string> log)
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool SetWindowText(IntPtr handle, string title);

    [DllImport("psapi.dll")]
    static extern bool EmptyWorkingSet(IntPtr handle);

    public async Task Apply(Process process, LaunchSettings settings, GameSession session)
    {
        try
        {
            process.PriorityClass = Enum.Parse<ProcessPriorityClass>(settings.Priority);
            if (settings.TrimLauncher)
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            host.Dispatcher.Invoke(() =>
            {
                if (settings.Visibility == "启动后最小化")
                    host.WindowState = WindowState.Minimized;
                if (settings.Visibility == "启动后隐藏")
                    host.Hide();
            });
            if (settings.WindowTitle.Length > 0)
                for (var i = 0; i < 60 && !process.HasExited; i++)
                {
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        SetWindowText(process.MainWindowHandle, settings.WindowTitle);
                        break;
                    }

                    await Task.Delay(500);
                }

            await session.Completion;
        }
        catch (Exception ex)
        {
            log("启动外观设置：" + ex.Message);
        }
        finally
        {
            if (!host.Dispatcher.HasShutdownStarted && settings.Visibility == "启动后隐藏")
                await host.Dispatcher.InvokeAsync(() =>
                {
                    host.Show();
                    host.WindowState = WindowState.Normal;
                });
        }
    }
}
