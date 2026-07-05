using System;
using System.Diagnostics;

using BAAZ.CMMS.App.Localization;

using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace BAAZ.CMMS.App.Services.Notifications;

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

    private static void Show(string title, string? body, string pageKey, Guid? requestId)
    {
        try
        {
            var builder = new AppNotificationBuilder().AddText(title);

            if (!string.IsNullOrEmpty(body))
                builder.AddText(body);

            builder.AddArgument("page", pageKey);

            if (requestId is { } id)
                builder.AddArgument("requestId", id.ToString("D"));

            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Toast] Show failed ({ex.GetType().Name}): {ex.Message}");
        }
    }
}
