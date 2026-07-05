using System;
using System.Runtime.InteropServices;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using WinRT.Interop;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Восстанавливает главное окно из свёрнутого/фонового состояния (клик по toast).</summary>
public static class MainWindowActivationHelper
{
    public static void BringToForeground(Window? window)
    {
        if (window is null)
            return;

        try
        {
            var hwnd = WindowNative.GetWindowHandle(window);

            if (window.AppWindow.Presenter is OverlappedPresenter overlapped
                && overlapped.State == OverlappedPresenterState.Minimized)
            {
                overlapped.Restore();
            }

            window.Activate();
            SetForegroundWindow(hwnd);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] BringToForeground failed: {ex.Message}");
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
