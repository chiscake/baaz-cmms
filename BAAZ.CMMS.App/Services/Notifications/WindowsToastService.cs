using System;
using System.Diagnostics;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using DevWinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Services.Notifications;

/// <summary>
/// In-app уведомления (DevWinUI Growl, глобальное floating-окно) — замена системных Windows toast.
/// Windows AppNotificationManager.Register() не работает в self-contained unpackaged деплое
/// (отсутствует Microsoft.WindowsAppRuntime.Insights.Resource.dll, WindowsAppSDK #6071/#6387),
/// поэтому клик по toast не активировал приложение и не навигировал. Growl работает полностью
/// в процессе — без COM-активации и системных ретраев.
/// </summary>
public sealed class WindowsToastService : IWindowsToastService
{
    public void ShowRequestNew(Guid requestId, string requestNumber)
    {
        Show(
            ResourceStrings.Get("Toast_Request_New"),
            string.Format(ResourceStrings.Get("Toast_Request_New_Body"), requestNumber),
            "RequestDetail",
            requestId);
    }

    public void ShowRequestStatusChanged(Guid requestId, string requestNumber, string statusLabel)
    {
        Show(
            ResourceStrings.Get("Toast_Request_StatusChanged"),
            string.Format(ResourceStrings.Get("Toast_Request_StatusChanged_Body"), requestNumber, statusLabel),
            "MyRequests",
            requestId);
    }

    public void ShowScheduleUpdated()
    {
        Show(
            ResourceStrings.Get("Toast_Schedule_StatusChanged"),
            body: null,
            "MaintenanceSchedule",
            requestId: null);
    }

    public void ShowToolRequisitionReady(Guid linkId, string requisitionNumber, string statusLabel)
    {
        Show(
            ResourceStrings.Get("Notification_ToolRequisition_Ready_Title"),
            string.Format(ResourceStrings.Get("Notification_ToolRequisition_Ready_Message"), requisitionNumber, statusLabel),
            "ToolRequisitionHistory",
            requestId: null,
            linkId: linkId);
    }

    private static void Show(string title, string? body, string pageKey, Guid? requestId, Guid? linkId = null)
    {
        var dispatcher = App.MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

        void ShowGrowl()
        {
            try
            {
                Growl.InfoGlobal(new GrowlInfo
                {
                    Title = title,
                    Message = body,
                    ShowDateTime = true,
                    StaysOpen = false,
                    WaitTime = TimeSpan.FromSeconds(8),
                    IsClosable = true,
                    UseBlueColorForInfo = true,
                    ShowCloseButton = true,
                    ShowConfirmButton = true,
                    ConfirmButtonText = ResourceStrings.Get("Toast_Action_Open"),
                    ConfirmButtonClicked = (_, _) =>
                    {
                        NavigateFromGrowl(pageKey, requestId, linkId);
                        return true;
                    },
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Toast] Growl show failed ({ex.GetType().Name}): {ex.Message}");
            }
        }

        if (dispatcher.HasThreadAccess)
            ShowGrowl();
        else
            dispatcher.TryEnqueue(DispatcherQueuePriority.High, ShowGrowl);
    }

    private static void NavigateFromGrowl(string pageKey, Guid? requestId, Guid? linkId = null)
    {
        MainWindowActivationHelper.BringToForeground(App.MainWindow);

        if (App.Services.GetService<IShellNotificationPresenter>() is { } presenter)
            presenter.NavigateFromToast(pageKey, requestId, linkId);
    }
}
