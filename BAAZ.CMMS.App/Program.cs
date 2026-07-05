using System;
using System.Diagnostics;
using System.Threading;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Services.Notifications;

using Helpers.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Microsoft.Windows.AppNotifications;

using WinRT;

namespace BAAZ.CMMS.App;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();
        SettingsHelper.Initialize();
        LanguageHelper.ApplySavedLanguage();
        InitializeAppNotifications();

        Application.Start(_ =>
        {
            var syncContext = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(syncContext);
            new App();
        });
    }

    private static void InitializeAppNotifications()
    {
        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
        }
        catch (Exception ex)
        {
            // Self-contained unpackaged: Insights.Resource.dll may be missing from WinAppSDK deployment (WindowsAppSDK #6071).
            Debug.WriteLine($"[AppNotifications] Register failed ({ex.GetType().Name}): {ex.Message}");
        }
    }

    private static void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        AppNotificationActivation.ParseArguments(args.Argument, out var pageKey, out var requestId);

        if (string.IsNullOrWhiteSpace(pageKey))
            return;

        if (App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                NavigateToastOnUiThread(pageKey, requestId)) == true)
            return;

        AppNotificationActivation.SetPending(pageKey, requestId);
    }

    private static void NavigateToastOnUiThread(string pageKey, Guid? requestId)
    {
        MainWindowActivationHelper.BringToForeground(App.MainWindow);

        if (App.Services.GetService<IShellNotificationPresenter>() is { } presenter)
            presenter.NavigateFromToast(pageKey, requestId);
        else
            AppNotificationActivation.SetPending(pageKey, requestId);
    }
}
