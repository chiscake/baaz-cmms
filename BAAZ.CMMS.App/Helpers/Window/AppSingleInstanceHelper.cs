using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// Перенаправляет повторный запуск (в т.ч. COM-активацию по клику на toast) в уже работающий процесс.
/// </summary>
public static class AppSingleInstanceHelper
{
    private const string InstanceKey = "BAAZ.CMMS.App";
    public const string RestartArgument = "--baaz-cmms-restart";
    private const uint AsfwAny = 0xFFFFFFFF;

    private static IntPtr _redirectEventHandle = IntPtr.Zero;

    public static bool IsRestartLaunch(string[] args) =>
        args.Any(static a => string.Equals(a, RestartArgument, StringComparison.OrdinalIgnoreCase));

    /// <summary>Перезапуск unpackaged-приложения с обходом single-instance redirect.</summary>
    public static void RestartApp()
    {
        var path = Environment.ProcessPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.WriteLine("[AppSingleInstance] RestartApp: process path is unavailable.");
            return;
        }

        try
        {
            AppInstance.GetCurrent().UnregisterKey();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSingleInstance] UnregisterKey failed: {ex.Message}");
        }

        Process.Start(new ProcessStartInfo(path, RestartArgument) { UseShellExecute = true });

        if (Application.Current is not null)
        {
            Application.Current.Exit();
            return;
        }

        Environment.Exit(0);
    }

    /// <returns>true, если текущий процесс следует завершить без запуска UI.</returns>
    public static bool TryRedirectToPrimaryInstance()
    {
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var mainInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (mainInstance.IsCurrent)
        {
            mainInstance.Activated += OnPrimaryInstanceActivated;
            return false;
        }

        RedirectActivationTo(activationArgs, mainInstance);
        return true;
    }

        private static void OnPrimaryInstanceActivated(object? sender, AppActivationArguments args)
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.High,
                () => MainWindowActivationHelper.BringToForeground(App.MainWindow));
        }

    private static void RedirectActivationTo(AppActivationArguments args, AppInstance mainInstance)
    {
        try
        {
            AllowSetForegroundWindow(mainInstance.ProcessId);
            AllowSetForegroundWindow(AsfwAny);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSingleInstance] AllowSetForegroundWindow failed: {ex.Message}");
        }

        _redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
        Task.Run(() =>
        {
            mainInstance.RedirectActivationToAsync(args).AsTask().Wait();
            SetEvent(_redirectEventHandle);
        });

        const uint CwmoDefault = 0;
        const uint Infinite = 0xFFFFFFFF;
        _ = CoWaitForMultipleObjects(
            CwmoDefault,
            Infinite,
            1,
            [_redirectEventHandle],
            out _);
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEvent(
        IntPtr lpEventAttributes,
        bool bManualReset,
        bool bInitialState,
        string? lpName);

    [DllImport("kernel32.dll")]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("ole32.dll")]
    private static extern uint CoWaitForMultipleObjects(
        uint dwFlags,
        uint dwMilliseconds,
        ulong nHandles,
        IntPtr[] pHandles,
        out uint dwIndex);
}
