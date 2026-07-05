using System;
using System.IO;
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
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "baaz-logo.ico");
        AppNotificationManager.Default.Register(
            ResourceStrings.Get("App_Title"),
            new Uri(iconPath));
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
