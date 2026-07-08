using System;
using System.Threading;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using Helpers.Settings;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

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

        if (!AppSingleInstanceHelper.IsRestartLaunch(args)
            && AppSingleInstanceHelper.TryRedirectToPrimaryInstance())
            return;

        Application.Start(_ =>
        {
            var syncContext = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(syncContext);
            new App();
        });
    }
}
