using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using WinRT.Interop;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Восстанавливает главное окно из свёрнутого/фонового состояния (клик по toast).</summary>
public static class MainWindowActivationHelper
{
    private const int SwRestore = 9;
    private const int SwShow = 5;
    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpShowwindow = 0x0040;

    private static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr HwndNotTopMost = new(-2);

    public static void BringToForeground(Window? window)
    {
        if (window is null)
            return;

        try
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var appWindow = window.AppWindow;

            if (!appWindow.IsVisible)
                appWindow.Show();

            if (appWindow.Presenter is OverlappedPresenter overlapped
                && overlapped.State == OverlappedPresenterState.Minimized)
            {
                overlapped.Restore();
            }

            appWindow.MoveInZOrderAtTop();
            window.Activate();

            ShowWindow(hwnd, IsIconic(hwnd) ? SwRestore : SwShow);

            if (!SetForegroundWindow(hwnd))
                SwitchToThisWindow(hwnd, altTab: true);

            if (GetForegroundWindow() != hwnd)
                ApplyTopMostForegroundWorkaround(hwnd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] BringToForeground failed: {ex.Message}");
        }
    }

    private static void ApplyTopMostForegroundWorkaround(IntPtr hwnd)
    {
        SetWindowPos(hwnd, HwndTopMost, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpShowwindow);
        SetWindowPos(hwnd, HwndNotTopMost, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpShowwindow);
        SetForegroundWindow(hwnd);

        if (GetForegroundWindow() != hwnd)
            SwitchToThisWindow(hwnd, altTab: true);
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern void SwitchToThisWindow(IntPtr hWnd, bool altTab);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
